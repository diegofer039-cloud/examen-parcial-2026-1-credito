using System.ComponentModel.DataAnnotations;

namespace CreditoPlataforma.Models;

public class SolicitudCredito
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public Cliente Cliente { get; set; } = null!;

    [Range(0.01, double.MaxValue, ErrorMessage = "El MontoSolicitado debe ser mayor a 0.")]
    public double MontoSolicitado { get; set; }

    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;

    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;

    [StringLength(500)]
    public string? MotivoRechazo { get; set; }
}
