using System.ComponentModel.DataAnnotations;

namespace CreditoPlataforma.Models.ViewModels;

public class SolicitudRegistroViewModel
{
    [Required(ErrorMessage = "El monto solicitado es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El MontoSolicitado debe ser mayor a 0.")]
    [Display(Name = "Monto solicitado")]
    public double MontoSolicitado { get; set; }
}
