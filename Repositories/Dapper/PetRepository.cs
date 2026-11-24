using global::Dapper;
using Microsoft.Data.Sqlite;
using VetCare.Data;
using VetCare.Exceptions;
using VetCare.Models;
using VetCare.Models.Enums;
using VetCare.Repositories.Interfaces;

namespace VetCare.Repositories.Dapper;

public class PetRepository : IRepository<Pet>
{
    private const string SelecionarPets = """
        SELECT p.Id, p.Nome, p.DataNascimento, p.Peso, p.Sexo, p.TutorId,
               p.EspecieId, p.RacaId, r.Nome AS NomeRaca, r.EspecieId AS EspecieDaRacaId
        FROM Pets p INNER JOIN Racas r ON r.Id = p.RacaId
        """;

    // Confere tutor, raça e espécie no mesmo comando que grava o pet.
    private const string VinculosValidos = """
        EXISTS (SELECT 1 FROM Tutores WHERE Id = @TutorId)
        AND EXISTS (
            SELECT 1 FROM Racas r INNER JOIN Especies e ON e.Id = r.EspecieId
            WHERE r.Id = @RacaId AND e.Id = @EspecieId
        )
        """;

    private readonly SqliteConnectionFactory _connectionFactory;

    public PetRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void Adicionar(Pet pet)
    {
        const string sql = """
            INSERT INTO Pets (Nome, DataNascimento, Peso, Sexo, TutorId, EspecieId, RacaId)
            SELECT @Nome, @DataNascimento, @Peso, @Sexo, @TutorId, @EspecieId, @RacaId
            WHERE
            """ + " " + VinculosValidos;

        using var conexao = _connectionFactory.CriarConexao();
        if (conexao.Execute(sql, Parametros(pet)) == 0)
            throw new RegraNegocioException("Selecione um tutor cadastrado e uma raça da espécie escolhida.");
    }

    public Pet? ObterPorId(int id)
    {
        using var conexao = _connectionFactory.CriarConexao();
        return conexao.QuerySingleOrDefault<PetRegistro>(
            SelecionarPets + " WHERE p.Id = @Id;", new { Id = id })?.ParaPet();
    }

    public List<Pet> Listar()
    {
        using var conexao = _connectionFactory.CriarConexao();
        return conexao.Query<PetRegistro>(SelecionarPets + " ORDER BY p.Nome, p.Id;")
            .Select(registro => registro.ParaPet())
            .ToList();
    }

    public List<Pet> ListarPorTutor(int tutorId)
    {
        using var conexao = _connectionFactory.CriarConexao();
        return conexao.Query<PetRegistro>(
                SelecionarPets + " WHERE p.TutorId = @TutorId ORDER BY p.Nome, p.Id;", new { TutorId = tutorId })
            .Select(registro => registro.ParaPet())
            .ToList();
    }

    public void Atualizar(Pet pet)
    {
        const string sql = """
            UPDATE Pets
            SET Nome = @Nome, DataNascimento = @DataNascimento, Peso = @Peso,
                Sexo = @Sexo, TutorId = @TutorId, EspecieId = @EspecieId, RacaId = @RacaId
            WHERE Id = @Id AND
            """ + " " + VinculosValidos;

        using var conexao = _connectionFactory.CriarConexao();
        if (conexao.Execute(sql, Parametros(pet)) == 0)
            throw new RegraNegocioException("O pet ou um dos vínculos selecionados não está disponível.");
    }

    public int TransferirParaTutor(int tutorId, IReadOnlyDictionary<int, int> tutoresAtuais)
    {
        if (tutoresAtuais.Count == 0)
            throw new RegraNegocioException("Selecione pelo menos um pet.");

        // Compara o tutor visto na revisão. Se algum vínculo mudou, a transação desfaz o lote inteiro.
        const string sql = """
            UPDATE Pets
            SET TutorId = @TutorId
            WHERE Id = @PetId AND TutorId = @TutorAtualId AND TutorId <> @TutorId
                AND EXISTS (SELECT 1 FROM Tutores WHERE Id = @TutorId);
            """;

        using var conexao = _connectionFactory.CriarConexao();
        using var transacao = conexao.BeginTransaction();
        var parametros = tutoresAtuais.Select(vinculo =>
            new { TutorId = tutorId, PetId = vinculo.Key, TutorAtualId = vinculo.Value });
        var alterados = conexao.Execute(sql, parametros, transacao);
        if (alterados != tutoresAtuais.Count)
            throw new RegraNegocioException("Um ou mais vínculos mudaram ou estão indisponíveis. Revise a seleção novamente.");

        transacao.Commit();
        return alterados;
    }

