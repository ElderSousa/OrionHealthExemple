// ####################################################################################
// # USINGS - As "caixas de ferramentas" que nosso arquivo precisa.                  #
// ####################################################################################

// Define o "endereço" desta classe, para que outras partes do programa possam encontrá-la.
namespace OrionHealth.Infrastructure.Mllp;

// Importa classes para comunicação de rede segura (TLS/SSL).
using System.Net.Security;
// Importa classes relacionadas à autenticação e protocolos de segurança.
using System.Security.Authentication;
// Importa a classe para trabalhar com certificados digitais (nossos arquivos .pfx).
using System.Security.Cryptography.X509Certificates;
// Importa as ferramentas do .NET para criar serviços de background (Hosted Services).
using Microsoft.Extensions.Hosting;
// Importa as ferramentas de logging, para podermos escrever mensagens no console.
using Microsoft.Extensions.Logging;
// Importa as ferramentas de Injeção de Dependência, para podermos criar "escopos".
using Microsoft.Extensions.DependencyInjection;
// Importa classes relacionadas a endereços de rede (IP).
using System.Net;
// Importa as classes para comunicação de rede TCP (Sockets).
using System.Net.Sockets;
// Importa classes para manipulação de texto, como StringBuilder e Encoding.
using System.Text;
// Importa o contrato (a interface) do nosso caso de uso, para podermos executá-lo.
using OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;


// ####################################################################################
// # A CLASSE - O nosso serviço "ouvinte" de rede, agora 100% funcional e seguro.     #
// ####################################################################################
public class MllpListenerService : BackgroundService
{
    // --- Campos Privados (As "Ferramentas" da Classe) ---
    private readonly ILogger<MllpListenerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly X509Certificate2 _serverCertificate;

    // --- Constantes do Protocolo MLLP ---
    private const char START_OF_BLOCK = (char)0x0B;
    private const char END_OF_BLOCK = (char)0x1C;
    private const char CARRIAGE_RETURN = (char)0x0D;

    // --- O Construtor ---
    public MllpListenerService(ILogger<MllpListenerService> logger, IServiceProvider serviceProvider)
    {
        // Guarda as dependências injetadas para uso nos outros métodos.
        _logger = logger;
        _serviceProvider = serviceProvider;
        // Define o caminho para o certificado DENTRO do contêiner Docker.
        var certPath = @"/app/certs/orionhealth.pfx";
        // Define a senha para carregar o certificado. (Em produção, viria de um cofre de segredos).
        var certPassword = "123456"; // <-- CONFIRME SE A SENHA ESTÁ CORRETA
        // Carrega o certificado do arquivo para a memória.
        _serverCertificate = new X509Certificate2(certPath, certPassword);
        // Loga uma mensagem de sucesso para confirmar que o certificado foi carregado na inicialização.
        _logger.LogInformation("Certificado do servidor '{Subject}' carregado com sucesso.", _serverCertificate.Subject);
    }

    // --- O Método Principal de Execução ---
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Cria um "escutador" TCP na porta 1080.
        var listener = new TcpListener(IPAddress.Any, 1080);
        // Inicia o escutador.
        listener.Start();
        // Loga que o servidor está pronto para receber conexões seguras.
        _logger.LogInformation("Servidor MLLP/TLS iniciado na porta 1080. Aguardando conexões...");

        // Loop principal: continua rodando enquanto a aplicação não for encerrada.
        while (!stoppingToken.IsCancellationRequested)
        {
            // Espera por uma nova conexão de cliente.
            TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);
            // Loga a conexão de um novo cliente.
            _logger.LogInformation("Cliente conectado de {RemoteEndPoint}", client.Client.RemoteEndPoint);
            // Inicia o processamento deste cliente em uma tarefa separada para não bloquear novas conexões.
            _ = HandleClientAsync(client, stoppingToken);
        }
    }

    // --- O Método de Processamento do Cliente (COM A CORREÇÃO FINAL) ---
    private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
    {
        // Garante que o cliente TCP seja fechado no final.
        using (client)
        // Garante que o stream SSL seja fechado de forma assíncrona no final.
        await using (var sslStream = new SslStream(client.GetStream(), false))
        {
            // Bloco principal de execução segura.
            try
            {
                // Inicia o "aperto de mão" TLS para estabelecer a criptografia.
                await sslStream.AuthenticateAsServerAsync(
                    serverCertificate: _serverCertificate,
                    clientCertificateRequired: false,
                    enabledSslProtocols: SslProtocols.Tls12 | SslProtocols.Tls13,
                    checkCertificateRevocation: true);
                
                // Loga o sucesso do handshake.
                _logger.LogInformation("Handshake TLS bem-sucedido. Conexão segura estabelecida.");

                // Prepara as variáveis para ler a mensagem.
                var buffer = new byte[4096];
                var messageBuilder = new StringBuilder();
                int bytesRead;

                // Loop para ler os dados da rede.
                while ((bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length, stoppingToken)) > 0)
                {
                    // Constrói a mensagem a partir dos bytes recebidos.
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                    string receivedData = messageBuilder.ToString();
                    // Procura pelos marcadores MLLP.
                    int start = receivedData.IndexOf(START_OF_BLOCK);
                    int end = receivedData.IndexOf(END_OF_BLOCK);

                    // Se uma mensagem MLLP completa foi encontrada...
                    if (start > -1 && end > start)
                    {
                        // ...extrai a mensagem HL7 pura.
                        string hl7Message = receivedData.Substring(start + 1, end - start - 1);
                        _logger.LogInformation("Mensagem HL7 segura recebida.");

                        // Cria um escopo de DI para este processamento.
                        await using (var scope = _serviceProvider.CreateAsyncScope())
                        {
                            // Pega e executa o caso de uso.
                            var useCase = scope.ServiceProvider.GetRequiredService<IReceiveOruR01UseCase>();
                            var result = await useCase.ExecuteAsync(hl7Message);
                            
                            // Prepara e envia a resposta (ACK/NAK).
                            var responseMessage = $"{START_OF_BLOCK}{result.AckNackMessage}{END_OF_BLOCK}{CARRIAGE_RETURN}";
                            byte[] responseBytes = Encoding.UTF8.GetBytes(responseMessage);
                            await sslStream.WriteAsync(responseBytes, stoppingToken);
                            await sslStream.FlushAsync(stoppingToken); // Força o envio.
                            _logger.LogInformation("Resposta {Status} segura enviada.", result.IsSuccess ? "ACK" : "NAK");
                        }
                        
                        // ######################################################
                        // # CORREÇÃO FINAL DE REDE ESTÁ AQUI                   #
                        // ######################################################
                        // Encerra a sessão TLS de forma graciosa, enviando um sinal
                        // de "close notify" para o cliente. Isso garante que o cliente
                        // receba todos os dados ANTES que a conexão TCP seja fechada pelo 'using'.
                        await sslStream.ShutdownAsync();

                        // Saímos do loop 'while' pois já processamos a mensagem e enviamos a resposta.
                        // Nosso protocolo é simples: uma mensagem por conexão.
                        break; 
                    }
                }
            }
            // Captura e loga qualquer erro durante o processamento.
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar cliente seguro.");
            }
            // O bloco 'finally' sempre é executado no final.
            finally
            {
                // Loga a desconexão do cliente.
                _logger.LogInformation("Cliente seguro desconectado.");
            }
        }
    }
}