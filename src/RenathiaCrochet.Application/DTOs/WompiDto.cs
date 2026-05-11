using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RenathiaCrochet.Application.DTOs
{
    /// <summary>
    /// Respuesta del endpoint POST /api/Cart/checkout.
    /// Contiene el OrderId y todos los datos necesarios para inicializar
    /// el widget de pago de Wompi en el frontend.
    /// </summary>
    public class CheckoutResponseDto
    {
        /// <summary>ID del pedido creado (Status = PendingPayment).</summary>
        public int OrderId { get; set; }

        /// <summary>Total del pedido en pesos colombianos (COP).</summary>
        public decimal Total { get; set; }

        // --- Datos para el widget de Wompi ---

        /// <summary>Llave publica de Wompi (pub_stagtest_... o pub_prod_...).</summary>
        public string PublicKey { get; set; } = string.Empty;

        /// <summary>Referencia unica del pago. Se usa el OrderId como referencia.</summary>
        public string Reference { get; set; } = string.Empty;

        /// <summary>Monto en centavos (Total * 100). Wompi requiere centavos.</summary>
        public long AmountInCents { get; set; }

        /// <summary>Moneda. Siempre "COP" para Colombia.</summary>
        public string Currency { get; set; } = "COP";

        /// <summary>
        /// Hash de integridad calculado en el backend con la llave secreta.
        /// Formula: SHA256(reference + amount_in_cents + currency + integrity_key)
        /// Se calcula en el backend para no exponer la llave de integridad al frontend.
        /// </summary>
        public string IntegrityHash { get; set; } = string.Empty;
    }

    // ─── DTOs para el webhook que Wompi envia al backend ─────────────────────

    /// <summary>
    /// Cuerpo completo del evento webhook que envia Wompi.
    /// Wompi envia este JSON via POST cuando una transaccion cambia de estado.
    /// </summary>
    public class WompiWebhookDto
    {
        [JsonPropertyName("data")]
        public WompiWebhookDataDto? Data { get; set; }

        /// <summary>Tipo de evento, por ejemplo "transaction.updated".</summary>
        [JsonPropertyName("event")]
        public string? Event { get; set; }

        /// <summary>"test" en sandbox, "production" en produccion.</summary>
        [JsonPropertyName("environment")]
        public string? Environment { get; set; }

        /// <summary>Unix timestamp del momento en que Wompi genero el evento.</summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        /// <summary>Firma para validar que el evento viene realmente de Wompi.</summary>
        [JsonPropertyName("signature")]
        public WompiSignatureDto? Signature { get; set; }
    }

    public class WompiWebhookDataDto
    {
        [JsonPropertyName("transaction")]
        public WompiTransactionDto? Transaction { get; set; }
    }

    /// <summary>
    /// Datos de la transaccion dentro del webhook.
    /// Los campos importantes para nosotros son: Status, Reference y Id.
    /// </summary>
    public class WompiTransactionDto
    {
        /// <summary>ID unico de la transaccion en Wompi.</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>Monto en centavos.</summary>
        [JsonPropertyName("amount_in_cents")]
        public long AmountInCents { get; set; }

        /// <summary>La referencia que enviamos desde el frontend (= OrderId).</summary>
        [JsonPropertyName("reference")]
        public string Reference { get; set; } = string.Empty;

        [JsonPropertyName("customer_email")]
        public string? CustomerEmail { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "COP";

        [JsonPropertyName("payment_method_type")]
        public string? PaymentMethodType { get; set; }

        /// <summary>Estado: "APPROVED", "DECLINED", "VOIDED", "ERROR".</summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("status_message")]
        public string? StatusMessage { get; set; }
    }

    /// <summary>
    /// Firma HMAC que incluye Wompi para verificar autenticidad del webhook.
    /// </summary>
    public class WompiSignatureDto
    {
        /// <summary>SHA256 calculado por Wompi.</summary>
        [JsonPropertyName("checksum")]
        public string? Checksum { get; set; }

        /// <summary>
        /// Lista de rutas JSON cuyas valores se usan para calcular el checksum.
        /// Ejemplo: ["data.transaction.id", "data.transaction.status", "data.transaction.amount_in_cents"]
        /// </summary>
        [JsonPropertyName("properties")]
        public List<string> Properties { get; set; } = new();
    }
}
