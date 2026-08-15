using System.Globalization;
using Npgsql;

namespace Licitaciones.IntegrationTests.Comun;

/// <summary>
/// Consultas directas al catálogo de PostgreSQL.
/// </summary>
/// <remarks>
/// Verificar el esquema con Entity Framework Core solo confirmaría el modelo en memoria. Estas
/// consultas leen lo que el motor realmente creó, que es lo que exige el enunciado.
/// </remarks>
public static class ConsultaSql
{
    /// <summary>
    /// Ejecuta una consulta escalar.
    /// </summary>
    /// <param name="cadenaConexion">Cadena de conexión.</param>
    /// <param name="sentencia">Consulta SQL con parámetros posicionales.</param>
    /// <param name="parametros">Valores de los parámetros.</param>
    /// <returns>El valor escalar, o <see langword="null"/> si la consulta no devolvió filas.</returns>
    public static async Task<object?> EscalarAsync(
        string cadenaConexion,
        string sentencia,
        params object[] parametros)
    {
        await using var conexion = new NpgsqlConnection(cadenaConexion);
        await conexion.OpenAsync();

        await using NpgsqlCommand comando = conexion.CreateCommand();
        comando.CommandText = sentencia;

        foreach (object parametro in parametros ?? [])
        {
            comando.Parameters.Add(new NpgsqlParameter { Value = parametro });
        }

        object? valor = await comando.ExecuteScalarAsync();
        return valor is DBNull ? null : valor;
    }

    /// <summary>
    /// Ejecuta una consulta escalar y devuelve el resultado como texto.
    /// </summary>
    /// <param name="cadenaConexion">Cadena de conexión.</param>
    /// <param name="sentencia">Consulta SQL con parámetros posicionales.</param>
    /// <param name="parametros">Valores de los parámetros.</param>
    /// <returns>El valor como texto, o <see langword="null"/> si no hay resultado.</returns>
    public static async Task<string?> TextoAsync(
        string cadenaConexion,
        string sentencia,
        params object[] parametros)
    {
        object? valor = await EscalarAsync(cadenaConexion, sentencia, parametros);
        return valor is null ? null : Convert.ToString(valor, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Indica si la consulta devuelve al menos una fila.
    /// </summary>
    /// <param name="cadenaConexion">Cadena de conexión.</param>
    /// <param name="sentencia">Consulta SQL con parámetros posicionales.</param>
    /// <param name="parametros">Valores de los parámetros.</param>
    /// <returns><see langword="true"/> si hay resultados.</returns>
    public static async Task<bool> ExisteAsync(
        string cadenaConexion,
        string sentencia,
        params object[] parametros)
    {
        object? valor = await EscalarAsync(cadenaConexion, sentencia, parametros);
        return valor is not null;
    }

    /// <summary>
    /// Ejecuta una sentencia que no devuelve resultados.
    /// </summary>
    /// <param name="cadenaConexion">Cadena de conexión.</param>
    /// <param name="sentencia">Sentencia SQL con parámetros posicionales.</param>
    /// <param name="parametros">Valores de los parámetros.</param>
    /// <returns>Cantidad de filas afectadas.</returns>
    public static async Task<int> EjecutarAsync(
        string cadenaConexion,
        string sentencia,
        params object[] parametros)
    {
        await using var conexion = new NpgsqlConnection(cadenaConexion);
        await conexion.OpenAsync();

        await using NpgsqlCommand comando = conexion.CreateCommand();
        comando.CommandText = sentencia;

        foreach (object parametro in parametros ?? [])
        {
            comando.Parameters.Add(new NpgsqlParameter { Value = parametro });
        }

        return await comando.ExecuteNonQueryAsync();
    }
}
