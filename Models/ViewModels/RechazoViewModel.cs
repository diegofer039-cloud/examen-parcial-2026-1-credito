using System.ComponentModel.DataAnnotations;

namespace CreditoPlataforma.Models.ViewModels;

public class RechazoViewModel
{
    public int SolicitudId { get; set; }

    public double MontoSolicitado { get; set; }

    public double IngresosMensuales { get; set; }

    [Required(ErrorMessage = "El MotivoRechazo es obligatorio.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 500 caracteres.")]
    [Display(Name = "Motivo de rechazo")]
    public string MotivoRechazo { get; set; } = string.Empty;
}
