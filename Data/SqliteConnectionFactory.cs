using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace VetCare.Data;

public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A string de conexão 'ConnectionStrings:DefaultConnection' não foi configurada.");

        _connectionString = ResolveConnectionString(configuredConnectionString, environment.ContentRootPath);
    }

    public string ConnectionString => _connectionString;

    public static string ResolveConnectionString(string connectionString, string contentRootPath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);

        if (!string.IsNullOrWhiteSpace(builder.DataSource)
            && !string.Equals(builder.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase)
            && !Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.GetFullPath(Path.Combine(contentRootPath, builder.DataSource));
        }

        builder.ForeignKeys = true;
        return builder.ToString();
    }

    public static string? ObterCaminhoBanco(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource)
            || string.Equals(builder.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return builder.DataSource;
    }

    public SqliteConnection CriarConexao()
    {
        var conexao = new SqliteConnection(_connectionString);
        conexao.Open();
        return conexao;
    }
}