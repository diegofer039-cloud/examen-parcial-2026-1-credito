using CreditoPlataforma.Data;
using CreditoPlataforma.Hubs;
using CreditoPlataforma.Models;
using CreditoPlataforma.Models.ViewModels;
using CreditoPlataforma.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CreditoPlataforma.Controllers;

[Authorize(Roles = SeedData.RolAnalista)]
public class AnalistaController(
    ApplicationDbContext context,
    ISolicitudCacheService cache,
    IHubContext<SolicitudesHub> hub) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly ISolicitudCacheService _cache = cache;
    private readonly IHubContext<SolicitudesHub> _hub = hub;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var pendientes = await _context.SolicitudesCredito
            .AsNoTracking()
            .Include(s => s.Cliente)
            .ThenInclude(c => c.Usuario)
            .Where(s => s.Estado == EstadoSolicitud.Pendiente)
            .OrderByDescending(s => s.FechaSolicitud)
            .ToListAsync();

        return View(pendientes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(int id)
    {
        var solicitud = await CargarSolicitudParaProcesar(id);
        if (solicitud is null)
        {
            return NotFound();
        }

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["MensajeError"] =
                $"La solicitud #{solicitud.Id} ya fue procesada ({solicitud.Estado}) y no puede modificarse.";
            return RedirectToAction(nameof(Index));
        }

        if (solicitud.MontoSolicitado > solicitud.Cliente.IngresosMensuales * 5)
        {
            TempData["MensajeError"] =
                $"No se puede aprobar la solicitud #{solicitud.Id}: el monto {solicitud.MontoSolicitado:N2} " +
                $"supera 5 veces los ingresos mensuales ({solicitud.Cliente.IngresosMensuales * 5:N2}).";
            return RedirectToAction(nameof(Index));
        }

        solicitud.Estado = EstadoSolicitud.Aprobado;
        solicitud.MotivoRechazo = null;
        await _context.SaveChangesAsync();
        await _cache.InvalidarListadoPorClienteAsync(solicitud.ClienteId);
        await NotificarPropietario(solicitud);

        TempData["MensajeExito"] = $"Solicitud #{solicitud.Id} aprobada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Rechazar(int id)
    {
        var solicitud = await CargarSolicitudParaProcesar(id);
        if (solicitud is null)
        {
            return NotFound();
        }

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["MensajeError"] =
                $"La solicitud #{solicitud.Id} ya fue procesada ({solicitud.Estado}) y no puede modificarse.";
            return RedirectToAction(nameof(Index));
        }

        return View(new RechazoViewModel
        {
            SolicitudId = solicitud.Id,
            MontoSolicitado = solicitud.MontoSolicitado,
            IngresosMensuales = solicitud.Cliente.IngresosMensuales
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(int id, RechazoViewModel modelo)
    {
        if (id != modelo.SolicitudId)
        {
            return BadRequest();
        }

        var solicitud = await CargarSolicitudParaProcesar(id);
        if (solicitud is null)
        {
            return NotFound();
        }

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["MensajeError"] =
                $"La solicitud #{solicitud.Id} ya fue procesada ({solicitud.Estado}) y no puede modificarse.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(modelo.MotivoRechazo))
        {
            ModelState.AddModelError(nameof(RechazoViewModel.MotivoRechazo), "El motivo de rechazo es obligatorio.");
        }

        if (!ModelState.IsValid)
        {
            modelo.MontoSolicitado = solicitud.MontoSolicitado;
            modelo.IngresosMensuales = solicitud.Cliente.IngresosMensuales;
            return View(modelo);
        }

        solicitud.Estado = EstadoSolicitud.Rechazado;
        solicitud.MotivoRechazo = modelo.MotivoRechazo.Trim();
        await _context.SaveChangesAsync();
        await _cache.InvalidarListadoPorClienteAsync(solicitud.ClienteId);
        await NotificarPropietario(solicitud);

        TempData["MensajeExito"] = $"Solicitud #{solicitud.Id} rechazada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task NotificarPropietario(SolicitudCredito solicitud)
    {
        var usuarioPropietario = solicitud.Cliente.UsuarioId;
        if (string.IsNullOrEmpty(usuarioPropietario))
        {
            return;
        }

        var evento = new SolicitudEstadoDto(solicitud.Id, solicitud.Estado.ToString(), solicitud.MotivoRechazo);

        await _hub.Clients.User(usuarioPropietario)
            .SendAsync("SolicitudEstadoActualizado", evento);
    }

    private async Task<SolicitudCredito?> CargarSolicitudParaProcesar(int id)
    {
        return await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id);
    }
}