    public void Excluir(int id)
    {
        const string verificarTabelaConsultas = """
            SELECT EXISTS (SELECT 1 FROM sqlite_schema WHERE type = 'table' AND name = 'Consultas' COLLATE NOCASE);
            """;
        const string excluirPet = "DELETE FROM Pets WHERE Id = @Id";

        using var conexao = _connectionFactory.CriarConexao();
        using var transacao = conexao.BeginTransaction(deferred: false);
        // Preserva o histórico de consultas e aceita bases antigas sem essa tabela.
        var temTabelaConsultas = conexao.ExecuteScalar<int>(verificarTabelaConsultas, transaction: transacao) == 1;
        var sql = temTabelaConsultas
            ? excluirPet + " AND NOT EXISTS (SELECT 1 FROM Consultas WHERE PetId = @Id);"
            : excluirPet + ";";

        if (conexao.Execute(sql, new { Id = id }, transacao) == 0)
        {
            var existe = conexao.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM Pets WHERE Id = @Id;", new { Id = id }, transacao) > 0;
            throw new RegraNegocioException(existe
                ? "Não é possível excluir um pet com histórico de consultas."
                : "O pet não foi encontrado.");
        }

        transacao.Commit();
    }

    public Raca? ObterRacaPorId(int id)
    {
        using var conexao = _connectionFactory.CriarConexao();
        var item = conexao.QuerySingleOrDefault<ItemCatalogo>(
            "SELECT Id, Nome, EspecieId FROM Racas WHERE Id = @Id;", new { Id = id });
        return item is null ? null : new Raca(item.Id, item.Nome, item.EspecieId);
    }

    public List<Especie> ListarEspecies()
    {
        using var conexao = _connectionFactory.CriarConexao();
        return conexao.Query<ItemCatalogo>("SELECT Id, Nome FROM Especies ORDER BY Nome, Id;")
            .Select(item => new Especie(item.Id, item.Nome))
            .ToList();
    }

    public List<Raca> ListarRacas()
    {
        using var conexao = _connectionFactory.CriarConexao();
        return conexao.Query<ItemCatalogo>("SELECT Id, Nome, EspecieId FROM Racas ORDER BY Nome, Id;")
            .Select(item => new Raca(item.Id, item.Nome, item.EspecieId))
            .ToList();
    }

    public void AdicionarEspecie(string nome) => CadastrarItemCatalogo(nome, null);

    public void AdicionarRaca(string nome, int especieId) => CadastrarItemCatalogo(nome, especieId);

    private void CadastrarItemCatalogo(string nome, int? especieId)
    {
        using var conexao = _connectionFactory.CriarConexao();
        // Reserva a escrita antes de consultar o catálogo para evitar cadastros simultâneos do mesmo nome.
        using var transacao = conexao.BeginTransaction(deferred: false);
        if (!InserirNoCatalogo(conexao, transacao, nome.Trim(), especieId))
            throw new RegraNegocioException(especieId.HasValue
                ? "Já existe uma raça com esse nome para a espécie selecionada."
                : "Já existe uma espécie com esse nome.");
        transacao.Commit();
    }

    public bool SugestoesDisponiveis()
    {
        using var conexao = _connectionFactory.CriarConexao();
        return TemTabelaDeSugestoes(conexao);
    }

    public List<SugestaoCatalogo>? ListarSugestoes()
    {
        const string sql = """
            SELECT s.Id, s.Nome, s.EspecieId, e.Nome AS EspecieNome
            FROM SugestoesCatalogo s LEFT JOIN Especies e ON e.Id = s.EspecieId
            ORDER BY s.Id;
            """;

        using var conexao = _connectionFactory.CriarConexao();
        // A lista vazia significa que a fila foi configurada sem itens
        // O null indica que a tabela ainda não existe.
        return TemTabelaDeSugestoes(conexao) ? conexao.Query<SugestaoCatalogo>(sql).ToList() : null;
    }

    public void SugerirItemCatalogo(string nome, int? especieId = null)
    {
        var nomeNormalizado = nome.Trim();
        if (nomeNormalizado.Length == 0)
            throw new RegraNegocioException("Informe o nome da sugestão.");

        using var conexao = _connectionFactory.CriarConexao();
        using var transacao = conexao.BeginTransaction(deferred: false);
        VerificarFilaDeSugestoes(conexao, transacao);

        if (especieId.HasValue && conexao.ExecuteScalar<int>(
                "SELECT EXISTS (SELECT 1 FROM Especies WHERE Id = @EspecieId);",
                new { EspecieId = especieId }, transacao) == 0)
            throw new RegraNegocioException("Selecione uma espécie cadastrada.");

        if (JaEstaNoCatalogo(conexao, transacao, nomeNormalizado, especieId))
            throw new RegraNegocioException("Esse item já está no catálogo.");

        var pendentes = conexao.Query<string>("SELECT Nome FROM SugestoesCatalogo WHERE EspecieId IS @EspecieId;",
            new { EspecieId = especieId }, transacao);
        if (pendentes.Contains(nomeNormalizado, StringComparer.OrdinalIgnoreCase))
            throw new RegraNegocioException("Já existe uma sugestão pendente para esse item.");

        conexao.Execute("INSERT INTO SugestoesCatalogo (Nome, EspecieId) VALUES (@Nome, @EspecieId);",
            new { Nome = nomeNormalizado, EspecieId = especieId }, transacao);
        transacao.Commit();
    }

