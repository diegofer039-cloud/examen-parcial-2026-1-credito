using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditoPlataforma.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDominioCredito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: false),
                    IngresosMensuales = table.Column<double>(type: "REAL", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                    table.CheckConstraint("CK_Cliente_IngresosMensuales", "\"IngresosMensuales\" > 0");
                    table.ForeignKey(
                        name: "FK_Clientes_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesCredito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    MontoSolicitado = table.Column<double>(type: "REAL", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    MotivoRechazo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesCredito", x => x.Id);
                    table.CheckConstraint("CK_Solicitud_MontoSolicitado", "\"MontoSolicitado\" > 0");
                    table.ForeignKey(
                        name: "FK_SolicitudesCredito_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_UsuarioId",
                table: "Clientes",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCredito_ClienteId",
                table: "SolicitudesCredito",
                column: "ClienteId",
                unique: true,
                filter: "\"Estado\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCredito_Estado",
                table: "SolicitudesCredito",
                column: "Estado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesCredito");

            migrationBuilder.DropTable(
                name: "Clientes");
        }
    }
}
