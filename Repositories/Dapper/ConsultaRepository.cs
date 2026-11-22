using System.Globalization;
using global::Dapper;
using Microsoft.Data.Sqlite;
using VetCare.Data;
using VetCare.Exceptions;
using VetCare.Models;
using VetCare.Models.Enums;
using VetCare.Repositories.Interfaces;

namespace VetCare.Repositories.Dapper;

public class ConsultaRepository : IConsultaRepository
{
    private const string FormatoDataHora = "yyyy-MM-dd HH:mm:ss";
    private const string MensagemConflito = "O veterinário já tem uma consulta agendada nesse horário.";
    private const int SqliteConstraintForeignKey = 787;
    private const int SqliteConstraintUnique = 2067;

    private const string SelecionarConsultas = """
        SELECT Id, PetId, VeterinarioId, DataHora, Status, QueixaInicial, Urgencia,
               Diagnostico, Procedimentos, Prescricao
        FROM Consultas
        """;

    private readonly SqliteConnectionFactory _connectionFactory;

    public ConsultaRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void Adicionar(Consulta entidade)
    {
        const string sql = """
            INSERT INTO Consultas (PetId, VeterinarioId, DataHora, Status, QueixaInicial, Urgencia,
                                   Diagnostico, Procedimentos, Prescricao)
            VALUES (@PetId, @VeterinarioId, @DataHora, @Status, @QueixaInicial, @Urgencia,
                    @Diagnostico, @Procedimentos, @Prescricao);
            """;

        var parametros = new
        {
            entidade.PetId,
            entidade.VeterinarioId,
            DataHora = FormatarDataHora(entidade.DataHora),
            Status = (int)entidade.Status,
            entidade.QueixaInicial,
            Urgencia = entidade.Urgencia ? 1 : 0,
            entidade.Diagnostico,
            entidade.Procedimentos,
            entidade.Prescricao
        };

        try
        {
            ExecutarGravacao(sql, parametros);
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == SqliteConstraintForeignKey)
        {
            throw new RegraNegocioException(
                "O pet ou o veterinário selecionado deixou de estar disponível. Revise a seleção e tente novamente.");
        }
    }

    public Consulta? ObterPorId(int id)
    {
        using var conexao = _connectionFactory.CriarConexao();
        return conexao.QuerySingleOrDefault<ConsultaRegistro>(
            SelecionarConsultas + " WHERE Id = @Id;", new { Id = id })?.ParaConsulta();
    }

    public List<Consulta> Listar()
    {
        using var conexao = _connectionFactory.CriarConexao();
        return conexao.Query<ConsultaRegistro>(SelecionarConsultas + " ORDER BY DataHora, Id;")
            .Select(registro => registro.ParaConsulta())
            .ToList();
    }

    public void Atualizar(Consulta entidade)
    {
        var alteracoes = new List<string>();
        var valoresOriginais = new List<string>();
        IncluirCampo(entidade.HorarioAlterado, "DataHora", "DataHoraOriginal");
        IncluirCampo(entidade.DiagnosticoAlterado, "Diagnostico", "DiagnosticoOriginal");
        IncluirCampo(entidade.ProcedimentosAlterados, "Procedimentos", "ProcedimentosOriginais");
        IncluirCampo(entidade.PrescricaoAlterada, "Prescricao", "PrescricaoOriginal");
        if (entidade.StatusAlterado)
            alteracoes.Add("Status = @Status");

        if (alteracoes.Count == 0)
        {
            ExigirConsultaAgendada(entidade.Id);
            return;
        }

        // Somente fragmentos fixos compõem o SET. Os valores continuam parametrizados.
        var sql = $"""
            UPDATE Consultas
            SET {string.Join(", ", alteracoes)}
            WHERE Id = @Id AND Status = @Agendada{string.Concat(valoresOriginais)};
            """;

        var linhasAfetadas = ExecutarGravacao(sql, new
        {
            entidade.Id,
            DataHora = FormatarDataHora(entidade.DataHora),
            DataHoraOriginal = FormatarDataHora(entidade.DataHoraOriginal),
            Status = (int)entidade.Status,
            Agendada = (int)StatusConsulta.Agendada,
            entidade.Diagnostico,
            entidade.DiagnosticoOriginal,
            entidade.Procedimentos,
            entidade.ProcedimentosOriginais,
            entidade.Prescricao,
            entidade.PrescricaoOriginal
        });

        if (linhasAfetadas == 0)
        {
            ExigirConsultaAgendada(entidade.Id);
            throw new RegraNegocioException("Os dados que você tentou alterar já foram atualizados. Reabra a consulta e revise as alterações antes de salvar.");
        }

        entidade.ConfirmarAtualizacao();

        void IncluirCampo(bool alterado, string coluna, string parametroOriginal)
        {
            if (!alterado)
                return;

            alteracoes.Add($"{coluna} = @{coluna}");
            // IS compara também os valores originais nulos no SQLite.
            valoresOriginais.Add($" AND {coluna} IS @{parametroOriginal}");
        }
    }

