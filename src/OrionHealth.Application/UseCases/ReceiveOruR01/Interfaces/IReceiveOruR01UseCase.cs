namespace OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;

public interface IReceiveOruR01UseCase
{
    // Uma classe simples para agrupar o resultado do processamento.
    // 'record' é um tipo especial de classe em C# 9+, ideal para carregar dados.
    // Ele já vem com várias funcionalidades prontas, como comparação de valores.
    public record Hl7ProcessingResult(bool IsSuccess, string AckNackMessage);
    
    // O único método do nosso caso de uso.
    // Ele recebe o texto da mensagem HL7.
    // E retorna de forma assíncrona um 'Hl7ProcessingResult', que nos diz se deu certo ou não,
    // e qual a mensagem de resposta (ACK ou NAK) que devemos enviar de volta.
    Task<Hl7ProcessingResult> ExecuteAsync(string hl7Message);

}