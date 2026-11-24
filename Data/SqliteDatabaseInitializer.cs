using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace VetCare.Data;

public static class SqliteDatabaseInitializer
{
    private const string ConsultaIndex = "UX_Consultas_Veterinario_DataHora_Agendada";
    private const string AutoIncrementIdPattern = @"\bId\s+INTEGER\s+PRIMARY\s+KEY\s+AUTOINCREMENT\b";
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

    private static readonly Table SugestoesCatalogo = new("SugestoesCatalogo",
        [new("Nome", "TEXT"), new("EspecieId", "INTEGER", NotNull: false)],
        ["EspecieId → Especies(Id)"]);

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
        var connection = transaction.Connection!;
        var isEmpty = connection.ExecuteScalar<long>("SELECT EXISTS (SELECT 1 FROM main.sqlite_schema);",
            transaction: transaction) == 0;
        var scriptPath = Path.Combine(contentRootPath, "Database", "script.sql");

        if (isEmpty)
        {
            EnsureWritable(readOnly, "criar o esquema");
            connection.Execute(File.ReadAllText(scriptPath), transaction: transaction);
        }

        foreach (var table in LegacySchema)
            ValidateTable(transaction, table);

        if (!isEmpty)
            MigrateMissingTable(transaction, scriptPath, Consultas, readOnly);

        ValidateTable(transaction, Consultas);
        ValidateConsultaConstraints(transaction);
        ValidateConsultaIndex(transaction);

        if (!isEmpty)
            MigrateMissingTable(transaction, scriptPath, SugestoesCatalogo, readOnly);