    private void ExigirConsultaAgendada(int id)
    {
        using var conexao = _connectionFactory.CriarConexao();
        var agendada = conexao.ExecuteScalar<int>(
            "SELECT EXISTS (SELECT 1 FROM Consultas WHERE Id = @Id AND Status = @Agendada);",
            new { Id = id, Agendada = (int)StatusConsulta.Agendada });
        if (agendada == 0)
            throw new RegraNegocioException("A consulta não foi encontrada ou já foi encerrada.");
    }

    public void Excluir(int id)
    {
        throw new RegraNegocioException("Consultas devem ser canceladas para preservar o histórico.");
    }

    public List<Consulta> ListarPorPet(int petId)
    {
        using var conexao = _connectionFactory.CriarConexao();
        return conexao.Query<ConsultaRegistro>(
                SelecionarConsultas + " WHERE PetId = @PetId ORDER BY DataHora DESC, Id DESC;",
                new { PetId = petId })
            .Select(registro => registro.ParaConsulta())
            .ToList();
    }

    public bool ExisteConflitoHorario(int veterinarioId, DateTime dataHora, int? consultaIdIgnorada = null)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM Consultas
                WHERE VeterinarioId = @VeterinarioId
                  AND DataHora = @DataHora
                  AND Status = @Agendada
                  AND (@ConsultaIdIgnorada IS NULL OR Id <> @ConsultaIdIgnorada)
            );
            """;

        using var conexao = _connectionFactory.CriarConexao();
        return conexao.ExecuteScalar<int>(sql, new
        {
            VeterinarioId = veterinarioId,
            DataHora = FormatarDataHora(dataHora),
            Agendada = (int)StatusConsulta.Agendada,
            ConsultaIdIgnorada = consultaIdIgnorada
        }) == 1;
    }

    private int ExecutarGravacao(string sql, object parametros)
    {
        using var conexao = _connectionFactory.CriarConexao();
        try
        {
            return conexao.Execute(sql, parametros);
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == SqliteConstraintUnique
            && ex.Message.EndsWith(
                "UNIQUE constraint failed: Consultas.VeterinarioId, Consultas.DataHora'.",
                StringComparison.Ordinal))
        {
            // O SQLite identifica a restrição pelas colunas, sem informar o nome do índice.
            throw new RegraNegocioException(MensagemConflito);
        }
    }

    private static string FormatarDataHora(DateTime dataHora) =>
        dataHora.ToString(FormatoDataHora, CultureInfo.InvariantCulture);

    private sealed class ConsultaRegistro
    {
        public int Id { get; set; }
        public int PetId { get; set; }
        public int VeterinarioId { get; set; }
        public string DataHora { get; set; } = string.Empty;
        public int Status { get; set; }
        public string QueixaInicial { get; set; } = string.Empty;
        public int Urgencia { get; set; }
        public string? Diagnostico { get; set; }
        public string? Procedimentos { get; set; }
        public string? Prescricao { get; set; }

        public Consulta ParaConsulta()
        {
            if (!DateTime.TryParseExact(DataHora, FormatoDataHora, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dataHora))
            {
                throw new InvalidDataException(
                    $"DataHora inválida na consulta de ID {Id}. Formato esperado: {FormatoDataHora}.");
            }

            return new Consulta(Id, PetId, VeterinarioId, dataHora, (StatusConsulta)Status,
                QueixaInicial, Urgencia == 1, Diagnostico, Procedimentos, Prescricao);
        }
    }
}
