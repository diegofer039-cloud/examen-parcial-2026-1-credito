using System.Security.Claims;
using CreditoPlataforma.Data;
using CreditoPlataforma.Models;
using CreditoPlataforma.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditoPlataforma.Controllers;

[Authorize]
public class SolicitudesController(ApplicationDbContext context) : Controller
{
    private readonly ApplicationDbContext _context = context;

    [HttpGet]
    public async Task<IActionResult> Index(SolicitudFiltroViewModel filtro)
    {
        filtro.Validar();
        if (filtro.HayErrores)
        {
            return View(filtro);
        }

        filtro.Resultados = await ConsultarSolicitudesUsuario(filtro);
        return View(filtro);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var solicitud = await _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .ThenInclude(c => c.Usuario)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud is null)
        {
            return NotFound();
        }

        var esPropietaria = solicitud.Cliente.UsuarioId == userId;
        if (!esPropietaria && !User.IsInRole(SeedData.RolAnalista))
        {
            return Forbid();
        }

        return View(solicitud);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var cliente = await ObtenerClienteActual();
        ViewBag.IngresosMensuales = cliente?.IngresosMensuales ?? 0d;
        return View(new SolicitudRegistroViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SolicitudRegistroViewModel modelo)
    {
        var cliente = await ObtenerClienteActual();
        ViewBag.IngresosMensuales = cliente?.IngresosMensuales ?? 0d;

        if (cliente is null)
        {
            ViewData["Error"] = "Tu usuario no tiene un cliente asociado. Contacta al administrador.";
            return View(modelo);
        }

        if (!cliente.Activo)
        {
            ViewData["Error"] = "No puedes registrar solicitudes: el cliente está inactivo.";
            return View(modelo);
        }

        var tienePendiente = await _context.SolicitudesCredito
            .AnyAsync(s => s.ClienteId == cliente.Id && s.Estado == EstadoSolicitud.Pendiente);

        if (tienePendiente)
        {
            ViewData["Error"] = "Ya existe una solicitud en estado Pendiente para este cliente.";
            return View(modelo);
        }

        if (modelo.MontoSolicitado > cliente.IngresosMensuales * 10)
        {
            ViewData["Error"] =
                $"El monto solicitado ({modelo.MontoSolicitado:N2}) supera el máximo permitido " +
                $"(10 veces los ingresos mensuales: {cliente.IngresosMensuales * 10:N2}).";
            return View(modelo);
        }

        if (!ModelState.IsValid)
        {
            ViewData["Error"] = "Revisa los datos del formulario; no se registró la solicitud.";
            return View(modelo);
        }

        var solicitud = new SolicitudCredito
        {
            ClienteId = cliente.Id,
            MontoSolicitado = modelo.MontoSolicitado,
            FechaSolicitud = DateTime.UtcNow,
            Estado = EstadoSolicitud.Pendiente
        };

        _context.SolicitudesCredito.Add(solicitud);
        await _context.SaveChangesAsync();

        ViewData["Exito"] =
            $"Solicitud #{solicitud.Id} registrada correctamente en estado Pendiente " +
            $"por {solicitud.MontoSolicitado:N2}.";

        return View(new SolicitudRegistroViewModel());
    }

    private async Task<Cliente?> ObtenerClienteActual()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        return await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);
    }

    private async Task<List<SolicitudCredito>> ConsultarSolicitudesUsuario(SolicitudFiltroViewModel filtro)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        IQueryable<SolicitudCredito> query = _context.SolicitudesCredito
            .Include(s => s.Cliente)
            .Where(s => s.Cliente.UsuarioId == userId);

        if (filtro.Estado.HasValue)
        {
            query = query.Where(s => s.Estado == filtro.Estado.Value);
        }

        if (filtro.MontoMin.HasValue)
        {
            query = query.Where(s => s.MontoSolicitado >= filtro.MontoMin.Value);
        }

        if (filtro.MontoMax.HasValue)
        {
            query = query.Where(s => s.MontoSolicitado <= filtro.MontoMax.Value);
        }

        if (filtro.FechaDesde.HasValue)
        {
            var desde = filtro.FechaDesde.Value.Date;
            query = query.Where(s => s.FechaSolicitud >= desde);
        }

        if (filtro.FechaHasta.HasValue)
        {
            var hasta = filtro.FechaHasta.Value.Date.AddDays(1);
            query = query.Where(s => s.FechaSolicitud < hasta);
        }

        return await query.OrderByDescending(s => s.FechaSolicitud).ToListAsync();
    }
}
