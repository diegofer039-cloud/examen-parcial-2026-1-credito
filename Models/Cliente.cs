using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CreditoPlataforma.Models;

public class Cliente
{
    public int Id { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;

    public IdentityUser? Usuario { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Los IngresosMensuales deben ser mayores a 0.")]
    public double IngresosMensuales { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<SolicitudCredito> Solicitudes { get; set; } = new List<SolicitudCredito>();
}
