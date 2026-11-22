using VetCare.Exceptions;
using VetCare.Models.Enums;

namespace VetCare.Models;

public class Veterinario : Funcionario
{
    public string Crmv { get; private set; } = string.Empty;
    public string Especialidade { get; private set; } = string.Empty;

    public Veterinario(
        int id,
        string nome,
        string cpf,
        string email,
        string telefone,
        string endereco,
        double salario,
        int nivelAcesso,
        string crmv,
        string especialidade,
        bool ativo = true)
        : base(id, nome, cpf, email, telefone, endereco, salario, nivelAcesso, ativo)
    {
        AtualizarDadosProfissionais(crmv, especialidade);
    }

    public Veterinario(
        string nome,
        string cpf,
        string email,
        string telefone,
        string endereco,
        double salario,
        int nivelAcesso,
        string crmv,
        string especialidade)
        : this(0, nome, cpf, email, telefone, endereco, salario, nivelAcesso, crmv, especialidade)
    {
    }

    protected override int NivelAcessoPadrao => 2;

    public override bool PodeExecutar(Operacao operacao) => operacao == Operacao.AcessarVeterinario;

    public void AtualizarInformacoes(
        string nome,
        string cpf,
        string email,
        string telefone,
        string endereco,
        double salario,
        int nivelAcesso,
        string crmv,
        string especialidade)
    {
        base.AtualizarInformacoes(nome, cpf, email, telefone, endereco, salario, nivelAcesso);
        AtualizarDadosProfissionais(crmv, especialidade);
    }

    private void AtualizarDadosProfissionais(string crmv, string especialidade)
    {
        if (string.IsNullOrWhiteSpace(crmv))
        {
            throw new RegraNegocioException("Informe o CRMV do veterinário.");
        }

        if (string.IsNullOrWhiteSpace(especialidade))
        {
            throw new RegraNegocioException("Informe a especialidade do veterinário.");
        }

        Crmv = crmv.Trim();
        Especialidade = especialidade.Trim();
    }
}
