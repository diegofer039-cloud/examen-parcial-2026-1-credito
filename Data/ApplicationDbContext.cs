using CreditoPlataforma.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CreditoPlataforma.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<SolicitudCredito> SolicitudesCredito => Set<SolicitudCredito>();

    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Cliente>(entity =>
        {
            entity.Property(c => c.IngresosMensuales)
                .HasColumnType("REAL")
                .HasConversion<double>();

            entity.HasIndex(c => c.UsuarioId).IsUnique();

            entity.HasOne(c => c.Usuario)
                .WithOne()
                .HasForeignKey<Cliente>(c => c.UsuarioId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Cliente_IngresosMensuales",
                "\"IngresosMensuales\" > 0"));
        });

        builder.Entity<SolicitudCredito>(entity =>
        {
            entity.Property(s => s.MontoSolicitado)
                .HasColumnType("REAL")
                .HasConversion<double>();

            entity.HasOne(s => s.Cliente)
                .WithMany(c => c.Solicitudes)
                .HasForeignKey(s => s.ClienteId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => s.Estado);

            entity.HasIndex(s => s.ClienteId)
                .IsUnique()
                .HasFilter("\"Estado\" = 0");

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Solicitud_MontoSolicitado",
                "\"MontoSolicitado\" > 0"));
        });

        builder.Entity<Notificacion>(entity =>
        {
            entity.HasIndex(n => n.MessageId).IsUnique();
            entity.HasIndex(n => n.UsuarioId);
            entity.Property(n => n.Texto).IsRequired().HasMaxLength(500);
        });
    }
}
