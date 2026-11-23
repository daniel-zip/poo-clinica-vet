using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VetCare.Models.ViewModels.Consulta;

public class ConsultaReagendarViewModel
{
    [Required(ErrorMessage = "Selecione um veterinário.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecione um veterinário válido.")]
    [Display(Name = "Veterinário responsável")]
    public int? VeterinarioId { get; set; }

    [Required(ErrorMessage = "Informe a nova data e o horário da consulta.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Nova data e horário")]
    public DateTime? NovaDataHora { get; set; }

    [BindNever]
    public ConsultaResumoViewModel Consulta { get; set; } = new();

    [BindNever]
    public IReadOnlyList<SelectListItem> Veterinarios { get; set; } = [];
}
