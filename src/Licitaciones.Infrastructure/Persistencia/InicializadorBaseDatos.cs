using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Licitaciones.Infrastructure.Persistencia;

/// <summary>
/// Aplica las migraciones pendientes de forma controlada al iniciar la aplicación.
/// </summary>
/// <remarks>
/// Toma un bloqueo de aviso (<c>pg_advisory_lock</c>) antes de migrar. Cuando la aplicación corre con varias
/// réplicas en Kubernetes, solo una ejecuta las migraciones y las demás esperan a que terminen, evitando
/// migraciones simultáneas sobre el mismo esquema.
/// </remarks>
public sealed class InicializadorBaseDatos
{
    /// <summary>Clave del bloqueo de aviso. Es arbitraria pero debe ser la misma en todas las réplicas.</summary>
    private const long ClaveBloqueoMigracion = 728_314_905;

    private readonly LicitacionesDbContext _contexto;
    private readonly ILogger<InicializadorBaseDatos> _registro;

    /// <summary>
    /// Crea el inicializador.
    /// </summary>
    /// <param name="contexto">Contexto de Entity Framework Core.</param>
    /// <param name="registro">Registro de eventos.</param>
    public InicializadorBaseDatos(LicitacionesDbContext contexto, ILogger<InicializadorBaseDatos> registro)
    {
        _contexto = contexto;
        _registro = registro;
    }

    /// <summary>
    /// Espera a que PostgreSQL esté disponible y aplica las migraciones pendientes.
    /// </summary>
    /// <param name="intentosMaximos">Cantidad de intentos de conexión antes de fallar.</param>
    /// <param name="esperaEntreIntentos">Espera entre intentos de conexión.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Tarea que finaliza cuando la base de datos está lista.</returns>
    public async Task MigrarAsync(
        int intentosMaximos = 10,
        TimeSpan? esperaEntreIntentos = null,
        CancellationToken cancelacion = default)
    {
        TimeSpan espera = esperaEntreIntentos ?? TimeSpan.FromSeconds(3);
        await EsperarDisponibilidadAsync(intentosMaximos, espera, cancelacion);

        string cadenaConexion = _contexto.Database.GetConnectionString()
            ?? throw new InvalidOperationException("No hay una cadena de conexión configurada.");

        await using var conexionBloqueo = new NpgsqlConnection(cadenaConexion);
        await conexionBloqueo.OpenAsync(cancelacion);

        await using (NpgsqlCommand bloquear = conexionBloqueo.CreateCommand())
        {
            bloquear.CommandText = "SELECT pg_advisory_lock($1)";
            bloquear.Parameters.AddWithValue(ClaveBloqueoMigracion);
            await bloquear.ExecuteNonQueryAsync(cancelacion);
        }

        try
        {
            IEnumerable<string> pendientes = await _contexto.Database.GetPendingMigrationsAsync(cancelacion);
            var listaPendientes = pendientes.ToList();

            if (listaPendientes.Count == 0)
            {
                _registro.LogInformation("La base de datos ya está actualizada; no hay migraciones pendientes.");
                return;
            }

            _registro.LogInformation(
                "Aplicando {Cantidad} migración(es) pendiente(s): {Migraciones}.",
                listaPendientes.Count,
                string.Join(", ", listaPendientes));

            await _contexto.Database.MigrateAsync(cancelacion);
            _registro.LogInformation("Migraciones aplicadas correctamente.");
        }
        finally
        {
            await using NpgsqlCommand desbloquear = conexionBloqueo.CreateCommand();
            desbloquear.CommandText = "SELECT pg_advisory_unlock($1)";
            desbloquear.Parameters.AddWithValue(ClaveBloqueoMigracion);
            await desbloquear.ExecuteNonQueryAsync(cancelacion);
        }
    }

    private async Task EsperarDisponibilidadAsync(int intentosMaximos, TimeSpan espera, CancellationToken cancelacion)
    {
        for (int intento = 1; intento <= intentosMaximos; intento++)
        {
            try
            {
                if (await _contexto.Database.CanConnectAsync(cancelacion))
                {
                    return;
                }
            }
            catch (NpgsqlException excepcion)
            {
                _registro.LogWarning(
                    "Intento {Intento} de {Total}: PostgreSQL aún no responde ({Motivo}).",
                    intento,
                    intentosMaximos,
                    excepcion.Message);
            }

            await Task.Delay(espera, cancelacion);
        }

        throw new InvalidOperationException(
            $"No fue posible conectarse a PostgreSQL después de {intentosMaximos} intentos.");
    }
}
