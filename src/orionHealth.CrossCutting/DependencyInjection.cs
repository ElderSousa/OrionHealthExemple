// Namespaces que vamos usar. O primeiro é para a DI, o segundo para Configuração,
// e os outros são para acessar nossas próprias classes e interfaces.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using OrionHealth.Application.Interfaces.Persistence;
using OrionHealth.Infrastructure.Persistence;
using OrionHealth.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;
using OrionHealth.Application.UseCases.ReceiveOruR01;
using OrionHealth.Application.Interfaces;
//using OrionHealth.Infrastructure.HL7; // Vamos criar isso na próxima sessão!

// 'static class' é uma classe que não pode ser instanciada (não se pode fazer 'new DependencyInjection()').
// Ela serve apenas como um agrupador para métodos estáticos.
public static class DependencyInjection
{
    // Este é um "método de extensão". A palavra 'this' antes do primeiro parâmetro
    // permite que a gente chame este método como se ele fizesse parte da classe 'IServiceCollection'.
    // Ex: 'services.AddAppServices(...)'. É um truque de C# para deixar o código mais legível.
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Configurar a camada de Aplicação
        services.AddApplication();

        // 2. Configurar a camada de Infraestrutura
        services.AddInfrastructure(configuration);

        // Retornamos 'services' para permitir o encadeamento de chamadas (ex: services.AddAppServices().AddOutraCoisa()).
        return services;
    }

    // Método privado para organizar a configuração da camada de Aplicação.
    private static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Aqui registramos nosso caso de uso.
        // AddScoped: Diz ao .NET para criar uma instância de 'ReceiveOruR01UseCase' por "escopo".
        // Em nosso projeto, vamos definir que cada mensagem HL7 processada é um escopo.
        // Isso garante que todos os serviços usados para processar UMA mensagem sejam os mesmos,
        // mas que a PRÓXIMA mensagem receba um conjunto novo de serviços. É perfeito para nós.
        services.AddScoped<IReceiveOruR01UseCase, ReceiveOruR01UseCase>();

        return services;
    }

    // Método privado para organizar a configuração da camada de Infraestrutura.
    private static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Pegamos a string de conexão do nosso arquivo de configuração (appsettings.json),
        // que será lido pelo IConfiguration.
        var connectionString = configuration.GetConnectionString("OracleConnection");

        // #### Registro do Entity Framework Core ####
        // AddDbContext é um método especial para o EF Core. Ele já registra o ApplicationDbContext
        // com um tempo de vida 'Scoped' e nos permite configurar como ele deve se conectar.
        services.AddDbContext<ApplicationDbContext>(options =>
            // Usamos o provedor do Oracle e passamos a string de conexão.
            options.UseOracle(connectionString)
        );

        // #### Registro do Dapper ####
        // Para o Dapper, precisamos registrar uma 'IDbConnection'.
        // Usamos 'AddScoped' para que cada escopo (cada mensagem HL7) tenha sua própria conexão.
        services.AddScoped<IDbConnection>(sp =>
            // 'sp' é o "Service Provider". O código '=> new OracleConnection(connectionString)'
            // é uma "fábrica": "Toda vez que alguém pedir uma IDbConnection, execute este código para criar uma nova".
            new OracleConnection(connectionString)
        );

        // #### Registro da Unit of Work e Repositórios ####
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        // Não precisamos registrar os repositórios (IPatientRepository, etc) aqui, pois
        // nossa classe UnitOfWork já é responsável por criá-los.

        // #### Registro do Parser HL7 (que faremos a seguir) ####
        // AddSingleton: Diz ao .NET para criar UMA ÚNICA instância de 'HapiParser' e reutilizá-la
        // durante toda a vida da aplicação. Isso é bom para classes que não guardam estado
        // e são caras para criar, como um parser.
        //services.AddSingleton<IHL7Parser, HapiParser>();

        return services;
    }
}