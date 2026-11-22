using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace VetCare.Models.ViewModels.Consulta;

public class ConsultaReagendarViewModel
{
    [Required(ErrorMessage = "Informe a nova data e o horário da consulta.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Nova data e horário")]
    public DateTime? NovaDataHora { get; set; }

    [BindNever]
    public ConsultaResumoViewModel Consulta { get; set; } = new();
}