        ValidateTable(transaction, SugestoesCatalogo, exactColumns: true);
        var suggestionSql = NormalizeSql(GetObjectSql(transaction, "table", SugestoesCatalogo.Name)!);
        if (!Regex.IsMatch(suggestionSql, AutoIncrementIdPattern, SqlPatternOptions))
            throw new InvalidOperationException("Esquema incompatível: SugestoesCatalogo.Id requer AUTOINCREMENT.");
    }

    private static void EnsureWritable(bool readOnly, string operation)
    {
        if (readOnly)
            throw new InvalidOperationException($"O banco está em modo somente leitura. É necessário acesso de escrita para {operation}.");
    }

    private static void MigrateMissingTable(SqliteTransaction transaction, string scriptPath, Table table, bool readOnly)
    {
        if (GetObjectSql(transaction, "table", table.Name) is not null)
            return;

        EnsureWritable(readOnly, $"criar a tabela {table.Name}");
        transaction.Connection!.Execute(ReadTableDdl(scriptPath, table.Name), transaction: transaction);
    }

    private static string ReadTableDdl(string scriptPath, string tableName)
    {
        var script = File.ReadAllText(scriptPath);
        var tableMarker = tableName.ToUpperInvariant();
        var startMarker = $"-- INICIO DDL {tableMarker}";
        var endMarker = $"-- FIM DDL {tableMarker}";
        var start = script.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
        var end = script.IndexOf(endMarker, StringComparison.OrdinalIgnoreCase);
        if (start < 0 || end <= start + startMarker.Length)
            throw new InvalidOperationException($"O bloco de criação de {tableName} está ausente ou inválido em Database/script.sql.");

        return script[(start + startMarker.Length)..end];
    }

    private static void ValidateTable(SqliteTransaction transaction, Table table, bool exactColumns = false)
    {
        if (GetObjectSql(transaction, "table", table.Name) is null)
            throw new InvalidOperationException($"Esquema incompleto: tabela {table.Name} ausente.");

        var connection = transaction.Connection!;
        var allColumns = connection.Query<(string Name, string Type, int NotNull, int PrimaryKey, int Hidden)>("""
            SELECT name, type, "notnull", pk, hidden FROM pragma_table_xinfo(@Table, 'main');
            """, new { Table = table.Name }, transaction).AsList();
        var columns = allColumns.Where(column => column.Hidden == 0)
            .ToDictionary(column => column.Name, StringComparer.OrdinalIgnoreCase);

        // Uma PK INTEGER que não é alias de rowid exige Id explícito em inserções.
        var primaryKeyIndex = connection.ExecuteScalar<int?>("""
            SELECT 1 FROM pragma_index_list(@Table, 'main') WHERE origin = 'pk' LIMIT 1;
            """, new { Table = table.Name }, transaction);
        if (!columns.TryGetValue("Id", out var id) || !id.Type.Equals("INTEGER", StringComparison.OrdinalIgnoreCase)
            || id.PrimaryKey != 1 || primaryKeyIndex is not null)
        {
            throw new InvalidOperationException($"Esquema incompatível: {table.Name}.Id deve ser a chave primária INTEGER gerada pelo SQLite.");
        }

        foreach (var expected in table.Columns)
        {
            if (!columns.TryGetValue(expected.Name, out var actual))
                throw new InvalidOperationException($"Esquema incompleto: coluna {table.Name}.{expected.Name} ausente.");

            if (!actual.Type.Equals(expected.Type, StringComparison.OrdinalIgnoreCase) || (actual.NotNull != 0) != expected.NotNull)
            {
                var nullability = expected.NotNull ? "NOT NULL" : "NULL";
                throw new InvalidOperationException(
                    $"Esquema incompatível: {table.Name}.{expected.Name} deve ser {expected.Type} {nullability}.");
            }
        }

        if (exactColumns && allColumns.Count != table.Columns.Length + 1)
            throw new InvalidOperationException($"Esquema incompatível: {table.Name} contém colunas não previstas.");

        ValidateForeignKeys(transaction, table);
    }

    private static void ValidateForeignKeys(SqliteTransaction transaction, Table table)
    {
        var foreignKeys = new List<string>();
        var connection = transaction.Connection!;
        var keys = connection.Query<(string From, string TargetTable, string? To, int Sequence, string OnUpdate, string OnDelete)>("""
            SELECT "from", "table", "to", seq, on_update, on_delete FROM pragma_foreign_key_list(@Table, 'main');
            """, new { Table = table.Name }, transaction);
        foreach (var key in keys)
        {
            if (key.Sequence != 0 || key.OnUpdate != "NO ACTION" || key.OnDelete != "NO ACTION")
                throw new InvalidOperationException($"Esquema incompatível: chave estrangeira não prevista em {table.Name}.");

            foreignKeys.Add($"{key.From} → {key.TargetTable}({key.To ?? "Id"})");
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

        var invalidReference = connection.ExecuteScalar<int?>("""
            SELECT 1 FROM pragma_foreign_key_check(@Table, 'main') LIMIT 1;
            """, new { Table = table.Name }, transaction);
        if (invalidReference is not null)
            throw new InvalidOperationException($"Banco inconsistente: {table.Name} contém referências sem registro correspondente.");
    }

    private static void ValidateConsultaConstraints(SqliteTransaction transaction)
    {
        var sql = NormalizeSql(GetObjectSql(transaction, "table", Consultas.Name)!);
        if (!Regex.IsMatch(sql, AutoIncrementIdPattern, SqlPatternOptions)
            || !Regex.IsMatch(sql, @"\bCHECK\s*\(\s*Status\s+IN\s*\(\s*0\s*,\s*1\s*,\s*2\s*\)\s*\)", SqlPatternOptions)
            || !Regex.IsMatch(sql, @"\bCHECK\s*\(\s*Urgencia\s+IN\s*\(\s*0\s*,\s*1\s*\)\s*\)", SqlPatternOptions))
        {
            throw new InvalidOperationException(
                "Esquema incompatível: Consultas requer Id com AUTOINCREMENT e os CHECKs de Status e Urgencia.");
        }
    }

    private static void ValidateConsultaIndex(SqliteTransaction transaction)
    {
        var indexedColumns = transaction.Connection!.Query<string?>("""
            SELECT columns.name
            FROM pragma_index_list('Consultas', 'main') AS indexes
            CROSS JOIN pragma_index_xinfo(indexes.name, 'main') AS columns
            WHERE indexes.name = @Index COLLATE NOCASE
              AND indexes."unique" = 1 AND indexes.partial = 1 AND columns."key" = 1
            ORDER BY columns.seqno;
            """, new { Index = ConsultaIndex }, transaction);

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

    private static string? GetObjectSql(SqliteTransaction transaction, string type, string name) =>
        transaction.Connection!.ExecuteScalar<string?>("""
            SELECT sql FROM main.sqlite_schema WHERE type = @Type AND name = @Name COLLATE NOCASE;
            """, new { Type = type, Name = name }, transaction);

    private sealed record Column(string Name, string Type, bool NotNull = true);
    private sealed record Table(string Name, Column[] Columns, string[] ForeignKeys);
}
