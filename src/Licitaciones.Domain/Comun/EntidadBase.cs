namespace Licitaciones.Domain.Comun;

/// <summary>
/// Raíz común de todas las entidades persistidas.
/// </summary>
/// <remarks>
/// Concentra el identificador, los campos de auditoría y el testigo de concurrencia optimista.
/// Los establecedores son protegidos para que el estado solo cambie mediante métodos de dominio;
/// Entity Framework Core puede escribirlos porque accede a los campos de respaldo.
/// </remarks>
public abstract class EntidadBase
{
    /// <summary>
    /// Identificador único generado por el sistema. Nunca es editable por el usuario.
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Instante de creación del registro, en UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; protected set; }

    /// <summary>
    /// Instante de la última modificación del registro, en UTC.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; protected set; }

    /// <summary>
    /// Testigo de concurrencia optimista. Se incrementa en cada modificación y se compara contra el valor
    /// almacenado al guardar; si no coincide, Entity Framework Core detecta el conflicto.
    /// </summary>
    public int Version { get; protected set; } = 1;

    /// <summary>
    /// Asigna identificador y marcas de auditoría iniciales. Solo debe invocarse desde las
    /// fábricas de las entidades derivadas.
    /// </summary>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    protected void InicializarAuditoria(DateTimeOffset ahora)
    {
        Id = Guid.NewGuid();
        CreatedAt = ahora.ToUniversalTime();
        UpdatedAt = CreatedAt;
        Version = 1;
    }

    /// <summary>
    /// Registra que la entidad fue modificada en el instante indicado y avanza el testigo de concurrencia.
    /// </summary>
    /// <param name="ahora">Instante actual provisto por <see cref="IReloj"/>.</param>
    protected void RegistrarActualizacion(DateTimeOffset ahora)
    {
        UpdatedAt = ahora.ToUniversalTime();

        // El desbordamiento es inalcanzable en la práctica, pero se evita explícitamente para que la
        // versión nunca retroceda a un valor ya utilizado.
        Version = Version == int.MaxValue ? 1 : Version + 1;
    }
}
