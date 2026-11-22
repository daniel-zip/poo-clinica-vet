using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace VetCare.Models.ViewModels.Consulta;

public class ConsultaAtendimentoViewModel
{
    [BindNever]
    public ConsultaResumoViewModel Consulta { get; set; } = new();

    [Display(Name = "Diagnóstico")]
    public string? Diagnostico { get; set; }

    public string? Procedimentos { get; set; }

    [Display(Name = "Prescrição")]
    public string? Prescricao { get; set; }
}
