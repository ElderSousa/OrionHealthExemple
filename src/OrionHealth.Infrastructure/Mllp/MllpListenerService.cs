// ####################################################################################
// # USING STATEMENTS - As "importações" que nosso arquivo precisa.                  #
// ####################################################################################

// Define o "endereço" desta classe, para que outras partes do programa possam encontrá-la.
namespace OrionHealth.Infrastructure.Mllp;

// Importa as ferramentas do .NET para criar serviços de background (Hosted Services).
using Microsoft.Extensions.Hosting;
// Importa as ferramentas de logging, para podermos escrever mensagens no console.
using Microsoft.Extensions.Logging;
// Importa as ferramentas de Injeção de Dependência, para podermos criar "escopos".
using Microsoft.Extensions.DependencyInjection;
// Importa classes relacionadas a endereços de rede (IP).
using System.Net;
// Importa as classes para comunicação de rede TCP (Sockets). É o coração da nossa comunicação.
using System.Net.Sockets;
// Importa classes para manipulação de texto, como StringBuilder e Encoding.
using System.Text;
// Importa o contrato (a interface) do nosso caso de uso, para podermos executá-lo.
using OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;

// ####################################################################################
// # A CLASSE - O nosso serviço "ouvinte" de rede.                                    #
// ####################################################################################

/// <summary>
/// Este é o nosso serviço de "ouvinte". Ele herda de BackgroundService,
/// uma classe especial do .NET projetada para tarefas de longa duração que rodam em segundo plano.
/// Sua única missão é escutar a rede por conexões que falam o protocolo MLLP.
/// </summary>
public class MllpListenerService : BackgroundService
{
    // --- Campos Privados (As "Ferramentas" da Classe) ---

    // 'private readonly' significa que esta variável só pode ser usada dentro desta classe
    // e seu valor é definido no construtor e nunca mais muda. É uma prática segura.
    
    // Ferramenta para registrar logs (mensagens de informação, erro, etc.) no console.
    private readonly ILogger<MllpListenerService> _logger;
    // Um provedor de serviços. Pense nele como uma "fábrica de ferramentas" sob demanda.
    // Vamos usá-lo para criar um novo conjunto de serviços (como o DbContext) para CADA mensagem
    // que recebermos, garantindo que o processamento de uma não interfira na outra.
    private readonly IServiceProvider _serviceProvider;

    // --- Constantes do Protocolo MLLP ---
    // 'const' significa que o valor é fixo e nunca muda.
    // Estamos definindo os caracteres de controle do MLLP. Eles não são letras ou números normais.
    // '(char)0x0B' converte o código hexadecimal 0B para o caractere correspondente (Start of Block).
    private const char START_OF_BLOCK = (char)0x0B;  // Indica o início de uma mensagem MLLP.
    private const char END_OF_BLOCK = (char)0x1C;    // Indica o fim de uma mensagem MLLP.
    private const char CARRIAGE_RETURN = (char)0x0D; // É o "Enter", necessário no final da transmissão MLLP.