    public bool AprovarSugestao(int id)
    {
        using var conexao = _connectionFactory.CriarConexao();
        using var transacao = conexao.BeginTransaction(deferred: false);
        VerificarFilaDeSugestoes(conexao, transacao);

        var sugestao = conexao.QuerySingleOrDefault<SugestaoCatalogo>(
            "SELECT Id, Nome, EspecieId FROM SugestoesCatalogo WHERE Id = @Id;", new { Id = id }, transacao)
            ?? throw new RegraNegocioException("Esta sugestão não está mais pendente.");

        // Se o item já foi cadastrado, a aprovação apenas retira a sugestão da fila.
        var inserida = InserirNoCatalogo(conexao, transacao, sugestao.Nome.Trim(), sugestao.EspecieId);
        conexao.Execute("DELETE FROM SugestoesCatalogo WHERE Id = @Id;", new { Id = id }, transacao);
        transacao.Commit();
        return inserida;
    }

    public void ReprovarSugestao(int id)
    {
        using var conexao = _connectionFactory.CriarConexao();
        using var transacao = conexao.BeginTransaction(deferred: false);
        VerificarFilaDeSugestoes(conexao, transacao);

        if (conexao.Execute("DELETE FROM SugestoesCatalogo WHERE Id = @Id;", new { Id = id }, transacao) == 0)
            throw new RegraNegocioException("Esta sugestão não está mais pendente.");
        transacao.Commit();
    }

    private static bool InserirNoCatalogo(SqliteConnection conexao, SqliteTransaction transacao, string nome, int? especieId)
    {
        if (JaEstaNoCatalogo(conexao, transacao, nome, especieId))
            return false;

        var sql = especieId.HasValue
            ? "INSERT INTO Racas (Nome, EspecieId) SELECT @Nome, Id FROM Especies WHERE Id = @EspecieId;"
            : "INSERT INTO Especies (Nome) VALUES (@Nome);";
        if (conexao.Execute(sql, new { Nome = nome, EspecieId = especieId }, transacao) == 0)
            throw new RegraNegocioException("Selecione uma espécie cadastrada.");

        return true;
    }

    private static bool TemTabelaDeSugestoes(SqliteConnection conexao, SqliteTransaction? transacao = null) =>
        conexao.ExecuteScalar<int>("""
            SELECT EXISTS (SELECT 1 FROM sqlite_schema WHERE type = 'table' AND name = 'SugestoesCatalogo' COLLATE NOCASE);
            """, transaction: transacao) == 1;

    private static void VerificarFilaDeSugestoes(SqliteConnection conexao, SqliteTransaction transacao)
    {
        if (!TemTabelaDeSugestoes(conexao, transacao))
            throw new RegraNegocioException("A fila de sugestões ainda não foi configurada no banco de dados.");
    }

    private static bool JaEstaNoCatalogo(SqliteConnection conexao, SqliteTransaction transacao, string nome, int? especieId)
    {
        // O nome de uma espécie é comparado ao catálogo inteiro de espécies. O de uma raça, só às raças da espécie escolhida.
        var sql = especieId.HasValue
            ? "SELECT Nome FROM Racas WHERE EspecieId = @EspecieId;"
            : "SELECT Nome FROM Especies;";
        return conexao.Query<string>(sql, new { EspecieId = especieId }, transacao)
            .Any(item => string.Equals(item.Trim(), nome, StringComparison.OrdinalIgnoreCase));
    }

    private static object Parametros(Pet pet) => new
    {
        pet.Id,
        pet.Nome,
        pet.DataNascimento,
        pet.Peso,
        Sexo = (int)pet.Sexo,
        pet.TutorId,
        pet.EspecieId,
        pet.RacaId
    };

    private sealed class PetRegistro
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime DataNascimento { get; set; }
        public decimal Peso { get; set; }
        public int Sexo { get; set; }
        public int TutorId { get; set; }
        public int EspecieId { get; set; }
        public int RacaId { get; set; }
        public string NomeRaca { get; set; } = string.Empty;
        public int EspecieDaRacaId { get; set; }

        // O Dapper preenche o registro e a entidade é reconstruída pelo construtor validado do domínio.
        public Pet ParaPet() => new(Id, Nome, DataNascimento, Peso, (SexoPet)Sexo,
            TutorId, EspecieId, new Raca(RacaId, NomeRaca, EspecieDaRacaId));
    }

    private sealed class ItemCatalogo
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int EspecieId { get; set; }
    }
}
