using System.Globalization;
using VetCare.Models.Enums;

namespace VetCare.Models.ViewModels;

public sealed record NavegacaoAgenda(string? AreaClinica, DateTime? Data, StatusConsulta? Status,
    bool VeioDoAtendimento = false, int? VeterinarioId = null)
{
    private const string FormatoData = "yyyy-MM-dd";

    public static NavegacaoAgenda DaConsulta(IQueryCollection query, string? areaClinica = null)
    {
        var data = DateTime.TryParseExact(query["dataAgenda"], FormatoData, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var dia) ? dia : (DateTime?)null;
        var status = Enum.TryParse<StatusConsulta>(query["statusAgenda"], out var situacao) && Enum.IsDefined(situacao)
            ? situacao
            : (StatusConsulta?)null;
        var area = areaClinica ?? query["areaClinica"].ToString();

        return new NavegacaoAgenda(area == "veterinario" ? area : null, data, status,
            query["origem"] == "atendimento", VeterinarioIdValido(query["veterinarioAgenda"]));
    }

    public static int? VeterinarioIdValido(string? valor) =>
        int.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0 ? id : null;

    public Dictionary<string, string> ParaAgenda() => MontarRota("data", "status", "veterinarioId");

    public Dictionary<string, string> ParaConsulta(int? id = null, bool veioDoAtendimento = false)
    {
        // Leva os filtros de origem até os formulários para restaurá-los no retorno à agenda.
        var rota = MontarRota("dataAgenda", "statusAgenda", "veterinarioAgenda");
        if (id is int consultaId)
            rota["id"] = consultaId.ToString(CultureInfo.InvariantCulture);
        if (veioDoAtendimento)
            rota["origem"] = "atendimento";
        return rota;
    }

    private Dictionary<string, string> MontarRota(string chaveData, string chaveStatus, string chaveVeterinario)
    {
        var rota = new Dictionary<string, string>();
        if (AreaClinica is not null)
            rota["areaClinica"] = AreaClinica;
        if (Data is DateTime data)
            rota[chaveData] = data.ToString(FormatoData, CultureInfo.InvariantCulture);
        if (Status is StatusConsulta status)
            rota[chaveStatus] = status.ToString();
        if (AreaClinica is null && VeterinarioId is int veterinarioId)
            rota[chaveVeterinario] = veterinarioId.ToString(CultureInfo.InvariantCulture);
        return rota;
    }
}
