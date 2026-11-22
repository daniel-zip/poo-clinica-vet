using global::Dapper;
using VetCare.Data;
using VetCare.Exceptions;
using VetCare.Models;
using VetCare.Repositories.Interfaces;
using VetCare.Services;

namespace VetCare.Repositories.Dapper;

public class FuncionarioRepository : IRepository<Funcionario>
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public FuncionarioRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void Adicionar(Funcionario entidade)
    {
        const string sql = """
            INSERT INTO Funcionarios (
                Tipo, Nome, Cpf, Email, Telefone, Endereco, Salario, NivelAcesso, Turno, Crmv, Especialidade, Ativo
            ) VALUES (
                @Tipo, @Nome, @Cpf, @Email, @Telefone, @Endereco, @Salario, @NivelAcesso, @Turno, @Crmv, @Especialidade, @Ativo
            );
            """;

        using var conexao = _connectionFactory.CriarConexao();
        conexao.Execute(sql, CriarParametros(entidade));
    }

    public Funcionario? ObterPorId(int id)
    {
        const string sql = """
            SELECT Id, Tipo, Nome, Cpf, Email, Telefone, Endereco, Salario, NivelAcesso, Turno, Crmv, Especialidade, Ativo
            FROM Funcionarios
            WHERE Id = @Id;
            """;

        using var conexao = _connectionFactory.CriarConexao();
        var registro = conexao.QuerySingleOrDefault<FuncionarioRegistro>(sql, new { Id = id });
        return registro?.ParaFuncionario();
    }

    public List<Funcionario> Listar()
    {
        const string sql = """
            SELECT Id, Tipo, Nome, Cpf, Email, Telefone, Endereco, Salario, NivelAcesso, Turno, Crmv, Especialidade, Ativo
            FROM Funcionarios
            ORDER BY Nome, Id;
            """;

        using var conexao = _connectionFactory.CriarConexao();
        return conexao.Query<FuncionarioRegistro>(sql)
            .Select(registro => registro.ParaFuncionario())
            .ToList();
    }

    public void Atualizar(Funcionario entidade)
    {
        const string sqlTipo = "SELECT Tipo FROM Funcionarios WHERE Id = @Id;";
        const string sqlAtualizacao = """
            UPDATE Funcionarios
            SET Nome = @Nome,
                Cpf = @Cpf,
                Email = @Email,
                Telefone = @Telefone,
                Endereco = @Endereco,
                Salario = @Salario,
                NivelAcesso = @NivelAcesso,
                Turno = @Turno,
                Crmv = @Crmv,
                Especialidade = @Especialidade
            WHERE Id = @Id AND Tipo = @Tipo;
            """;

        using var conexao = _connectionFactory.CriarConexao();
        var tipoPersistido = conexao.QuerySingleOrDefault<string>(sqlTipo, new { entidade.Id });
        if (tipoPersistido is null)
        {
            throw new RegraNegocioException("O funcionário não foi encontrado.");
        }

        var tipoEntidade = ObterTipo(entidade);
        if (!string.Equals(tipoPersistido, tipoEntidade, StringComparison.Ordinal))
        {
            throw new RegraNegocioException("Não é permitido alterar o tipo/subclasse do funcionário.");
        }

        var linhasAfetadas = conexao.Execute(sqlAtualizacao, CriarParametros(entidade));
        if (linhasAfetadas == 0)
        {
            throw new RegraNegocioException("O funcionário não foi encontrado.");
        }
    }

    public void Excluir(int id)
    {
        const string sqlConsulta = "SELECT Ativo FROM Funcionarios WHERE Id = @Id;";
        const string sqlDesativacao = "UPDATE Funcionarios SET Ativo = 0 WHERE Id = @Id;";

        using var conexao = _connectionFactory.CriarConexao();
        var ativo = conexao.QuerySingleOrDefault<int?>(sqlConsulta, new { Id = id });
        if (ativo is null)
        {
            throw new RegraNegocioException("O funcionário não foi encontrado.");
        }

        if (ativo == 0)
        {
            throw new RegraNegocioException("O funcionário já está desativado.");
        }

        var linhasAfetadas = conexao.Execute(sqlDesativacao, new { Id = id });
        if (linhasAfetadas == 0)
        {
            throw new RegraNegocioException("O funcionário não foi encontrado.");
        }
    }

    private static object CriarParametros(Funcionario entidade)
    {
        return entidade switch
        {
            Recepcionista recepcionista => new
            {
                Tipo = "Recepcionista",
                recepcionista.Id,
                recepcionista.Nome,
                recepcionista.Cpf,
                recepcionista.Email,
                recepcionista.Telefone,
                recepcionista.Endereco,
                recepcionista.Salario,
                recepcionista.NivelAcesso,
                Turno = (string?)recepcionista.Turno,
                Crmv = (string?)null,
                Especialidade = (string?)null,
                Ativo = recepcionista.Ativo ? 1 : 0
            },
            Veterinario veterinario => new
            {
                Tipo = "Veterinario",
                veterinario.Id,
                veterinario.Nome,
                veterinario.Cpf,
                veterinario.Email,
                veterinario.Telefone,
                veterinario.Endereco,
                veterinario.Salario,
                veterinario.NivelAcesso,
                Turno = (string?)null,
                Crmv = (string?)veterinario.Crmv,
                Especialidade = (string?)veterinario.Especialidade,
                Ativo = veterinario.Ativo ? 1 : 0
            },
            Administrador administrador => new
            {
                Tipo = "Administrador",
                administrador.Id,
                administrador.Nome,
                administrador.Cpf,
                administrador.Email,
                administrador.Telefone,
                administrador.Endereco,
                administrador.Salario,
                administrador.NivelAcesso,
                Turno = (string?)null,
                Crmv = (string?)null,
                Especialidade = (string?)null,
                Ativo = administrador.Ativo ? 1 : 0
            },
            _ => throw new RegraNegocioException("Tipo de funcionário não suportado para persistência.")
        };
    }

    private static string ObterTipo(Funcionario entidade)
    {
        return entidade switch
        {
            Recepcionista => "Recepcionista",
            Veterinario => "Veterinario",
            Administrador => "Administrador",
            _ => throw new RegraNegocioException("Tipo de funcionário não suportado para persistência.")
        };
    }

    private sealed class FuncionarioRegistro
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Endereco { get; set; } = string.Empty;
        public double Salario { get; set; }
        public int NivelAcesso { get; set; }
        public string? Turno { get; set; }
        public string? Crmv { get; set; }
        public string? Especialidade { get; set; }
        public int Ativo { get; set; }

        public Funcionario ParaFuncionario()
        {
            return FuncionarioFactory.Criar(
                Tipo,
                Id,
                Nome,
                Cpf,
                Email,
                Telefone,
                Endereco,
                Salario,
                NivelAcesso,
                Ativo != 0,
                Turno,
                Crmv,
                Especialidade);
        }
    }
}
