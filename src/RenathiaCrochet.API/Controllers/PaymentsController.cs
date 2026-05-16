using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Application.Services;
using RenathiaCrochet.Domain.Entities;
using RenathiaCrochet.Domain.Interfaces;
using RenathiaCrochet.Infrastructure.Data;

namespace RenathiaCrochet.API.Controllers
{
    /// <summary>
    /// Controlador que recibe los eventos de pago enviados por Wompi.
    ///
    /// Flujo:
    ///   1. El cliente paga en el widget de Wompi (frontend).
    ///   2. Wompi llama a POST /api/Payments/wompi-webhook con el resultado.
    ///   3. Este endpoint valida la firma para confirmar que viene de Wompi.
    ///   4. Si el pago fue APPROVED, cambia el pedido de PendingPayment a PaymentReceived.
    ///
    /// IMPORTANTE: Este endpoint no requiere autenticacion JWT (el webhook viene de Wompi,
    /// no de un usuario autenticado). La seguridad se garantiza validando la firma.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly WompiService _wompiService;
        private readonly IOrderRepository _orderRepository;
        private readonly IUserRepository _userRepository;
        private readonly EmailService _emailService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            WompiService wompiService,
            IOrderRepository orderRepository,
            IUserRepository userRepository,
            EmailService emailService,
            ILogger<PaymentsController> logger)
        {
            _wompiService = wompiService;
            _orderRepository = orderRepository;
            _userRepository = userRepository;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/Payments/wompi-webhook
        ///
        /// Recibe el evento de Wompi cuando una transaccion cambia de estado.
        /// No requiere JWT porque el llamante es Wompi, no el usuario.
        ///
        /// Pasos:
        ///   1. Validar firma SHA256 con la llave de eventos de Wompi.
        ///   2. Ignorar eventos que no sean "transaction.updated".
        ///   3. Solo actuar si el estado es "APPROVED".
        ///   4. Buscar el pedido por la referencia (= OrderId).
        ///   5. Solo actualizar si el pedido sigue en "PendingPayment"
        ///      (evita procesar duplicados).
        ///   6. Cambiar status a "PaymentReceived" y registrar tracking.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("wompi-webhook")]
        public async Task<IActionResult> WompiWebhook([FromBody] WompiWebhookDto webhook)
        {
            // ── Paso 1: Validar firma ────────────────────────────────────────
            // Si la firma no coincide, el evento no viene de Wompi (o fue alterado).
            // Retornamos 400 para que Wompi sepa que algo esta mal.
            if (!_wompiService.ValidateWebhookSignature(webhook))
            {
                _logger.LogWarning("Webhook de Wompi recibido con firma invalida. Timestamp: {ts}", webhook.Timestamp);
                return BadRequest(new { message = "Firma del webhook invalida" });
            }

            // ── Paso 2: Solo procesar eventos de transaccion actualizada ─────
            if (webhook.Event != "transaction.updated")
            {
                _logger.LogInformation("Evento de Wompi ignorado: {evento}", webhook.Event);
                return Ok(new { message = "Evento ignorado" });
            }

            var transaction = webhook.Data?.Transaction;
            if (transaction == null)
            {
                _logger.LogWarning("Webhook de Wompi sin datos de transaccion");
                return BadRequest(new { message = "Datos de transaccion ausentes" });
            }

            _logger.LogInformation(
                "Webhook Wompi: transaccion {id}, estado {status}, referencia {ref}",
                transaction.Id, transaction.Status, transaction.Reference);

            // ── Paso 3: Solo actuar si el pago fue aprobado ──────────────────
            if (transaction.Status != "APPROVED")
            {
                // El pago fue rechazado o tuvo error.
                // El pedido se queda en PendingPayment; el usuario puede intentar de nuevo.
                _logger.LogInformation(
                    "Transaccion {id} no aprobada (status: {status}). Pedido queda en PendingPayment.",
                    transaction.Id, transaction.Status);
                return Ok(new { message = "Transaccion no aprobada, pedido sin cambios" });
            }

            // ── Paso 4: Buscar el pedido por la referencia (= OrderId) ───────
            if (!int.TryParse(transaction.Reference, out int orderId))
            {
                _logger.LogWarning("Referencia de Wompi no es un OrderId valido: {ref}", transaction.Reference);
                return BadRequest(new { message = "Referencia invalida" });
            }

            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("Pedido {orderId} no encontrado para la transaccion {id}", orderId, transaction.Id);
                return NotFound(new { message = "Pedido no encontrado" });
            }

            // ── Paso 5: Verificar que el pedido sigue pendiente ──────────────
            // Proteccion contra duplicados: si ya fue procesado, ignorar.
            if (order.Status != "PendingPayment")
            {
                _logger.LogInformation(
                    "Pedido {orderId} ya tiene status '{status}'. Webhook ignorado (duplicado).",
                    orderId, order.Status);
                return Ok(new { message = "Pedido ya procesado" });
            }

            // ── Paso 6: Confirmar el pago ────────────────────────────────────
            order.Status = "PaymentReceived";
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepository.UpdateAsync(order);

            await _orderRepository.AddTrackingAsync(new OrderTracking
            {
                OrderId = orderId,
                Status = "PaymentReceived",
                Notes = $"Pago aprobado por Wompi. Transaccion: {transaction.Id}"
            });

            _logger.LogInformation(
                "Pedido {orderId} confirmado como PaymentReceived. Transaccion Wompi: {id}",
                orderId, transaction.Id);

            // ── Paso 7: Enviar correo de confirmación al cliente ─────────────
            // Se envuelve en try-catch para que un fallo de correo nunca rompa
            // la respuesta 200 OK que Wompi necesita recibir.
            try
            {
                var user = await _userRepository.GetByIdAsync(order.UserId);
                if (user != null)
                {
                    await _emailService.SendOrderConfirmationAsync(order, user.Email);
                    _logger.LogInformation(
                        "Correo de confirmación enviado a {email} para el pedido {orderId}",
                        user.Email, orderId);
                }
            }
            catch (Exception ex)
            {
                // El pago ya fue procesado; solo registramos el error del correo.
                _logger.LogError(ex,
                    "Error al enviar correo de confirmación para el pedido {orderId}", orderId);
            }

            // Wompi espera un 200 OK para saber que recibimos correctamente el evento.
            return Ok(new { message = "Pago confirmado correctamente" });
        }
    }
}
