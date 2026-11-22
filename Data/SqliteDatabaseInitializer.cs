using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace VetCare.Data;

public static class SqliteDatabaseInitializer
{
    private const string ConsultaIndex = "UX_Consultas_Veterinario_DataHora_Agendada";
    private const string ConsultaDdlStart = "-- INICIO DDL CONSULTAS";
    private const string ConsultaDdlEnd = "-- FIM DDL CONSULTAS";
    private const RegexOptions SqlPatternOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    private static readonly Table[] LegacySchema =
    [
        new("Tutores", [new("Nome", "TEXT"), new("Cpf", "TEXT"), new("Telefone", "TEXT")], []),
        new("Especies", [new("Nome", "TEXT")], []),
        new("Racas", [new("Nome", "TEXT"), new("EspecieId", "INTEGER")], ["EspecieId → Especies(Id)"]),
        new("Pets",
            [new("Nome", "TEXT"), new("DataNascimento", "TEXT"), new("Peso", "REAL"), new("Sexo", "INTEGER"),
                new("TutorId", "INTEGER"), new("EspecieId", "INTEGER"), new("RacaId", "INTEGER")],
            ["TutorId → Tutores(Id)", "EspecieId → Especies(Id)", "RacaId → Racas(Id)"]),
        new("Funcionarios",
            [new("Tipo", "TEXT"), new("Nome", "TEXT"), new("Cpf", "TEXT"), new("Email", "TEXT"),
                new("Telefone", "TEXT"), new("Endereco", "TEXT"), new("Salario", "REAL"),
                new("NivelAcesso", "INTEGER"), new("Turno", "TEXT", NotNull: false),
                new("Crmv", "TEXT", NotNull: false), new("Especialidade", "TEXT", NotNull: false),
                new("Ativo", "INTEGER")], [])
    ];

    private static readonly Table Consultas = new("Consultas",
        [new("PetId", "INTEGER"), new("VeterinarioId", "INTEGER"), new("DataHora", "TEXT"),
            new("Status", "INTEGER"), new("QueixaInicial", "TEXT"), new("Urgencia", "INTEGER"),
            new("Diagnostico", "TEXT", NotNull: false), new("Procedimentos", "TEXT", NotNull: false),
            new("Prescricao", "TEXT", NotNull: false)],
        ["PetId → Pets(Id)", "VeterinarioId → Funcionarios(Id)"]);

    public static void Initialize(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "A string de conexão 'ConnectionStrings:DefaultConnection' não foi configurada.");

        var connectionString = SqliteConnectionFactory.ResolveConnectionString(
            configuredConnectionString, environment.ContentRootPath);
        var databasePath = SqliteConnectionFactory.ObterCaminhoBanco(connectionString);
        var connectionOptions = new SqliteConnectionStringBuilder(connectionString);

