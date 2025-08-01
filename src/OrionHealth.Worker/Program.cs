// ####################################################################################
// # USANDO NOSSAS CAMADAS - Importamos as peças que vamos montar.                    #
// ####################################################################################

// Usamos o 'CrossCutting' para acessar nosso configurador de Injeção de Dependência.
// Nossas importações de endereço.
using OrionHealth.CrossCutting;
using OrionHealth.Infrastructure.Mllp;
using Microsoft.Extensions.Hosting;

// ####################################################################################
// # O PONTO DE PARTIDA - Onde a aplicação começa.                                     #
// ####################################################################################

// Criamos o "construtor de Host". O Host é o ambiente que vai executar nossa aplicação,
// gerenciando configuração, logs e o ciclo de vida dos nossos serviços.
IHost host = Host.CreateDefaultBuilder(args)
    // O método 'ConfigureServices' é onde a mágica da Injeção de Dependência acontece.
    .ConfigureServices((hostContext, services) =>
    {
        // #### O INTERRUPTOR PRINCIPAL ####
        // Chamamos o nosso método 'AddAppServices' que criamos na camada CrossCutting.
        // Esta única linha de código é responsável por registrar TODAS as nossas dependências:
        // - A conexão com o banco de dados (DbContext e IDbConnection).
        // - A Unit of Work.
        // - Nossos casos de uso (IReceiveOruR01UseCase).
        // - O parser de HL7 (IHL7Parser).
        // Passamos 'hostContext.Configuration' para que ele tenha acesso ao appsettings.json.
        services.AddAppServices(hostContext.Configuration);

        // #### REGISTRANDO NOSSO SERVIÇO DE BACKGROUND ####
        // 'AddHostedService' diz ao Host: "Eu tenho um serviço que precisa rodar continuamente
        // em background. Por favor, inicie-o quando a aplicação começar e pare-o
        // quando a aplicação for desligada".
        // Aqui, registramos nosso MllpListenerService para ser esse serviço.
        services.AddHostedService<MllpListenerService>();
    })
    // 'Build()' constrói o Host com todas as configurações que definimos.
    .Build();

// 'Run()' inicia a aplicação e a mantém rodando até que seja interrompida (ex: com Ctrl+C).
// É esta linha que efetivamente "liga o motor".
host.Run();