using System.Data.Common;
using System.Text.Json;

namespace DirectorPrompt.Infrastructure;

public static class RowReader
{
    public static async Task<Dictionary<string, object?>?> ReadRowAsync
    (
        DbConnection     connection,
        string           sql,
        object?          param           = null,
        DbTransaction?   transaction     = null,
        CancellationToken cancellationToken = default
    )
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        if (transaction is not null)
            command.Transaction = transaction;

        AddParameters(command, param);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var row = new Dictionary<string, object?>();

        for (var i = 0; i < reader.FieldCount; i++)
            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

        return row;
    }

    public static async Task<string?> ReadRowAsJSONAsync
    (
        DbConnection     connection,
        string           sql,
        object?          param           = null,
        DbTransaction?   transaction     = null,
        CancellationToken cancellationToken = default
    )
    {
        var row = await ReadRowAsync(connection, sql, param, transaction, cancellationToken);

        return row is null ? null : JsonSerializer.Serialize(row);
    }

    private static void AddParameters(DbCommand command, object? param)
    {
        if (param is null)
            return;

        var properties = param.GetType().GetProperties();

        foreach (var prop in properties)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"@{prop.Name}";
            parameter.Value = prop.GetValue(param) ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }
}
