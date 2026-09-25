using System.Text.Json;
using CreditoPlataforma.Data;
using CreditoPlataforma.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace CreditoPlataforma.Services;

public interface ISolicitudCacheService
{
    Task<List<SolicitudListadoDto>?> GetListadoAsync(string usuarioId);

    Task SetListadoAsync(string usuarioId, List<SolicitudListadoDto> solicitudes);

    Task InvalidarListadoAsync(string usuarioId);

    Task InvalidarListadoPorClienteAsync(int clienteId);
}

public class SolicitudCacheService(
    IDistributedCache cache,
    ApplicationDbContext context,
    ILogger<SolicitudCacheService> logger) : ISolicitudCacheService
{
    public static readonly TimeSpan DuracionCache = TimeSpan.FromSeconds(60);

    private static string Clave(string usuarioId) => $"solicitudes:listado:{usuarioId}";

    private static readonly DistributedCacheEntryOptions Opciones = new()
    {
        AbsoluteExpirationRelativeToNow = DuracionCache
    };

    public async Task<List<SolicitudListadoDto>?> GetListadoAsync(string usuarioId)
    {
        var json = await cache.GetStringAsync(Clave(usuarioId));
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<SolicitudListadoDto>>(json);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Cache de solicitudes inválida para el usuario {UsuarioId}; se elimina.", usuarioId);
            await cache.RemoveAsync(Clave(usuarioId));
            return null;
        }
    }

    public async Task SetListadoAsync(string usuarioId, List<SolicitudListadoDto> solicitudes)
    {
        var json = JsonSerializer.Serialize(solicitudes);
        await cache.SetStringAsync(Clave(usuarioId), json, Opciones);
    }

    public async Task InvalidarListadoAsync(string usuarioId)
    {
        await cache.RemoveAsync(Clave(usuarioId));
        logger.LogInformation("Cache de solicitudes invalidada para el usuario {UsuarioId}.", usuarioId);
    }

    public async Task InvalidarListadoPorClienteAsync(int clienteId)
    {
        var usuarioId = await context.Clientes
            .Where(c => c.Id == clienteId)
            .Select(c => c.UsuarioId)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrEmpty(usuarioId))
        {
            await InvalidarListadoAsync(usuarioId);
        }
    }
}
