using VetCare.Exceptions;
using VetCare.Models;

namespace VetCare.Services;

public static class FuncionarioFactory //recebe os dados e o cargo e intancia o funcionário
{
    public static Funcionario Criar( //recebe os dados e retorna o funcionário instanciado
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
        if (string.IsNullOrWhiteSpace(tipo)) //dispara a exceção caso não coloque o tipo do funcionário
        {
            throw new RegraNegocioException("Informe o tipo do funcionário.");
        }

        var tipoNormalizado = tipo.Trim(); 

        return tipoNormalizado switch //serve para instanciar o funcionário de acordo com o tipo informado
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
                //caso não seja um dos tipos válidos dispara a exceção
        };
    }
}
