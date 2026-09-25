using CreditoPlataforma.Models;

namespace CreditoPlataforma.Models.ViewModels;

public class SolicitudListadoDto
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public double MontoSolicitado { get; set; }

    public DateTime FechaSolicitud { get; set; }

    public EstadoSolicitud Estado { get; set; }

    public string? MotivoRechazo { get; set; }

    public double IngresosMensuales { get; set; }

    public bool ClienteActivo { get; set; }
}
