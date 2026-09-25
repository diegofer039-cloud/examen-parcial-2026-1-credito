using CreditoPlataforma.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CreditoPlataforma.Data;

public static class SeedData
{
    public const string RolAnalista = "Analista";
    public const string RolCliente = "Cliente";

    public const string PasswordAnalista = "Analista123!";
    public const string PasswordCliente = "Cliente123!";

    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("SeedData");

        await context.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        foreach (var rol in new[] { RolAnalista, RolCliente })
        {
            if (!await roleManager.RoleExistsAsync(rol))
            {
                await roleManager.CreateAsync(new IdentityRole(rol));
            }
        }

        var analista = await EnsureUserAsync(userManager, "analista@demo.com", PasswordAnalista, RolAnalista);
        var cliente1 = await EnsureUserAsync(userManager, "cliente1@demo.com", PasswordCliente, RolCliente);
        var cliente2 = await EnsureUserAsync(userManager, "cliente2@demo.com", PasswordCliente, RolCliente);

        var c1 = await EnsureClienteAsync(context, cliente1, ingresosMensuales: 5000d, activo: true);
        var c2 = await EnsureClienteAsync(context, cliente2, ingresosMensuales: 3000d, activo: true);

        if (!await context.SolicitudesCredito.AnyAsync())
        {
            context.SolicitudesCredito.AddRange(
                new SolicitudCredito
                {
                    ClienteId = c1.Id,
                    MontoSolicitado = 8000d,
                    FechaSolicitud = DateTime.UtcNow.AddDays(-2),
                    Estado = EstadoSolicitud.Pendiente
                },
                new SolicitudCredito
                {
                    ClienteId = c2.Id,
                    MontoSolicitado = 9000d,
                    FechaSolicitud = DateTime.UtcNow.AddDays(-10),
                    Estado = EstadoSolicitud.Aprobado
                });

            await context.SaveChangesAsync();
            logger.LogInformation("Datos iniciales creados: 2 clientes y 2 solicitudes.");
        }

        logger.LogInformation(
            "Usuarios de demostración listos. Analista: {Email} / {Password}. Clientes: cliente1@demo.com y cliente2@demo.com / {ClientePassword}",
            analista.Email, PasswordAnalista, PasswordCliente);
    }

    private static async Task<IdentityUser> EnsureUserAsync(
        UserManager<IdentityUser> userManager,
        string email,
        string password,
        string rol)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errores = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"No se pudo crear el usuario {email}: {errores}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, rol))
        {
            await userManager.AddToRoleAsync(user, rol);
        }

        return user;
    }

    private static async Task<Cliente> EnsureClienteAsync(
        ApplicationDbContext context,
        IdentityUser usuario,
        double ingresosMensuales,
        bool activo)
    {
        var cliente = await context.Clientes
            .FirstOrDefaultAsync(c => c.UsuarioId == usuario.Id);

        if (cliente is null)
        {
            cliente = new Cliente
            {
                UsuarioId = usuario.Id,
                IngresosMensuales = ingresosMensuales,
                Activo = activo
            };
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();
        }

        return cliente;
    }
}
