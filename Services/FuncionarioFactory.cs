using VetCare.Exceptions;
using VetCare.Models;

namespace VetCare.Services;

public static class FuncionarioFactory
{
    public static Funcionario Criar(
        string tipo,
        int id,
        string nome,
        string cpf,
        string email,
        string telefone,
        string endereco,
        double salario,
        int nivelAcesso,
        bool ativo,
        string? turno,
        string? crmv,
        string? especialidade)
    {
        if (string.IsNullOrWhiteSpace(tipo))
        {
            throw new RegraNegocioException("Informe o tipo do funcionário.");
        }

        var tipoNormalizado = tipo.Trim();

        return tipoNormalizado switch
        {
            "Recepcionista" => new Recepcionista(
                id,
                nome,
                cpf,
                email,
                telefone,
                endereco,
                salario,
                nivelAcesso,
                turno ?? string.Empty,
                ativo),
            "Veterinario" => new Veterinario(
                id,
                nome,
                cpf,
                email,
                telefone,
                endereco,
                salario,
                nivelAcesso,
                crmv ?? string.Empty,
                especialidade ?? string.Empty,
                ativo),
            "Administrador" => new Administrador(
                id,
                nome,
                cpf,
                email,
                telefone,
                endereco,
                salario,
                nivelAcesso,
                ativo),
            _ => throw new RegraNegocioException(
                $"Tipo de funcionário '{tipoNormalizado}' inválido. Valores aceitos: Recepcionista, Veterinario e Administrador.")
        };
    }
}
