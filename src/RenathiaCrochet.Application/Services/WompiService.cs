using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using RenathiaCrochet.Application.DTOs;

namespace RenathiaCrochet.Application.Services
{
    /// <summary>
    /// Servicio que encapsula la logica criptografica de Wompi.
    ///
    /// Responsabilidades:
    ///   1. Calcular el hash de integridad para el widget de pago (frontend).
    ///   2. Validar la firma del webhook recibido de Wompi (backend).
    ///
    /// Las claves se leen de variables de entorno / appsettings para que funcione
    /// tanto en localhost (appsettings.Development.json) como en Azure (App Settings).
    /// </summary>
    public class WompiService
    {
        private readonly string _publicKey;

        /// <summary>
        /// Llave de eventos de Wompi.
        /// Se usa para validar que el webhook realmente viene de Wompi.
        /// En el panel de Wompi aparece como "Llave de eventos" (stagtest_events_...).
        /// Si aun no tienes esta llave separada, puedes usar temporalmente la llave privada.
        /// </summary>
        private readonly string _eventsKey;

        /// <summary>
        /// Llave de integridad de Wompi.
        /// Se usa para firmar los datos del widget (reference + amount + currency).
        /// En el panel aparece como "Llave de integridad" (stagtest_integrity_...).
        /// Si no la tienes separada, usa la llave privada.
        /// </summary>
        private readonly string _integrityKey;

        public WompiService(IConfiguration config)
        {
            _publicKey = config["WOMPI_PUBLIC_KEY"]
                ?? throw new InvalidOperationException("Falta la variable de entorno WOMPI_PUBLIC_KEY");

            _eventsKey = config["WOMPI_EVENTS_KEY"]
                ?? config["WOMPI_PRIVATE_KEY"]
                ?? throw new InvalidOperationException("Falta WOMPI_EVENTS_KEY o WOMPI_PRIVATE_KEY");

            _integrityKey = config["WOMPI_INTEGRITY_KEY"]
                ?? config["WOMPI_PRIVATE_KEY"]
                ?? throw new InvalidOperationException("Falta WOMPI_INTEGRITY_KEY o WOMPI_PRIVATE_KEY");
        }

        /// <summary>Expone la llave publica para incluirla en la respuesta del checkout.</summary>
        public string PublicKey => _publicKey;

        /// <summary>
        /// Calcula el hash de integridad que el widget de Wompi necesita para verificar
        /// que los datos del pago no fueron alterados en el frontend.
        ///
        /// Formula oficial de Wompi:
        ///   SHA256( reference + amount_in_cents + currency + integrity_key )
        ///
        /// Ejemplo:
        ///   reference      = "123"
        ///   amount_in_cents = 5000000  (= $50.000 COP)
        ///   currency       = "COP"
        ///   integrity_key  = "stagtest_integrity_abc..."
        ///   input          = "1235000000COPstagtest_integrity_abc..."
        ///   resultado      = SHA256(input) en hexadecimal minuscula
        /// </summary>
        public string ComputeIntegrityHash(string reference, long amountInCents, string currency = "COP")
        {
            var input = $"{reference}{amountInCents}{currency}{_integrityKey}";
            return ComputeSha256(input);
        }

        /// <summary>
        /// Valida la firma del webhook que envia Wompi.
        ///
        /// Wompi incluye en el body un objeto "signature" con:
        ///   - "checksum": el SHA256 que ellos calcularon
        ///   - "properties": lista de rutas JSON cuyos valores entran al hash
        ///
        /// El algoritmo de validacion es:
        ///   1. Extraer los valores de las propiedades listadas en signature.properties
        ///   2. Concatenarlos en ese orden
        ///   3. Agregar el timestamp del evento
        ///   4. Agregar la events_key
        ///   5. Calcular SHA256 de todo lo concatenado
        ///   6. Comparar con el checksum que envio Wompi
        ///
        /// Si no coincide, el webhook no viene de Wompi (o fue alterado) y se rechaza.
        /// </summary>
        public bool ValidateWebhookSignature(WompiWebhookDto webhook)
        {
            if (webhook.Signature?.Checksum == null || webhook.Signature.Properties.Count == 0)
                return false;

            var concat = new StringBuilder();

            // Paso 1 y 2: concatenar los valores de las propiedades en orden
            foreach (var propertyPath in webhook.Signature.Properties)
            {
                concat.Append(ResolveProperty(webhook, propertyPath));
            }

            // Paso 3: agregar el timestamp
            concat.Append(webhook.Timestamp);

            // Paso 4: agregar la llave de eventos
            concat.Append(_eventsKey);

            // Paso 5: calcular SHA256
            var computed = ComputeSha256(concat.ToString());

            // Paso 6: comparar (case-insensitive por si acaso)
            return string.Equals(computed, webhook.Signature.Checksum, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Resuelve una ruta de propiedad con notacion de punto (ej: "data.transaction.id")
        /// a su valor dentro del objeto WompiWebhookDto.
        ///
        /// Wompi especifica las propiedades que participan en el hash usando esta notacion.
        /// Cubrimos las propiedades que Wompi incluye en sus eventos de transaccion.
        /// </summary>
        private static string ResolveProperty(WompiWebhookDto webhook, string path) => path switch
        {
            "data.transaction.id"
                => webhook.Data?.Transaction?.Id ?? "",
            "data.transaction.status"
                => webhook.Data?.Transaction?.Status ?? "",
            "data.transaction.amount_in_cents"
                => webhook.Data?.Transaction?.AmountInCents.ToString() ?? "",
            "data.transaction.reference"
                => webhook.Data?.Transaction?.Reference ?? "",
            "data.transaction.customer_email"
                => webhook.Data?.Transaction?.CustomerEmail ?? "",
            "data.transaction.payment_method_type"
                => webhook.Data?.Transaction?.PaymentMethodType ?? "",
            "data.transaction.currency"
                => webhook.Data?.Transaction?.Currency ?? "",
            _ => ""  // propiedad desconocida: contribuye cadena vacia
        };

        /// <summary>
        /// Calcula SHA256 de un string UTF-8 y lo retorna en hexadecimal minuscula.
        /// Es el formato que usa Wompi para sus hashes.
        /// </summary>
        private static string ComputeSha256(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}
