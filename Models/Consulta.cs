using VetCare.Exceptions;
using VetCare.Models.Enums;

namespace VetCare.Models;

public class Consulta
{
    public static readonly TimeSpan InicioAgendamentos = TimeSpan.FromHours(8);
    public static readonly TimeSpan FimAgendamentos = TimeSpan.FromHours(20);
    public const int IntervaloAgendamentosEmMinutos = 30;
    public const string MensagemDiasDeAgendamento = "Escolha uma data de segunda a sexta-feira.";
    public const string FusoHorarioAgendamentos = "America/Sao_Paulo";
    private static readonly TimeZoneInfo FusoDaClinica = TimeZoneInfo.FindSystemTimeZoneById(FusoHorarioAgendamentos);
    public static DateTime AgoraNaClinica => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, FusoDaClinica).DateTime;

    public int Id { get; }
    public int PetId { get; }
    public int VeterinarioId { get; private set; }
    public DateTime DataHora { get; private set; }
    public StatusConsulta Status { get; private set; }
    public string QueixaInicial { get; }
    public bool Urgencia { get; }
    public string? Diagnostico { get; private set; }
    public string? Procedimentos { get; private set; }
    public string? Prescricao { get; private set; }

    internal DateTime DataHoraOriginal { get; private set; }
    internal int VeterinarioIdOriginal { get; private set; }
    internal string? DiagnosticoOriginal { get; private set; }
    internal string? ProcedimentosOriginais { get; private set; }
    internal string? PrescricaoOriginal { get; private set; }

    internal bool HorarioAlterado => DataHora != DataHoraOriginal;
    internal bool VeterinarioAlterado => VeterinarioId != VeterinarioIdOriginal;
    internal bool DiagnosticoAlterado => Diagnostico != DiagnosticoOriginal;
    internal bool ProcedimentosAlterados => Procedimentos != ProcedimentosOriginais;
    internal bool PrescricaoAlterada => Prescricao != PrescricaoOriginal;
    internal bool StatusAlterado { get; private set; }

    public Consulta(int petId, int veterinarioId, DateTime dataHora, string queixaInicial, bool urgencia)
        : this(0, petId, veterinarioId, dataHora, StatusConsulta.Agendada, queixaInicial, urgencia,
            null, null, null)
    {
        ValidarPeriodoDeAgendamento(dataHora);
    }

    // Usado pelo repositório para recuperar o estado persistido da consulta.
    internal Consulta(int id, int petId, int veterinarioId, DateTime dataHora, StatusConsulta status,
        string queixaInicial, bool urgencia, string? diagnostico, string? procedimentos, string? prescricao)
    {
        if (id < 0)
            throw new RegraNegocioException("O identificador da consulta é inválido.");
        if (petId <= 0)
            throw new RegraNegocioException("Selecione um pet cadastrado.");
        if (veterinarioId <= 0)
            throw new RegraNegocioException("Selecione um veterinário cadastrado.");
        if (!Enum.IsDefined(status))
            throw new RegraNegocioException("O status da consulta é inválido.");
        if (string.IsNullOrWhiteSpace(queixaInicial))
            throw new RegraNegocioException("Informe a queixa inicial da consulta.");

        ValidarHorario(dataHora);

        Id = id;
        PetId = petId;
        VeterinarioId = veterinarioId;
        DataHora = dataHora;
        Status = status;
        QueixaInicial = queixaInicial.Trim();
        Urgencia = urgencia;
        Diagnostico = diagnostico;
        Procedimentos = procedimentos;
        Prescricao = prescricao;
        ConfirmarAtualizacao();
    }

    public void Reagendar(DateTime novaDataHora, int? veterinarioId = null)
    {
        ExigirAgendada();
        if (veterinarioId is <= 0)
            throw new RegraNegocioException("Selecione um veterinário cadastrado.");
        ValidarHorario(novaDataHora);
        ValidarPeriodoDeAgendamento(novaDataHora);
        DataHora = novaDataHora;
        VeterinarioId = veterinarioId ?? VeterinarioId;
    }

    public void Cancelar()
    {
        ExigirAgendada();
        Status = StatusConsulta.Cancelada;
        StatusAlterado = true;
    }

    public void RegistrarAtendimento(string? diagnostico, string? procedimentos, string? prescricao)
    {
        ExigirAgendada();
        Diagnostico = diagnostico;
        Procedimentos = procedimentos;
        Prescricao = prescricao;
    }

    public void Finalizar()
    {
        ExigirAgendada();
        Status = StatusConsulta.Finalizada;
        StatusAlterado = true;
    }

    internal void ConfirmarAtualizacao()
    {
        DataHoraOriginal = DataHora;
        VeterinarioIdOriginal = VeterinarioId;
        DiagnosticoOriginal = Diagnostico;
        ProcedimentosOriginais = Procedimentos;
        PrescricaoOriginal = Prescricao;
        StatusAlterado = false;
    }

    private void ExigirAgendada()
    {
        if (Status != StatusConsulta.Agendada)
            throw new RegraNegocioException("Somente consultas agendadas podem ser alteradas.");
    }

    private static void ValidarHorario(DateTime dataHora)
    {
        if (dataHora == default)
            throw new RegraNegocioException("Informe a data e o horário da consulta.");
        if (dataHora.TimeOfDay.Ticks % (IntervaloAgendamentosEmMinutos * TimeSpan.TicksPerMinute) != 0)
            throw new RegraNegocioException("Selecione um horário em intervalos de 30 minutos.");
    }

    private static void ValidarPeriodoDeAgendamento(DateTime dataHora)
    {
        if (dataHora.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            throw new RegraNegocioException(MensagemDiasDeAgendamento);
        if (dataHora.TimeOfDay < InicioAgendamentos || dataHora.TimeOfDay > FimAgendamentos)
            throw new RegraNegocioException($"Escolha um horário entre {InicioAgendamentos:hh\\:mm} e {FimAgendamentos:hh\\:mm}.");

        if (dataHora <= AgoraNaClinica)
            throw new RegraNegocioException("Escolha uma data e um horário futuros.");
    }
}