    // --- O Construtor ---
    /// <summary>
    /// O construtor é o método chamado quando uma instância de MllpListenerService é criada.
    /// É aqui que a Injeção de Dependência nos entrega as ferramentas que pedimos.
    /// </summary>
    public MllpListenerService(ILogger<MllpListenerService> logger, IServiceProvider serviceProvider)
    {
        // Guardamos as ferramentas recebidas nos nossos campos privados para poder usá-las depois.
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    // --- O Método Principal de Execução ---
    /// <summary>
    /// Este é o método principal que o .NET chama quando o serviço inicia.
    /// 'protected override' significa que estamos substituindo um método que já existe na classe base (BackgroundService).
    /// 'async Task' indica que é um método assíncrono que não retorna um valor direto.
    /// 'CancellationToken' é um sinal que o .NET nos envia para dizer "é hora de parar".
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Criamos um "escutador" TCP. 'IPAddress.Any' significa que ele aceitará conexões
        // de qualquer endereço de IP, não apenas da máquina local. 1080 é a porta de rede.
        var listener = new TcpListener(IPAddress.Any, 1080);
        // Iniciamos o escutador. A partir daqui, a porta 1080 está "aberta" na nossa máquina.
        listener.Start();
        // Escrevemos uma mensagem no log para sabermos que tudo começou bem.
        _logger.LogInformation("Servidor MLLP iniciado na porta 1080. Aguardando conexões...");

        // Este é o loop principal do servidor. Ele roda "para sempre".
        // A condição '!stoppingToken.IsCancellationRequested' verifica se o .NET nos pediu para parar.
        while (!stoppingToken.IsCancellationRequested)
        {
            // 'await listener.AcceptTcpClientAsync(...)' é uma chamada que bloqueia de forma assíncrona.
            // O código "pausa" aqui, esperando um cliente se conectar, mas não trava a aplicação.
            TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);
            // Assim que um cliente se conecta, o código continua. Logamos a informação de quem conectou.
            _logger.LogInformation("Cliente conectado de {RemoteEndPoint}", client.Client.RemoteEndPoint);

            // ESTA LINHA É MUITO IMPORTANTE: O PADRÃO "FIRE AND FORGET"
            // ' = ' significa que estamos descartando o resultado da tarefa.
            // Não usamos 'await' aqui. Lançamos o 'HandleClientAsync' para rodar em uma thread separada
            // e imediatamente voltamos ao topo do loop 'while' para esperar a PRÓXIMA conexão.
            // Isso permite que nosso servidor atenda múltiplos clientes ao mesmo tempo.
            _ = HandleClientAsync(client, stoppingToken);
        }
    }

    // --- O Método de Processamento do Cliente ---
    /// <summary>
    /// Método responsável por lidar com toda a comunicação de um único cliente.
    /// </summary>
    private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
    {
        // 'using (client)' garante que, ao final deste bloco, a conexão com o cliente
        // seja sempre fechada, mesmo que ocorram erros. É um 'using' normal, síncrono.
        using (client)
        // 'await using' é a versão assíncrona. A usamos aqui porque 'NetworkStream'
        // pode precisar fazer operações de rede assíncronas para se fechar corretamente.
        await using (var stream = client.GetStream())
        {
            // O bloco 'try' contém o código que pode dar erro.
            try
            {
                // Um "buffer" é um pequeno pedaço de memória (um array de bytes) para receber os dados da rede.
                // 4096 bytes (4 KB) é um tamanho comum e eficiente.
                var buffer = new byte[4096];
                // Usamos um 'StringBuilder' para montar a mensagem completa. É muito mais eficiente
                // do que concatenar strings com o operador '+', especialmente dentro de um loop.
                var messageBuilder = new StringBuilder();

                // Este loop lê os dados da rede.
                // 'await stream.ReadAsync(...)' lê os dados que chegaram e os coloca no buffer.
                // Ele retorna o número de bytes que foram lidos ('bytesRead').
                // O loop continua enquanto 'bytesRead' for diferente de 0 (ou seja, enquanto o cliente enviar dados).
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, stoppingToken)) != 0)
                {
                    // Convertemos os bytes recebidos para texto (usando o padrão UTF-8) e adicionamos ao nosso montador.
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                    // Pegamos todo o texto que já recebemos até agora.
                    string receivedData = messageBuilder.ToString();

                    // Procuramos pelos caracteres de controle do MLLP no texto recebido.
                    int start = receivedData.IndexOf(START_OF_BLOCK);
                    int end = receivedData.IndexOf(END_OF_BLOCK);

                    // Se encontramos tanto o início quanto o fim, e o fim vem depois do início...
                    if (start > -1 && end > start)
                    {
                        // ...significa que temos uma mensagem HL7 completa!
                        // Extraímos o texto que está entre os marcadores.
                        string hl7Message = receivedData.Substring(start + 1, end - start - 1);
                        _logger.LogInformation("Mensagem HL7 recebida.");

                        // ESTA É A CONEXÃO COM O RESTO DA NOSSA ARQUITETURA.
                        // Criamos um "escopo" de injeção de dependência para este processamento.
                        // Isso cria um "ambiente" isolado com um novo DbContext e uma nova UnitOfWork,
                        // só para esta mensagem.
                        await using (var scope = _serviceProvider.CreateAsyncScope())
                        {
                            // De dentro deste escopo, pedimos a ferramenta que precisamos: nosso caso de uso.
                            var useCase = scope.ServiceProvider.GetRequiredService<IReceiveOruR01UseCase>();
                            // Executamos o caso de uso, passando a mensagem HL7 que extraímos.
                            var result = await useCase.ExecuteAsync(hl7Message);

                            // Preparamos a resposta (ACK ou NAK) para ser enviada de volta,
                            // embrulhando-a novamente com os caracteres de controle do MLLP.
                            var responseMessage = $"{START_OF_BLOCK}{result.AckNackMessage}{END_OF_BLOCK}{CARRIAGE_RETURN}";
                            // Convertemos a string de resposta de volta para bytes para poder enviá-la pela rede.
                            byte[] responseBytes = Encoding.UTF8.GetBytes(responseMessage);

                            // Escrevemos os bytes da resposta de volta para o cliente.
                            await stream.WriteAsync(responseBytes, stoppingToken);
                            _logger.LogInformation("Resposta {Status} enviada.", result.IsSuccess ? "ACK" : "NAK");
                        }
                        
                        // Removemos a mensagem que acabamos de processar do nosso StringBuilder.
                        // Isso é importante caso o cliente envie múltiplas mensagens na mesma conexão.
                        messageBuilder.Remove(0, end + 2); // +2 para remover <FS><CR> também.
                    }
                }
            }
            // O bloco 'catch' é a nossa rede de segurança. Se qualquer erro acontecer no 'try'...
            catch (Exception ex)
            {
                // ...nós o capturamos e registramos no log para futura investigação.
                _logger.LogError(ex, "Erro ao processar cliente.");
            }
            // O bloco 'finally' é executado SEMPRE no final, dando erro ou não.
            finally
            {
                // Logamos que o cliente se desconectou.
                _logger.LogInformation("Cliente desconectado.");
            }
        }
    }
}