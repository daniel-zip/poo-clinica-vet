namespace VetCare.Repositories.Interfaces;

public interface IRepository<T> where T : class
{
    void Adicionar(T entidade);
    T? ObterPorId(int id);
    List<T> Listar();
    void Atualizar(T entidade);
    void Excluir(int id);
}