// Namespaces que nosso arquivo vai usar.
namespace OrionHealth.Application.UseCases.ReceiveOruR01;

using OrionHealth.Application.Interfaces;
using OrionHealth.Application.Interfaces.Persistence;
using OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;
using System.Threading.Tasks; // Necessário para usar Task
using static OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces.IReceiveOruR01UseCase;

// Esta é a classe CONCRETA que implementa o contrato 'IReceiveOruR01UseCase'.
// O ':' significa "esta classe segue as regras definidas por esta interface".
public class ReceiveOruR01UseCase : IReceiveOruR01UseCase
{
    // Campos privados e somente leitura ('private readonly').
    // Eles guardarão as "ferramentas" (dependências) que nossa classe precisa para trabalhar.
    // 'readonly' significa que seu valor só pode ser definido no construtor, garantindo
    // que não será alterado depois.
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHL7Parser _hl7Parser;

    // Este é o "Construtor" da classe. Ele é chamado quando um objeto 'ReceiveOruR01UseCase' é criado.
    // É aqui que a mágica da Injeção de Dependência acontece.
    // Em vez de a classe criar suas ferramentas (new UnitOfWork(), new Hl7Parser()),
    // ela recebe as ferramentas prontas como parâmetros.
    public ReceiveOruR01UseCase(IUnitOfWork unitOfWork, IHL7Parser hl7Parser)
    {
        // Atribuímos as ferramentas recebidas aos nossos campos privados para poder usá-las
        // em outros métodos da classe.
        _unitOfWork = unitOfWork;
        _hl7Parser = hl7Parser;
    }

    // A implementação do método do nosso contrato.
    // 'async' indica que este método pode ter operações que serão "aguardadas" (await).
    public async Task<Hl7ProcessingResult> ExecuteAsync(string hl7Message)
    {
        // 'try...catch' é um bloco de segurança.
        // O código dentro do 'try' é executado. Se QUALQUER erro acontecer,
        // a execução pula para o bloco 'catch' em vez de quebrar a aplicação inteira.
        try
        {
            // 1. Usamos nossa ferramenta de parsing para traduzir a mensagem.
            // 'out var' é um atalho para declarar as variáveis patient e results
            // diretamente na chamada do método.
            _hl7Parser.ParseOruR01(hl7Message, out var patient, out var results);

            // 2. Usamos a Unit of Work para acessar o repositório de pacientes.
            // O 'await' aqui pausa a execução do método ATÉ que a consulta ao banco termine,
            // mas libera o "garçom" (thread) para fazer outras coisas.
            var existingPatient = await _unitOfWork.Patients.FindByMrnAsync(patient.MedicalRecordNumber);

            // 3. Lógica de negócio: se o paciente não existe, adicione.
            if (existingPatient is null)
            {
                _unitOfWork.Patients.Add(patient);
            }
            else
            {
                // Se o paciente já existe, vamos usar o objeto que veio do banco (existingPatient)
                // e apenas atualizar seu nome, caso tenha mudado. O Entity Framework é inteligente
                // o suficiente para detectar essa mudança.
                // E garantimos que os novos resultados serão associados ao ID do paciente que já existe.
                existingPatient.FullName = patient.FullName;
                patient = existingPatient;
            }

            // 4. Adiciona todos os resultados de exame.
            foreach (var result in results)
            {
                // Associamos cada resultado ao paciente correto.
                result.PatientId = patient.Id;
                _unitOfWork.ObservationResults.Add(result);
            }

            // 5. O momento crucial: persistir TUDO no banco de dados.
            // Esta é a chamada assíncrona que salva o paciente (novo ou atualizado) E
            // todos os seus resultados em uma única transação.
            await _unitOfWork.SaveChangesAsync();

            // 6. Se tudo até aqui deu certo, criamos uma mensagem de sucesso (ACK).
            string ackMessage = _hl7Parser.CreateAck(hl7Message);
            return new Hl7ProcessingResult(true, ackMessage);
        }
        catch (Exception ex)
        {
            // 7. Se qualquer coisa no bloco 'try' deu errado, caímos aqui.
            // 'ex' é um objeto que contém os detalhes do erro.
            // Criamos uma mensagem de falha (NAK) com a mensagem de erro.
            // (Em um sistema real, poderíamos logar o erro completo 'ex' para investigação).
            string nakMessage = _hl7Parser.CreateNak(hl7Message, ex.Message);
            return new Hl7ProcessingResult(false, nakMessage);
        }
    }
}