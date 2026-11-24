using System.Data.Common;

namespace H2Sharp.EntityFrameworkCore.TestUtilities;

public class TestRelationalCommandBuilderFactory(RelationalCommandBuilderDependencies dependencies) : IRelationalCommandBuilderFactory
{
    public RelationalCommandBuilderDependencies Dependencies { get; } = dependencies;

    public virtual IRelationalCommandBuilder Create()
        => new TestRelationalCommandBuilder(Dependencies);

    private class TestRelationalCommandBuilder(RelationalCommandBuilderDependencies dependencies) : IRelationalCommandBuilder
    {
        private readonly List<IRelationalParameter> _parameters = [];

        public IndentedStringBuilder Instance { get; } = new();

        public RelationalCommandBuilderDependencies Dependencies { get; } = dependencies;

        public IReadOnlyList<IRelationalParameter> Parameters
            => _parameters;

        public IRelationalCommandBuilder AddParameter(IRelationalParameter parameter)
        {
            _parameters.Add(parameter);

            return this;
        }

        public IRelationalCommandBuilder RemoveParameterAt(int index)
        {
            _parameters.RemoveAt(index);

            return this;
        }

        [Obsolete("Code trying to add parameter should add type mapped parameter using TypeMappingSource directly.")]
        public IRelationalTypeMappingSource TypeMappingSource
            => Dependencies.TypeMappingSource;

        public IRelationalCommand Build()
            => new TestRelationalCommand(
                Dependencies,
                Instance.ToString(),
                Parameters);

        public IRelationalCommandBuilder Append(string value)
        {
            Instance.Append(value);

            return this;
        }

        public IRelationalCommandBuilder AppendLine()
        {
            Instance.AppendLine();

            return this;
        }

        public IRelationalCommandBuilder IncrementIndent()
        {
            Instance.IncrementIndent();

            return this;
        }

        public IRelationalCommandBuilder DecrementIndent()
        {
            Instance.DecrementIndent();

            return this;
        }

        public int CommandTextLength
            => Instance.Length;
    }

    // The builder is used for both SQL generation and query templates. A command has to carry the
    // captured SQL/parameters, delegate execution to a real RelationalCommand, and run the failure
    // hook that feeds ExecutionStrategyTest's counters and deferred failures.
    private sealed class TestRelationalCommand(
        RelationalCommandBuilderDependencies dependencies,
        string commandText,
        IReadOnlyList<IRelationalParameter> parameters)
        : IRelationalCommand
    {
        private readonly RelationalCommand _real = new(dependencies, commandText, parameters);

        public string CommandText
            => _real.CommandText;

        public IReadOnlyList<IRelationalParameter> Parameters
            => _real.Parameters;

        public int ExecuteNonQuery(RelationalCommandParameterObject parameterObject)
        {
            var failure = PreExecution(parameterObject.Connection);
            var result = _real.ExecuteNonQuery(parameterObject);
            ThrowIfFailed(parameterObject.Connection, failure);
            return result;
        }

        public async Task<int> ExecuteNonQueryAsync(
            RelationalCommandParameterObject parameterObject,
            CancellationToken cancellationToken = default)
        {
            var failure = PreExecution(parameterObject.Connection);
            var result = await _real.ExecuteNonQueryAsync(parameterObject, cancellationToken).ConfigureAwait(false);
            ThrowIfFailed(parameterObject.Connection, failure);
            return result;
        }

        public object ExecuteScalar(RelationalCommandParameterObject parameterObject)
        {
            var failure = PreExecution(parameterObject.Connection);
            var result = _real.ExecuteScalar(parameterObject);
            ThrowIfFailed(parameterObject.Connection, failure);
            return result;
        }

        public async Task<object> ExecuteScalarAsync(
            RelationalCommandParameterObject parameterObject,
            CancellationToken cancellationToken = default)
        {
            var failure = PreExecution(parameterObject.Connection);
            var result = await _real.ExecuteScalarAsync(parameterObject, cancellationToken).ConfigureAwait(false);
            ThrowIfFailed(parameterObject.Connection, failure);
            return result;
        }

        public RelationalDataReader ExecuteReader(RelationalCommandParameterObject parameterObject)
        {
            var failure = PreExecution(parameterObject.Connection);
            var reader = _real.ExecuteReader(parameterObject);
            if (failure is not null)
            {
                parameterObject.Connection.DbConnection.Close();
                reader.Dispose();
                throw Transient(failure);
            }

            return reader;
        }

        public async Task<RelationalDataReader> ExecuteReaderAsync(
            RelationalCommandParameterObject parameterObject,
            CancellationToken cancellationToken = default)
        {
            var failure = PreExecution(parameterObject.Connection);
            var reader = await _real.ExecuteReaderAsync(parameterObject, cancellationToken).ConfigureAwait(false);
            if (failure is not null)
            {
                parameterObject.Connection.DbConnection.Close();
                reader.Dispose();
                throw Transient(failure);
            }

            return reader;
        }

        public DbCommand CreateDbCommand(RelationalCommandParameterObject parameterObject, Guid commandId, DbCommandMethod commandMethod)
            => throw new NotImplementedException();

        public void PopulateFrom(IRelationalCommandTemplate command)
            => _real.PopulateFrom(command);

        private static string PreExecution(IRelationalConnection connection)
        {
            var testConnection = (TestH2Connection)connection;
            testConnection.ExecutionCount++;

            if (testConnection.ExecutionFailures.Count == 0)
            {
                return null;
            }

            var fail = testConnection.ExecutionFailures.Dequeue();
            if (fail is true)
            {
                testConnection.DbConnection.Close();
                throw Transient(testConnection.ErrorCode);
            }

            // null means normal execution; false means fail after the statement has run.
            return fail is false ? testConnection.ErrorCode : null;
        }

        private static void ThrowIfFailed(IRelationalConnection connection, string failure)
        {
            if (failure is null)
            {
                return;
            }

            connection.DbConnection.Close();
            throw Transient(failure);
        }

        private static H2SharpException Transient(string sqlState)
            => new("Simulated failure", sqlState, 0, SqlExceptionKind.Unspecified, null, null, null);
    }
}
