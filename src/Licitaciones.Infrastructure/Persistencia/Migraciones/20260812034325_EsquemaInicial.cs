using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Licitaciones.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class EsquemaInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "estados_licitacion",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estados_licitacion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "niveles_aprobacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    monto_minimo_crc = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    monto_maximo_crc = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    aprobador = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niveles_aprobacion", x => x.id);
                    table.CheckConstraint("ck_niveles_aprobacion_minimo_positivo", "monto_minimo_crc > 0");
                    table.CheckConstraint("ck_niveles_aprobacion_rango_coherente", "monto_maximo_crc IS NULL OR monto_maximo_crc >= monto_minimo_crc");
                });

            migrationBuilder.CreateTable(
                name: "proveedores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proveedores", x => x.id);
                    table.CheckConstraint("ck_proveedores_nombre_no_vacio", "length(btrim(nombre)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "tipos_cambio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    crc_por_usd = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    fecha_vigencia = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_cambio", x => x.id);
                    table.CheckConstraint("ck_tipos_cambio_valor_positivo", "crc_por_usd > 0");
                });

            migrationBuilder.CreateTable(
                name: "licitaciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    codigo_normalizado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    fecha_cierre = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    presupuesto_estimado_crc = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_licitaciones", x => x.id);
                    table.CheckConstraint("ck_licitaciones_codigo_no_vacio", "length(btrim(codigo)) > 0");
                    table.CheckConstraint("ck_licitaciones_presupuesto_positivo", "presupuesto_estimado_crc > 0");
                    table.ForeignKey(
                        name: "fk_licitaciones_estado",
                        column: x => x.estado,
                        principalTable: "estados_licitacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ofertas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    licitacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proveedor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monto_ofertado_crc = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fecha_registro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ofertas", x => x.id);
                    table.CheckConstraint("ck_ofertas_monto_positivo", "monto_ofertado_crc > 0");
                    table.ForeignKey(
                        name: "fk_ofertas_licitacion",
                        column: x => x.licitacion_id,
                        principalTable: "licitaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ofertas_proveedor",
                        column: x => x.proveedor_id,
                        principalTable: "proveedores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "estados_licitacion",
                columns: new[] { "id", "descripcion", "nombre" },
                values: new object[,]
                {
                    { 0, "Licitación en preparación; admite edición y no acepta ofertas.", "Borrador" },
                    { 1, "Licitación publicada; acepta ofertas hasta la fecha de cierre.", "Publicada" },
                    { 2, "Licitación cerrada; estado terminal que conserva las ofertas como evidencia.", "Cerrada" }
                });

            migrationBuilder.InsertData(
                table: "niveles_aprobacion",
                columns: new[] { "id", "aprobador", "created_at", "monto_maximo_crc", "monto_minimo_crc", "updated_at", "version" },
                values: new object[,]
                {
                    { new Guid("6f1f4d3e-0f2a-4c5d-9c6b-1a2b3c4d5e01"), "Encargado de área", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 999999.99m, 0.01m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 },
                    { new Guid("6f1f4d3e-0f2a-4c5d-9c6b-1a2b3c4d5e02"), "Gerencia", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 9999999.99m, 1000000.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 },
                    { new Guid("6f1f4d3e-0f2a-4c5d-9c6b-1a2b3c4d5e03"), "Junta Directiva", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 10000000.00m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 }
                });

            migrationBuilder.InsertData(
                table: "tipos_cambio",
                columns: new[] { "id", "activo", "crc_por_usd", "created_at", "fecha_vigencia", "updated_at", "version" },
                values: new object[] { new Guid("8a2b6c4d-1e3f-4a5b-8c9d-0e1f2a3b4c05"), true, 520.0000m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1 });

            migrationBuilder.CreateIndex(
                name: "ux_estados_licitacion_nombre",
                table: "estados_licitacion",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_licitaciones_estado",
                table: "licitaciones",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "ix_licitaciones_fecha_cierre",
                table: "licitaciones",
                column: "fecha_cierre");

            migrationBuilder.CreateIndex(
                name: "ux_licitaciones_codigo_normalizado",
                table: "licitaciones",
                column: "codigo_normalizado",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_niveles_aprobacion_monto_minimo",
                table: "niveles_aprobacion",
                column: "monto_minimo_crc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ofertas_licitacion_monto_fecha",
                table: "ofertas",
                columns: new[] { "licitacion_id", "monto_ofertado_crc", "fecha_registro" });

            migrationBuilder.CreateIndex(
                name: "IX_ofertas_proveedor_id",
                table: "ofertas",
                column: "proveedor_id");

            migrationBuilder.CreateIndex(
                name: "ux_ofertas_licitacion_proveedor",
                table: "ofertas",
                columns: new[] { "licitacion_id", "proveedor_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_proveedores_nombre_normalizado",
                table: "proveedores",
                column: "nombre_normalizado",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tipos_cambio_fecha_vigencia",
                table: "tipos_cambio",
                column: "fecha_vigencia");

            migrationBuilder.CreateIndex(
                name: "ux_tipos_cambio_activo",
                table: "tipos_cambio",
                column: "activo",
                unique: true,
                filter: "activo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "niveles_aprobacion");

            migrationBuilder.DropTable(
                name: "ofertas");

            migrationBuilder.DropTable(
                name: "tipos_cambio");

            migrationBuilder.DropTable(
                name: "licitaciones");

            migrationBuilder.DropTable(
                name: "proveedores");

            migrationBuilder.DropTable(
                name: "estados_licitacion");
        }
    }
}
