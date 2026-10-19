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

}
