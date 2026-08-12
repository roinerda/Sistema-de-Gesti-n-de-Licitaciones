using Licitaciones.Application.Abstracciones;
using Licitaciones.Domain.Comun;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Licitaciones.Infrastructure.Persistencia;

/// <summary>
/// Implementación de la unidad de trabajo sobre Entity Framework Core.
/// </summary>
/// <remarks>
/// Traduce los errores del proveedor de datos a excepciones propias de la aplicación. Así ni el dominio ni
/// los casos de uso conocen PostgreSQL, y los mensajes devueltos al cliente nunca exponen detalles técnicos.
/// </remarks>
public sealed class UnidadDeTrabajo : IUnidadDeTrabajo
{
    private const string EstadoViolacionUnicidad = "23505";
    private const string EstadoViolacionClaveForanea = "23503";
    private const string EstadoViolacionRestriccion = "23514";

    private readonly LicitacionesDbContext _contexto;

    /// <summary>
    /// Crea la unidad de trabajo.
    /// </summary>
    /// <param name="contexto">Contexto de Entity Framework Core.</param>
    public UnidadDeTrabajo(LicitacionesDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    public async Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default)
    {
        try
        {
            return await _contexto.SaveChangesAsync(cancelacion);
        }
        catch (DbUpdateConcurrencyException excepcion)
        {
            throw new ConflictoConcurrenciaException(
                "El registro fue modificado por otra operación.",
                excepcion);
        }
        catch (DbUpdateException excepcion) when (excepcion.InnerException is PostgresException postgres)
        {
            throw TraducirErrorPostgres(postgres, excepcion);
        }
    }

    /// <inheritdoc />
    public void EstablecerVersionOriginal(EntidadBase entidad, int version)
    {
        ArgumentNullException.ThrowIfNull(entidad);
        _contexto.Entry(entidad).Property(e => e.Version).OriginalValue = version;
    }

    /// <inheritdoc />
    public async Task<T> EjecutarEnTransaccionAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(operacion);

        // La estrategia de ejecución permite reintentos ante errores transitorios sin perder la
        // atomicidad: si se reintenta, la transacción completa vuelve a ejecutarse.
        IExecutionStrategy estrategia = _contexto.Database.CreateExecutionStrategy();

        return await estrategia.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaccion = await _contexto.Database.BeginTransactionAsync(cancelacion);

            T resultado = await operacion(cancelacion);

            await transaccion.CommitAsync(cancelacion);
            return resultado;
        });
    }

    private static Exception TraducirErrorPostgres(PostgresException postgres, DbUpdateException original) =>
        postgres.SqlState switch
        {
            EstadoViolacionUnicidad => new ViolacionUnicidadException(
                postgres.ConstraintName,
                "Ya existe un registro con los mismos datos únicos.",
                original),

            EstadoViolacionClaveForanea => new ViolacionIntegridadException(
                postgres.ConstraintName,
                "La operación no se puede completar porque existen registros relacionados.",
                original),

            EstadoViolacionRestriccion => new ViolacionIntegridadException(
                postgres.ConstraintName,
                "Los datos enviados no cumplen una restricción de integridad de la base de datos.",
                original),

            _ => original,
        };
}
