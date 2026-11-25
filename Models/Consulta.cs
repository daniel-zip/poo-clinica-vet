using VetCare.Exceptions;
using VetCare.Models.Enums;

namespace VetCare.Models;

public class Consulta
{   // Config gerais da Consulta
    // TimeSpan: intervalo de tempo (1a horário 8h, último 20h)
    public static readonly TimeSpan InicioAgendamentos = TimeSpan.FromHours(8);
    public static readonly TimeSpan FimAgendamentos = TimeSpan.FromHours(20);
    public const int IntervaloAgendamentosEmMinutos = 30;
    public const string MensagemDiasDeAgendamento = "Escolha uma data de segunda a sexta-feira.";
    // Fuso do calendário
    public const string FusoHorarioAgendamentos = "America/Sao_Paulo";
    // Pra validar dia/hora do agendamento
    public static DateTime AgoraNaClinica => DateTime.Now;

    // Características gerais do objeto
    /* obs.: "get ; " - imutáveis depois de instanciar
    "get; private set" - mutáveis na classe */
    public int Id { get; }
    public int PetId { get; }
    public int VeterinarioId { get; private set; }
    public DateTime DataHora { get; private set; }
    public StatusConsulta Status { get; private set; }
    public string QueixaInicial { get; }
    public bool Urgencia { get; }
    /* ? indica que o valor das propriedades pode ser nulo, ou seja, o 
    preenchimento durante o atendimento é opcional */
    public string? Diagnostico { get; private set; }
    public string? Procedimentos { get; private set; }
    public string? Prescricao { get; private set; }

    internal DateTime DataHoraOriginal { get; private set; }
    internal int VeterinarioIdOriginal { get; private set; }
    internal string? DiagnosticoOriginal { get; private set; }
    internal string? ProcedimentosOriginais { get; private set; }
    internal string? PrescricaoOriginal { get; private set; }

    // Salvar valores originais para compará-los com os novos em uma possível atualização
    internal bool HorarioAlterado => DataHora != DataHoraOriginal;
    internal bool VeterinarioAlterado => VeterinarioId != VeterinarioIdOriginal;
    internal bool DiagnosticoAlterado => Diagnostico != DiagnosticoOriginal;
    internal bool ProcedimentosAlterados => Procedimentos != ProcedimentosOriginais;
    internal bool PrescricaoAlterada => Prescricao != PrescricaoOriginal;
    internal bool StatusAlterado { get; private set; }

    // Construtor pra agendar novas consultas
    public Consulta(int petId, int veterinarioId, DateTime dataHora, string queixaInicial, bool urgencia)
        : this(0, petId, veterinarioId, dataHora, StatusConsulta.Agendada, queixaInicial, urgencia,
            null, null, null)
    {
        ValidarHorario(dataHora);
        ValidarPeriodoDeAgendamento(dataHora);
    }

    // Construtor usado pelo repositório para reinstanciar a consulta salva no BD
    internal Consulta(int id, int petId, int veterinarioId, DateTime dataHora, StatusConsulta status,
        string queixaInicial, bool urgencia, string? diagnostico, string? procedimentos, string? prescricao)
    {   
        //impedir instanciações inválidas
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
        if (dataHora == default) 
            // Estar igual ao default (01/01/0001 00:00:00) indica que não há data inserida
            // Além disso, permite datas passadas, pq pode reinstanciar consultas já realizadas
            throw new RegraNegocioException("Informe a data e o horário da consulta.");

        // Atribuição dos valores dos parâmetros às propriedades do objeto
        Id = id;
        PetId = petId;
        VeterinarioId = veterinarioId;
        DataHora = dataHora;
        Status = status;
        QueixaInicial = queixaInicial.Trim(); // padronização: tira espaços anteriores e posteriores
        Urgencia = urgencia;
        Diagnostico = diagnostico;
        Procedimentos = procedimentos;
        Prescricao = prescricao;

        // Salva valores atuais como originais (permitindo alterações futuras)
        ConfirmarAtualizacao(); 
    }

    // Permite alterar de forma controlada a data/hora e o veterinário (que são private set)
    public void Reagendar(DateTime novaDataHora, int? veterinarioId = null)
    // int? (permite id nulo) veterinarioId = null (torna opcional a inserção de novo ID do veterinário)
    {
        ExigirAgendada(); // Verifica o status da Consulta (= Agendada)
        if (veterinarioId <= 0)
            throw new RegraNegocioException("Selecione um veterinário cadastrado.");
        ValidarHorario(novaDataHora);
        ValidarPeriodoDeAgendamento(novaDataHora);
        DataHora = novaDataHora;
        /* ?? Se o ID do parâmetro for nulo, mantém-se o ID da Propriedade,
        caso contrário, troca-se o veterinário atribuindo o novo valor do parâmetro à propriedade */
        VeterinarioId = veterinarioId ?? VeterinarioId;
    }

    // Apenas cancela alterando o status, e não exclui (preserva histórico)
    public void Cancelar() 
    {
        ExigirAgendada();
        Status = StatusConsulta.Cancelada;
        StatusAlterado = true; 
    }

    // Registra as informações que podem ser nulas (ex: não haver diagnóstico)
    public void RegistrarAtendimento(string? diagnostico, string? procedimentos, string? prescricao)
    {
        ExigirAgendada();
        Diagnostico = diagnostico;
        Procedimentos = procedimentos;
        Prescricao = prescricao;
    }

    // Altera status para finalizada
    public void Finalizar()
    {
        ExigirAgendada();
        Status = StatusConsulta.Finalizada;
        StatusAlterado = true;
    }

    // Estabelece a referência de modificação *atualizada*
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

    // Verificar se está dentro do período de 30min
    private static void ValidarHorario(DateTime dataHora)
    {
        if (dataHora.TimeOfDay.TotalMinutes % IntervaloAgendamentosEmMinutos != 0)
            throw new RegraNegocioException("Selecione um horário em intervalos de 30 minutos.");
    }

    // Apenas agendamentos entre segunda e sexta-feira, das 8h às 20h e no futuro
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
