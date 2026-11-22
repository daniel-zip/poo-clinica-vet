using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VetCare.Models.ViewModels.Consulta;

public class ConsultaAgendarViewModel
{
    [Required(ErrorMessage = "Selecione um pet.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecione um pet válido.")]
    [Display(Name = "Pet")]
    public int? PetId { get; set; }

    [Required(ErrorMessage = "Selecione um veterinário.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecione um veterinário válido.")]
    [Display(Name = "Veterinário")]
    public int? VeterinarioId { get; set; }

    [Required(ErrorMessage = "Informe a data e o horário da consulta.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Data e horário")]
    public DateTime? DataHora { get; set; }

    [Required(ErrorMessage = "Informe a queixa inicial da consulta.")]
    [Display(Name = "Queixa inicial")]
    public string QueixaInicial { get; set; } = string.Empty;

    [Display(Name = "Urgência")]
    public bool Urgencia { get; set; }

    [BindNever]
    public IReadOnlyList<SelectListItem> Pets { get; set; } = [];

    [BindNever]
    public IReadOnlyList<SelectListItem> Veterinarios { get; set; } = [];
}
