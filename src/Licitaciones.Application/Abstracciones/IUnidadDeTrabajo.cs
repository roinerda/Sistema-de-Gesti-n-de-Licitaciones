using Licitaciones.Domain.Comun;

namespace Licitaciones.Application.Abstracciones;

/// <summary>
/// Puerto de persistencia transaccional.
/// </summary>
/// <remarks>
/// La capa de aplicación coordina cambios en varias entidades y confirma una sola vez. La implementación
/// traduce los errores de Entity Framework Core a <see cref="ConflictoConcurrenciaException"/> y
/// <see cref="ViolacionUnicidadException"/> para que el dominio y los casos de uso no conozcan el ORM.
/// </remarks>
public interface IUnidadDeTrabajo
{
    /// <summary>
    /// Confirma los cambios pendientes.
    /// </summary>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>Cantidad de registros afectados.</returns>
    /// <exception cref="ConflictoConcurrenciaException">Si otro proceso modificó los registros.</exception>
    /// <exception cref="ViolacionUnicidadException">Si se viola un índice único de la base de datos.</exception>
    Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Declara cuál era la versión que el cliente tenía a la vista al editar el registro.
    /// </summary>
    /// <param name="entidad">Entidad que se está modificando.</param>
    /// <param name="version">Versión recibida desde el formulario o desde el cuerpo de la petición.</param>
    /// <remarks>
    /// Sin esta llamada, la comparación de concurrencia usa la versión leída milisegundos antes de guardar y
    /// solo detecta conflictos simultáneos. Al fijar la versión del cliente, también se detecta el caso real:
    /// dos personas abren el mismo formulario y la segunda guarda sobre cambios que nunca vio.
    /// </remarks>
    void EstablecerVersionOriginal(EntidadBase entidad, int version);

    /// <summary>
    /// Ejecuta una operación que afecta varios registros dentro de una transacción explícita.
    /// </summary>
    /// <typeparam name="T">Tipo del valor devuelto por la operación.</typeparam>
    /// <param name="operacion">Operación a ejecutar.</param>
    /// <param name="cancelacion">Token de cancelación.</param>
    /// <returns>El valor devuelto por la operación.</returns>
    Task<T> EjecutarEnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken cancelacion = default);
}

/// <summary>
/// Señala que otro proceso modificó el registro entre la lectura y la escritura.
/// </summary>
public sealed class ConflictoConcurrenciaException : Exception
{
    /// <summary>Crea la excepción con un mensaje seguro.</summary>
    /// <param name="mensaje">Mensaje descriptivo.</param>
    /// <param name="interna">Excepción original del proveedor de datos.</param>
    public ConflictoConcurrenciaException(string mensaje, Exception? interna = null)
        : base(mensaje, interna)
    {
    }
}

/// <summary>
/// Señala la violación de un índice único de la base de datos.
/// </summary>
/// <remarks>
/// La unicidad se valida antes de guardar, pero dos peticiones simultáneas pueden pasar esa validación;
/// PostgreSQL es la última línea de defensa y su error se traduce a un mensaje controlado.
/// </remarks>
public sealed class ViolacionUnicidadException : Exception
{
    /// <summary>Crea la excepción indicando el índice involucrado.</summary>
    /// <param name="nombreIndice">Nombre del índice único violado.</param>
    /// <param name="mensaje">Mensaje descriptivo.</param>
    /// <param name="interna">Excepción original del proveedor de datos.</param>
    public ViolacionUnicidadException(string? nombreIndice, string mensaje, Exception? interna = null)
        : base(mensaje, interna) => NombreIndice = nombreIndice;

    /// <summary>Nombre del índice único que la base de datos rechazó.</summary>
    public string? NombreIndice { get; }
}

/// <summary>
/// Señala la violación de una clave foránea o de una restricción CHECK de la base de datos.
/// </summary>
/// <remarks>
/// Permite cumplir el requisito de traducir los errores de integridad referencial a mensajes controlados
/// (sección 8.9) sin exponer el nombre del motor, la consulta ni la traza.
/// </remarks>
public sealed class ViolacionIntegridadException : Exception
{
    /// <summary>Crea la excepción indicando la restricción involucrada.</summary>
    /// <param name="nombreRestriccion">Nombre de la restricción violada.</param>
    /// <param name="mensaje">Mensaje descriptivo.</param>
    /// <param name="interna">Excepción original del proveedor de datos.</param>
    public ViolacionIntegridadException(string? nombreRestriccion, string mensaje, Exception? interna = null)
        : base(mensaje, interna) => NombreRestriccion = nombreRestriccion;

    /// <summary>Nombre de la restricción de integridad que la base de datos rechazó.</summary>
    public string? NombreRestriccion { get; }
}
