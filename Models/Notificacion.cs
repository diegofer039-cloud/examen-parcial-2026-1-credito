using System.ComponentModel.DataAnnotations;

namespace CreditoPlataforma.Models;

public class Notificacion
{
    public int Id { get; set; }

    [Required]
    [StringLength(64)]
    public string MessageId { get; set; } = string.Empty;

    public int SolicitudId { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Texto { get; set; } = string.Empty;

    public DateTime FechaProcesamientoUtc { get; set; } = DateTime.UtcNow;
}
