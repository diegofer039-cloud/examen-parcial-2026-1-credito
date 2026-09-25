using System.Text.Json.Serialization;

namespace CreditoPlataforma.Models;

public class SolicitudRegistrada
{
    [JsonPropertyName("MessageId")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("SolicitudId")]
    public int SolicitudId { get; set; }

    [JsonPropertyName("UsuarioId")]
    public string UsuarioId { get; set; } = string.Empty;

    [JsonPropertyName("FechaEventoUtc")]
    public DateTime FechaEventoUtc { get; set; } = DateTime.UtcNow;
}
