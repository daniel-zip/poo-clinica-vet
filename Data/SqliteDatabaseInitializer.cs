using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace VetCare.Data;

public static class SqliteDatabaseInitializer
{
    private static readonly (string Table, string[] Columns)[] RequiredSchema =
    [
        ("Tutores", ["Id", "Nome", "Cpf", "Telefone"]),
        ("Especies", ["Id", "Nome"]),
        ("Racas", ["Id", "Nome", "EspecieId"]),
        ("Pets", ["Id", "Nome", "DataNascimento", "Peso", "Sexo", "TutorId", "EspecieId", "RacaId"]),
        ("Funcionarios", ["Id", "Tipo", "Nome", "Cpf", "Email", "Telefone", "Endereco", "Salario",
            "NivelAcesso", "Turno", "Crmv", "Especialidade", "Ativo"])
    ];

    public static void Initialize(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A string de conexão 'ConnectionStrings:DefaultConnection' não foi configurada.");

        var connectionString = SqliteConnectionFactory.ResolveConnectionString(
            configuredConnectionString,
            environment.ContentRootPath);
        var databasePath = SqliteConnectionFactory.ObterCaminhoBanco(connectionString);
        var connectionOptions = new SqliteConnectionStringBuilder(connectionString);

        try
        {
            if (databasePath is not null
                && (connectionOptions.Mode != SqliteOpenMode.ReadWriteCreate || File.Exists(databasePath)))
            {
                ValidateExistingDatabase(connectionOptions, databasePath);
                return;
            }

            var scriptPath = Path.Combine(environment.ContentRootPath, "Database", "script.sql");
            var script = File.ReadAllText(scriptPath);

            if (databasePath is not null && !TryReserveDatabaseFile(databasePath))
            {
                ValidateExistingDatabase(connectionOptions, databasePath);
                return;
            }

            ApplyScript(connectionString, script);
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                $"Falha ao inicializar o banco SQLite em '{databasePath ?? connectionOptions.DataSource}': {ex.Message}", ex);
        }
    }

    private static bool TryReserveDatabaseFile(string databasePath)
    {
        if (File.Exists(databasePath))
        {
            return false;
        }

        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            using var file = new FileStream(databasePath, FileMode.CreateNew, FileAccess.Write);
            return true;
        }
        catch (IOException) when (File.Exists(databasePath))
        {
            return false;
        }
    }

    private static void ValidateExistingDatabase(SqliteConnectionStringBuilder connectionOptions, string databasePath)
    {
        connectionOptions.Mode = SqliteOpenMode.ReadOnly;
        using var connection = new SqliteConnection(connectionOptions.ToString());
        connection.Open();

        var missing = new List<string>();
        foreach (var (table, requiredColumns) in RequiredSchema)
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT columns.name
                FROM main.sqlite_schema AS objects
                CROSS JOIN pragma_table_info(objects.name, 'main') AS columns
                WHERE objects.type = 'table' AND objects.name = @Table COLLATE NOCASE;
                """;
            command.Parameters.AddWithValue("@Table", table);

            using var reader = command.ExecuteReader();
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
            {
                columns.Add(reader.GetString(0));
            }

            if (columns.Count == 0)
            {
                missing.Add($"tabela {table}");
                continue;
            }

            missing.AddRange(requiredColumns
                .Where(column => !columns.Contains(column))
                .Select(column => $"coluna {table}.{column}"));
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"O banco SQLite existente em '{databasePath}' tem estrutura incompleta. Itens ausentes: {string.Join(", ", missing)}.");
        }
    }

    private static void ApplyScript(string connectionString, string script)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        // A string de conexão já habilita as chaves estrangeiras antes da transação.
        // Se a execução falhar, o descarte da transação reverte as alterações.
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = script;
        command.ExecuteNonQuery();
        transaction.Commit();
    }
}
