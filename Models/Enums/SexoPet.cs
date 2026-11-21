using System.ComponentModel.DataAnnotations;

namespace VetCare.Models.Enums;

public enum SexoPet
{
    Macho = 1,

    [Display(Name = "Fêmea")]
    Femea = 2
}
