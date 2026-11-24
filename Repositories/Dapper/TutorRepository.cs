using global::Dapper;
using Microsoft.Data.Sqlite;
using VetCare.Data;
using VetCare.Exceptions;
using VetCare.Models;
using VetCare.Repositories.Interfaces;

namespace VetCare.Repositories.Dapper;

public class TutorRepository : IRepository<Tutor>
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public TutorRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void Adicionar(Tutor tutor)
    {
        const string sql = "INSERT INTO Tutores (Nome, CPF, Telefone) VALUES (@Nome, @Cpf, @Telefone);";

        using var conexao = _connectionFactory.CriarConexao();
        conexao.Execute(sql, new { tutor.Nome, tutor.Cpf, tutor.Telefone });
    }

    public Tutor? ObterPorId(int id)
    {
        const string sql = "SELECT Id, Nome, CPF, Telefone FROM Tutores WHERE Id = @Id;";

        using var conexao = _connectionFactory.CriarConexao();
        return conexao.QuerySingleOrDefault<TutorRegistro>(sql, new { Id = id })?.ParaTutor();
    }

    // Faz a listagem dos tutores por ordem alfabética
    public List<Tutor> Listar()
    {
        const string sql = "SELECT Id, Nome, CPF, Telefone FROM Tutores ORDER BY Nome, Id;";

        using var conexao = _connectionFactory.CriarConexao();
        return conexao.Query<TutorRegistro>(sql).Select(registro => registro.ParaTutor()).ToList();
    }

    public void Atualizar(Tutor tutor)
    {
        const string sql = "UPDATE Tutores SET Nome = @Nome, CPF = @Cpf, Telefone = @Telefone WHERE Id = @Id;";

        using var conexao = _connectionFactory.CriarConexao();
        var alterados = conexao.Execute(sql, new { tutor.Id, tutor.Nome, tutor.Cpf, tutor.Telefone });
        if (alterados == 0)
        {
            throw new RegraNegocioException("O tutor não foi encontrado.");
        }
    }

    public void Excluir(int id)
    {
        const string sql = "DELETE FROM Tutores WHERE Id = @Id;";

        using var conexao = _connectionFactory.CriarConexao();
        try
        {
            if (conexao.Execute(sql, new { Id = id }) == 0)
            {
                throw new RegraNegocioException("O tutor não foi encontrado.");
            }
        }
        // A chave estrangeira do pet protege o vínculo mesmo que ele tenha sido criado após a abertura da confirmação.
        catch (SqliteException ex) when (
            ex.SqliteExtendedErrorCode == 787 ||
            (ex.SqliteExtendedErrorCode == 1811 &&
                ex.Message.Contains("FOREIGN KEY constraint failed", StringComparison.Ordinal)))
        {
            throw new RegraNegocioException("Não é possível excluir um tutor que ainda tem pets vinculados.");
        }
    }

    // O Dapper preenche este registro. O construtor de Tutor concentra a validação e a normalização dos dados.
    private sealed class TutorRegistro
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;

        public Tutor ParaTutor() => new Tutor(Id, Nome, Cpf, Telefone);
    }
}
