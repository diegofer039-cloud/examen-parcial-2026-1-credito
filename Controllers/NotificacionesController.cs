using System.Security.Claims;
using CreditoPlataforma.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CreditoPlataforma.Controllers;

[Authorize]
public class NotificacionesController(ApplicationDbContext context) : Controller
{
    private readonly ApplicationDbContext _context = context;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var notificaciones = await _context.Notificaciones
            .AsNoTracking()
            .Where(n => n.UsuarioId == usuarioId)
            .OrderByDescending(n => n.FechaProcesamientoUtc)
            .ToListAsync();

        return View(notificaciones);
    }
}
