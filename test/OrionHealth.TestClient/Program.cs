// ####################################################################################
// # USINGS - As "caixas de ferramentas" que nosso programa precisa.                  #
// ####################################################################################
// Importa tipos básicos do C#, como a classe Console.
using System;
// Importa as classes para comunicação de rede segura (TLS/SSL), como a SslStream.
using System.Net.Security;
// Importa as classes para comunicação de rede TCP, como a TcpClient.
using System.Net.Sockets;
// Importa classes para converter texto em bytes e vice-versa (codificação).
using System.Text;
// Importa as ferramentas para trabalhar com programação assíncrona (async/await).
using System.Threading.Tasks;

// --- Cliente de Teste MLLP/TLS em C# (Versão Final com Main Async) ---

// Envolvemos toda a nossa lógica em uma classe e um método Main para criar um executável de console.
class Program
{
    // 'static async Task Main' é o ponto de entrada da nossa aplicação.
    // 'async Task' é a chave da nossa correção: torna o método principal assíncrono.
    // Isso garante que o programa irá esperar por todas as operações de rede (await)
    // antes de encerrar, nos dando tempo para receber a resposta do servidor.
    static async Task Main(string[] args)
    {
        // --- 1. Definição da Mensagem e Conexão ---

        // Define o endereço do nosso servidor. 'localhost' refere-se à nossa própria máquina.
        string serverAddress = "localhost";
        // Define a porta de rede que nosso servidor está escutando.
        int port = 1080;

        // Define a mensagem HL7 que queremos enviar usando um "Raw String Literal".
        // A sintaxe """...""" permite criar um bloco de texto com múltiplas linhas
        // exatamente como ele é escrito, evitando erros de formatação e concatenação.
        string hl7Data = """
        MSH|^~\&|TEST_CLIENT|TEST_HOSPITAL|OrionHealth|MainHospital|20250803223000||ORU^R01|MSG_FINAL_TEST|P|2.5.1
        PID|1||98765^^^MRN||Silva^Maria||19750412|F
        OBX|1|NM|NA^Sodio||142|mEq/L|||||F
        """;
        
        // Adiciona o "envelope" MLLP à nossa mensagem HL7.
        // (char)0x0B é o caractere de Início de Bloco (<VT>).
        // '.Replace("\r\n", "\r")' garante que as quebras de linha estejam no formato HL7 (\r).
        // (char)0x1C + (char)0x0D são os caracteres de Fim de Bloco (<FS><CR>).
        string mllpMessage = (char)0x0B + hl7Data.Replace("\r\n", "\r") + (char)0x1C + (char)0x0D;

        // Muda a cor do texto no console para amarelo, para dar destaque à mensagem de status.
        Console.ForegroundColor = ConsoleColor.Yellow;
        // Informa ao usuário que a tentativa de conexão está começando.
        Console.WriteLine($"Tentando conectar em {serverAddress}:{port}...");

        // O bloco 'try' contém o código principal que pode falhar (ex: o servidor estar offline).
        try
        {
            // --- 2. Conexão TCP e Estabelecimento do Túnel TLS ---

            // 'using var' cria um cliente TCP e abre a conexão com o servidor.
            // A palavra 'using' garante que o cliente será fechado e os recursos liberados no final.
            using var client = new TcpClient(serverAddress, port);
            
            // 'await using var' cria nosso "túnel" seguro SslStream.
            // Ele "embrulha" o stream de rede normal ('client.GetStream()') para adicionar criptografia.
            await using var sslStream = new SslStream(
                // O primeiro argumento é o stream de rede que queremos proteger.
                client.GetStream(),
                // O segundo argumento ('false') diz para não fechar o stream de rede interno.
                false,
                // O terceiro argumento é um "callback" para validar o certificado do servidor.
                (sender, certificate, chain, sslPolicyErrors) => {
                    // Como nosso servidor usa um certificado autoassinado, precisamos dizer
                    // ao nosso cliente para "confiar" nele para fins de teste.
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("Aviso: Aceitando certificado autoassinado do servidor.");
                    // Retornar 'true' significa "Confie neste certificado".
                    return true;
                });

            // 'AuthenticateAsClientAsync' inicia o "aperto de mão" (Handshake) TLS.
            // É neste momento que o cliente e o servidor negociam a criptografia.
            await sslStream.AuthenticateAsClientAsync(serverAddress);
            // Se o método acima não lançou um erro, a conexão segura foi estabelecida.
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Conexão TLS estabelecida com sucesso!");
            
            // --- 3. Envio da Mensagem e Recebimento da Resposta ---

            // Converte nossa mensagem de texto (string) para um array de bytes, usando o padrão UTF-8.
            byte[] buffer = Encoding.UTF8.GetBytes(mllpMessage);
            // Escreve os bytes da nossa mensagem no túnel criptografado.
            await sslStream.WriteAsync(buffer);
            // Força o envio imediato de quaisquer dados que estejam no buffer de rede.
            await sslStream.FlushAsync();

            // Restaura a cor padrão do console.
            Console.ResetColor();
            // Imprime a mensagem HL7 (sem o envelope MLLP) que enviamos.
            Console.WriteLine("\n>>> MENSAGEM ENVIADA:");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(hl7Data);

            // Cria um buffer (um array de bytes vazio) para receber a resposta do servidor.
            byte[] responseBuffer = new byte[1024];
            // 'await' aqui é crucial: o programa irá PAUSAR e esperar pela resposta do servidor.
            // 'ReadAsync' lê os dados que chegaram e retorna o número de bytes lidos.
            int bytesRead = await sslStream.ReadAsync(responseBuffer);
            // Converte os bytes recebidos de volta para texto (string).
            string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);

            // Restaura a cor do console.
            Console.ResetColor();
            // Imprime a resposta (ACK/NAK) que recebemos do servidor.
            Console.WriteLine("\n<<< RESPOSTA RECEBIDA:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(response);
        }
        // O bloco 'catch' é a nossa rede de segurança. Se qualquer erro acontecer...
        catch (Exception ex)
        {
            // ...nós mudamos a cor para vermelho e imprimimos a mensagem de erro.
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nOcorreu um erro: {ex.Message}");
        }
        // O bloco 'finally' é executado SEMPRE no final, dando erro ou não.
        finally
        {
            // Limpamos a cor e informamos que o processo terminou.
            Console.ResetColor();
            Console.WriteLine("\nConexão fechada.");
        }
    }
}