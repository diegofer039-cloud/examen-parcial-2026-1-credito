using System.Security.Claims;
using CreditoPlataforma.Data;
using CreditoPlataforma.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CreditoPlataforma.Hubs;

[Authorize]
public class SolicitudesHub(ApplicationDbContext context) : Hub
{
    public async Task<IReadOnlyList<SolicitudEstadoDto>> ObtenerMisEstados()
    {
        var usuarioId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(usuarioId))
        {
            return Array.Empty<SolicitudEstadoDto>();
        }

        var estados = await context.SolicitudesCredito
            .AsNoTracking()
            .Where(s => s.Cliente.UsuarioId == usuarioId)
            .Select(s => new SolicitudEstadoDto(s.Id, s.Estado.ToString(), s.MotivoRechazo))
            .ToListAsync();

        return estados;
    }

    public string? UsuarioActual() => Context.UserIdentifier ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