        try
        {
            if (connectionOptions.Mode == SqliteOpenMode.ReadWriteCreate
                && Path.GetDirectoryName(databasePath) is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
            }

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // O SQLite serializa inicializações concorrentes antes da leitura do esquema.
            var readOnly = connectionOptions.Mode == SqliteOpenMode.ReadOnly;
            using var transaction = connection.BeginTransaction(deferred: readOnly);
            InitializeSchema(transaction, environment.ContentRootPath, readOnly);
            transaction.Commit();
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Falha ao inicializar o banco SQLite em '{databasePath ?? connectionOptions.DataSource}': {ex.Message}", ex);
        }
    }

    private static void InitializeSchema(SqliteTransaction transaction, string contentRootPath, bool readOnly)
    {
        using var hasObjects = CreateCommand(transaction, "SELECT EXISTS (SELECT 1 FROM main.sqlite_schema);");
        var isEmpty = (long)hasObjects.ExecuteScalar()! == 0;
        var scriptPath = Path.Combine(contentRootPath, "Database", "script.sql");

        if (isEmpty)
        {
            EnsureWritable(readOnly, "criar o esquema");
            using var create = CreateCommand(transaction, File.ReadAllText(scriptPath));
            create.ExecuteNonQuery();
        }

        foreach (var table in LegacySchema)
            ValidateTable(transaction, table);

        if (!isEmpty && GetObjectSql(transaction, "table", Consultas.Name) is null)
        {
            EnsureWritable(readOnly, "migrar o esquema legado para Consultas");
            using var migrate = CreateCommand(transaction, ReadConsultaDdl(scriptPath));
            migrate.ExecuteNonQuery();
        }

        ValidateTable(transaction, Consultas);
        ValidateConsultaConstraints(transaction);
        ValidateConsultaIndex(transaction);
    }

    private static void EnsureWritable(bool readOnly, string operation)
    {
        if (readOnly)
            throw new InvalidOperationException($"O banco está em modo somente leitura. É necessário acesso de escrita para {operation}.");
    }

    private static string ReadConsultaDdl(string scriptPath)
    {
        var script = File.ReadAllText(scriptPath);
        var start = script.IndexOf(ConsultaDdlStart, StringComparison.Ordinal);
        var end = script.IndexOf(ConsultaDdlEnd, StringComparison.Ordinal);
        if (start < 0 || end <= start + ConsultaDdlStart.Length)
            throw new InvalidOperationException("O bloco de criação de Consultas está ausente ou inválido em Database/script.sql.");

        return script[(start + ConsultaDdlStart.Length)..end];
    }

    private static void ValidateTable(SqliteTransaction transaction, Table table)
    {
        if (GetObjectSql(transaction, "table", table.Name) is null)
            throw new InvalidOperationException($"Esquema incompleto: tabela {table.Name} ausente.");

        var columns = new Dictionary<string, (string Type, bool NotNull, int PrimaryKey)>(StringComparer.OrdinalIgnoreCase);
        using (var command = CreateCommand(transaction, """
            SELECT name, type, "notnull", pk FROM pragma_table_info(@Table, 'main');
            """))
        {
            command.Parameters.AddWithValue("@Table", table.Name);
            using var reader = command.ExecuteReader();
            while (reader.Read())
                columns.Add(reader.GetString(0), (reader.GetString(1), reader.GetBoolean(2), reader.GetInt32(3)));
        }

        // Uma PK INTEGER que não é alias de rowid exige Id explícito em inserções.
        using var primaryKeyIndex = CreateCommand(transaction, """
            SELECT 1 FROM pragma_index_list(@Table, 'main') WHERE origin = 'pk' LIMIT 1;
            """);
        primaryKeyIndex.Parameters.AddWithValue("@Table", table.Name);
        if (!columns.TryGetValue("Id", out var id) || !id.Type.Equals("INTEGER", StringComparison.OrdinalIgnoreCase)
            || id.PrimaryKey != 1 || primaryKeyIndex.ExecuteScalar() is not null)
        {
            throw new InvalidOperationException($"Esquema incompatível: {table.Name}.Id deve ser a chave primária INTEGER gerada pelo SQLite.");
        }

        foreach (var expected in table.Columns)
        {
            if (!columns.TryGetValue(expected.Name, out var actual))
                throw new InvalidOperationException($"Esquema incompleto: coluna {table.Name}.{expected.Name} ausente.");

            if (!actual.Type.Equals(expected.Type, StringComparison.OrdinalIgnoreCase) || actual.NotNull != expected.NotNull)
            {
                var nullability = expected.NotNull ? "NOT NULL" : "NULL";
                throw new InvalidOperationException(
                    $"Esquema incompatível: {table.Name}.{expected.Name} deve ser {expected.Type} {nullability}.");
            }
        }

        ValidateForeignKeys(transaction, table);
    }

    private static void ValidateForeignKeys(SqliteTransaction transaction, Table table)
    {
        var foreignKeys = new List<string>();
        using (var command = CreateCommand(transaction, """
            SELECT "from", "table", "to", seq, on_update, on_delete FROM pragma_foreign_key_list(@Table, 'main');
            """))
        {
            command.Parameters.AddWithValue("@Table", table.Name);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetInt32(3) != 0 || reader.GetString(4) != "NO ACTION" || reader.GetString(5) != "NO ACTION")
                    throw new InvalidOperationException($"Esquema incompatível: chave estrangeira não prevista em {table.Name}.");

                var targetColumn = reader.IsDBNull(2) ? "Id" : reader.GetString(2);
                foreignKeys.Add($"{reader.GetString(0)} → {reader.GetString(1)}({targetColumn})");
            }
        }

        if (foreignKeys.Count != table.ForeignKeys.Length
            || !foreignKeys.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(table.ForeignKeys))
        {
            var expected = table.ForeignKeys.Length == 0 ? "nenhuma" : string.Join(", ", table.ForeignKeys);
            throw new InvalidOperationException(
                $"Esquema incompatível: chaves estrangeiras esperadas em {table.Name}: {expected}.");
        }

        if (foreignKeys.Count == 0)
            return;

        using var check = CreateCommand(transaction, """
            SELECT 1 FROM pragma_foreign_key_check(@Table, 'main') LIMIT 1;
            """);
        check.Parameters.AddWithValue("@Table", table.Name);
        if (check.ExecuteScalar() is not null)
            throw new InvalidOperationException($"Banco inconsistente: {table.Name} contém referências sem registro correspondente.");
    }

    private static void ValidateConsultaConstraints(SqliteTransaction transaction)
    {
        var sql = NormalizeSql(GetObjectSql(transaction, "table", Consultas.Name)!);
        if (!Regex.IsMatch(sql, @"\bId\s+INTEGER\s+PRIMARY\s+KEY\s+AUTOINCREMENT\b", SqlPatternOptions)
            || !Regex.IsMatch(sql, @"\bCHECK\s*\(\s*Status\s+IN\s*\(\s*0\s*,\s*1\s*,\s*2\s*\)\s*\)", SqlPatternOptions)
            || !Regex.IsMatch(sql, @"\bCHECK\s*\(\s*Urgencia\s+IN\s*\(\s*0\s*,\s*1\s*\)\s*\)", SqlPatternOptions))
        {
            throw new InvalidOperationException(
                "Esquema incompatível: Consultas requer Id com AUTOINCREMENT e os CHECKs de Status e Urgencia.");
        }
    }

    private static void ValidateConsultaIndex(SqliteTransaction transaction)
    {
        var indexedColumns = new List<string?>();
        using (var command = CreateCommand(transaction, """
            SELECT columns.name
            FROM pragma_index_list('Consultas', 'main') AS indexes
            CROSS JOIN pragma_index_xinfo(indexes.name, 'main') AS columns
            WHERE indexes.name = @Index COLLATE NOCASE
              AND indexes."unique" = 1 AND indexes.partial = 1 AND columns."key" = 1
            ORDER BY columns.seqno;
            """))
        {
            command.Parameters.AddWithValue("@Index", ConsultaIndex);
            using var reader = command.ExecuteReader();
            while (reader.Read())
                indexedColumns.Add(reader.IsDBNull(0) ? null : reader.GetString(0));
        }

        var sql = GetObjectSql(transaction, "index", ConsultaIndex);
        if (!indexedColumns.SequenceEqual(["VeterinarioId", "DataHora"], StringComparer.OrdinalIgnoreCase)
            || sql is null
            || !Regex.IsMatch(NormalizeSql(sql), @"\bWHERE\s+(?:\(\s*)*Status\s*=\s*0\s*(?:\)\s*)*;?\s*\z", SqlPatternOptions))
        {
            throw new InvalidOperationException(
                $"Esquema incompleto ou incompatível: esperado índice único {ConsultaIndex} " +
                "em Consultas (VeterinarioId, DataHora) WHERE Status = 0.");
        }
    }

    // Ignora comentários e conteúdo de literais ao verificar as cláusulas armazenadas em sqlite_schema.
    private static string NormalizeSql(string sql) =>
        Regex.Replace(sql, """--[^\r\n]*|/\*[\s\S]*?\*/|'(?:''|[^'])*'|"(?:""|[^"])*"|`(?:``|[^`])*`|\[[^\]]*\]""", match =>
        {
            if (match.Value[0] is '-' or '/')
                return " ";

            var identifier = match.Value[1..^1];
            return match.Value[0] != '\'' && Regex.IsMatch(identifier, @"\A[A-Za-z_][A-Za-z0-9_]*\z")
                ? identifier
                : "''";
        });

    private static string? GetObjectSql(SqliteTransaction transaction, string type, string name)
    {
        using var command = CreateCommand(transaction, """
            SELECT sql FROM main.sqlite_schema WHERE type = @Type AND name = @Name COLLATE NOCASE;
            """);
        command.Parameters.AddWithValue("@Type", type);
        command.Parameters.AddWithValue("@Name", name);
        return command.ExecuteScalar() as string;
    }

    private static SqliteCommand CreateCommand(SqliteTransaction transaction, string sql)
    {
        var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return command;
    }

    private sealed record Column(string Name, string Type, bool NotNull = true);
    private sealed record Table(string Name, Column[] Columns, string[] ForeignKeys);
}
