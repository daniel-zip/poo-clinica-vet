using VetCare.Models.Enums;

namespace VetCare.Models;

public class Administrador : Funcionario
{
    public Administrador(
        int id,
        string nome,
        string cpf,
        string email,
        string telefone,
        string endereco,
        double salario,
        int nivelAcesso,
        bool ativo = true)
        : base(id, nome, cpf, email, telefone, endereco, salario, nivelAcesso, ativo)
    {
    }

    public Administrador(
        string nome,
        string cpf,
        string email,
        string telefone,
        string endereco,
        double salario,
        int nivelAcesso)
        : this(0, nome, cpf, email, telefone, endereco, salario, nivelAcesso)
    {
    }

    protected override int NivelAcessoPadrao => 3;

    public override bool PodeExecutar(Operacao operacao) =>
        operacao == Operacao.AcessarRecepcao
        || operacao == Operacao.AcessarVeterinario
        || operacao == Operacao.AcessarAdministracao;
}
