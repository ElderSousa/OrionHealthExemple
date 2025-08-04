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
        try
        {
            // 1. Parse da mensagem (continua igual).
            _hl7Parser.ParseOruR01(hl7Message, out var patient, out var results);

            // 2. Busca pelo paciente existente (continua igual).
            var existingPatient = await _unitOfWork.Patients.FindByMrnAsync(patient.MedicalRecordNumber);

            // ### LÓGICA CORRIGIDA AQUI ###
            if (existingPatient is null)
            {
                // Se o paciente é novo:
                // a) Adicionamos o paciente ao contexto do EF.
                _unitOfWork.Patients.Add(patient);
                // b) SALVAMOS apenas o paciente primeiro para que o Oracle gere um ID.
                await _unitOfWork.SaveChangesAsync();
                // Agora, nosso objeto 'patient' tem o ID correto preenchido pelo banco.
            }
            else
            {
                // Se o paciente já existe, atualizamos os dados e usamos o objeto existente.
                existingPatient.FullName = patient.FullName;
                patient = existingPatient;
            }

            // 3. Associamos os resultados ao paciente (que agora, com certeza, tem um ID).
            foreach (var result in results)
            {
                // Atribuímos o ID do paciente (novo ou existente) a cada resultado.
                result.PatientId = patient.Id;
                _unitOfWork.ObservationResults.Add(result);
            }

            // 4. SALVAMOS os resultados de exame.
            await _unitOfWork.SaveChangesAsync();

            // 5. Geração do ACK (continua igual).
            string ackMessage = _hl7Parser.CreateAck(hl7Message);
            return new Hl7ProcessingResult(true, ackMessage);
        }
        catch (Exception ex)
        {
            // Tratamento de erro (continua igual).
            string nakMessage = _hl7Parser.CreateNak(hl7Message, ex.Message);
            return new Hl7ProcessingResult(false, nakMessage);
        }
    }
}