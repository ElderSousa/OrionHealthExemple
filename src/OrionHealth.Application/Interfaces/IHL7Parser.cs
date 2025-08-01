using OrionHealth.Domain.Entities;

namespace OrionHealth.Application.Interfaces;

public interface IHL7Parser
{
    // Contrato principal de parsing.
    // Recebe o texto da mensagem HL7.
    // 'out' é uma palavra-chave especial. Ela significa que este método vai retornar DOIS valores, não apenas um.
    // Ele vai "preencher" a variável 'patient' e a variável 'results' que forem passadas para ele.
    // 'List<ObservationResult>' é uma lista (uma coleção) de objetos do tipo ObservationResult.
    // Uma mensagem HL7 ORU^R01 pode conter vários resultados para o mesmo paciente.
    void ParseOruR01(string hl7Message, out Patient patient, out List<ObservationResult> results);

    // Contrato para criar uma mensagem de sucesso (ACK).
    // Recebe a mensagem original para poder copiar informações como o ID da mensagem.
    // Retorna a mensagem de ACK como um texto (string).
    string CreateAck(string originalMessage);

    // Contrato para criar uma mensagem de erro (NAK - Negative Acknowledgment).
    // Recebe a mensagem original e o motivo do erro.
    // Retorna a mensagem de NAK como um texto.
    string CreateNak(string originalMessage, string errorMessage);
}