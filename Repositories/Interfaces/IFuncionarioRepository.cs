using VetCare.Models;

namespace VetCare.Repositories.Interfaces;

public interface IFuncionarioRepository : IRepository<Funcionario>
{
    bool Reativar(int id);
    void Desativar(int id, int administradorId);
}
