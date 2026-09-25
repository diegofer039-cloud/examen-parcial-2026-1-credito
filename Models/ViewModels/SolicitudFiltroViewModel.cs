using CreditoPlataforma.Models;

namespace CreditoPlataforma.Models.ViewModels;

public class SolicitudFiltroViewModel
{
    public EstadoSolicitud? Estado { get; set; }

    public double? MontoMin { get; set; }

    public double? MontoMax { get; set; }

    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    public List<SolicitudCredito> Resultados { get; set; } = new();

    public List<string> Errores { get; set; } = new();

    public bool HayErrores => Errores.Count > 0;

    public void Validar()
    {
        Errores = new List<string>();

        if (MontoMin is < 0 || MontoMax is < 0)
        {
            Errores.Add("No se aceptan montos negativos en el filtro de MontoSolicitado.");
        }

        if (MontoMin is not null && MontoMax is not null && MontoMin > MontoMax)
        {
            Errores.Add("El monto mínimo no puede ser mayor que el monto máximo.");
        }

        if (FechaDesde is not null && FechaHasta is not null && FechaDesde > FechaHasta)
        {
            Errores.Add("Rango de fechas inválido: la fecha de inicio no puede ser mayor que la fecha final.");
        }
    }
}
