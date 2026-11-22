using VetCare.Models;
using VetCare.Repositories.Dapper;
using VetCare.Repositories.Interfaces;
using VetCare.Services;

namespace VetCare.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVetCareDependencies(this IServiceCollection services)
    {
        services.AddScoped<SqliteConnectionFactory>();
        services.AddScoped<IRepository<Tutor>, TutorRepository>();

        services.AddScoped<PetRepository>();
        services.AddScoped<IRepository<Pet>>(provider =>
            provider.GetRequiredService<PetRepository>());

        services.AddScoped<IRepository<Funcionario>, FuncionarioRepository>();
        services.AddScoped<IConsultaRepository, ConsultaRepository>();
        services.AddScoped<ConsultaService>();

        return services;
    }
}
