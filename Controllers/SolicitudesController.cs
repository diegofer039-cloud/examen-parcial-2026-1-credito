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
