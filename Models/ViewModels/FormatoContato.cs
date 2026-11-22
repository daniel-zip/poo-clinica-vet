using System.Text.RegularExpressions;

namespace VetCare.Models.ViewModels;

public static partial class FormatoContato
{
    public const string PadraoCpf = @"(?:[0-9]{11}|[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2})";
    public const string PadraoTelefone = @"(?:[0-9]{10,11}|\([0-9]{2}\) ?[0-9]{4,5}-[0-9]{4})";

    public static string ExibirCpf(string valor)
    {
        if (!CpfValido().IsMatch(valor))
            return valor;

        var digitos = string.Concat(valor.Where(char.IsAsciiDigit));
        return $"{digitos[..3]}.{digitos[3..6]}.{digitos[6..9]}-{digitos[9..]}";
    }

    public static string ExibirTelefone(string valor)
    {
        if (!TelefoneValido().IsMatch(valor))
            return valor;

        var digitos = string.Concat(valor.Where(char.IsAsciiDigit));
        return $"({digitos[..2]}) {digitos[2..^4]}-{digitos[^4..]}";
    }

    [GeneratedRegex(@"\A" + PadraoCpf + @"\z")]
    private static partial Regex CpfValido();

    [GeneratedRegex(@"\A" + PadraoTelefone + @"\z")]
    private static partial Regex TelefoneValido();
}
