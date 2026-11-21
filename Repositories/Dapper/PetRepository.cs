using global::Dapper;
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

    public void Excluir(int id)
    {
        const string sql = """
            DELETE FROM Pets WHERE Id = @Id
            AND NOT EXISTS (SELECT 1 FROM Consultas WHERE PetId = @Id);
            """;

        using var conexao = _connectionFactory.CriarConexao();
        if (conexao.Execute(sql, new { Id = id }) == 0)
        {
            var existe = conexao.ExecuteScalar<int>("SELECT COUNT(*) FROM Pets WHERE Id = @Id;", new { Id = id }) > 0;
            throw new RegraNegocioException(existe
                ? "Não é possível excluir um pet com histórico de consultas."
                : "O pet não foi encontrado.");
        }
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

    public void AdicionarEspecie(string nome)
    {
        using var conexao = _connectionFactory.CriarConexao();
        conexao.Execute("INSERT INTO Especies (Nome) VALUES (@Nome);", new { Nome = nome });
    }

    public void AdicionarRaca(string nome, int especieId)
    {
        const string sql = """
            INSERT INTO Racas (Nome, EspecieId)
            SELECT @Nome, Id FROM Especies WHERE Id = @EspecieId;
            """;

        using var conexao = _connectionFactory.CriarConexao();
        if (conexao.Execute(sql, new { Nome = nome, EspecieId = especieId }) == 0)
        {
            throw new RegraNegocioException("Selecione uma espécie cadastrada.");
        }
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
