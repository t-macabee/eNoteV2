using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace eNote.Tests.TestUtils;

public sealed class CountingCommandInterceptor : DbCommandInterceptor
{
    private readonly List<string> _commands = [];

    public IReadOnlyList<string> Commands => _commands;

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        _commands.Add(command.CommandText);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        _commands.Add(command.CommandText);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public int CountContaining(string sqlFragment) =>
        _commands.Count(sql => sql.Contains(sqlFragment, StringComparison.OrdinalIgnoreCase));

    public void Clear() => _commands.Clear();
}
