// ####################################################################################
// # USING STATEMENTS - As "importações" que nosso arquivo precisa.                  #
// ####################################################################################

// Usings da biblioteca NHapi, nossa principal ferramenta para HL7.
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Base.Util;
using NHapi.Model.V251.Group;
using NHapi.Model.V251.Message;

// Usings dos nossos próprios projetos, para acessar interfaces e entidades.
using OrionHealth.Application.Interfaces.Persistence;
using OrionHealth.Domain.Entities;

// Usings padrões do .NET para funcionalidades gerais.
using System;
using System.Collections.Generic;
using System.Globalization;

// ####################################################################################
// # A CLASSE - A implementação concreta do nosso "Tradutor de HL7".                 #
// ####################################################################################

/// <summary>
/// A classe HapiParser é a nossa implementação concreta da interface IHL7Parser.
/// Ela usa a biblioteca NHapi para fazer todo o trabalho pesado de ler e escrever
/// mensagens no formato HL7 v2.
/// </summary>
public class HapiParser : IHL7Parser
{
    // Criamos uma instância do PipeParser, que é o objeto do NHapi responsável
    // por entender mensagens que usam o caractere '|' (pipe) como separador.
    // É 'private readonly' porque só será usado dentro desta classe e não vai mudar.
    private readonly PipeParser _parser = new PipeParser();

    // ----------------------------------------------------------------------------------
    // MÉTODO: CreateAck - Cria uma mensagem de confirmação de SUCESSO.
    // ----------------------------------------------------------------------------------
    /// <summary>
    /// Gera uma mensagem de confirmação positiva (ACK - Acknowledgment).
    /// </summary>
    /// <param name="originalMessage">A mensagem original recebida.</param>
    /// <returns>Uma string contendo a mensagem ACK no formato HL7.</returns>
    public string CreateAck(string originalMessage)
    {
        // Usamos um bloco try-catch como rede de segurança. Se algo der errado
        // ao processar a mensagem original, não quebramos a aplicação.
        try
        {
            // 1. Fazemos o "parse" da mensagem original de texto para um objeto HL7.
            var original = _parser.Parse(originalMessage);
            // 2. Criamos um objeto "Terser", que é uma ferramenta poderosa do NHapi
            // para "pescar" (Get) ou "definir" (Set) valores em campos específicos.
            var terserOriginal = new Terser(original);
            // 3. Pescamos o ID de controle da mensagem original (campo MSH-10).
            // Precisamos devolver esse mesmo ID na nossa resposta.
            var originalMessageControlId = terserOriginal.Get("MSH-10-1");

            // 4. Criamos um objeto ACK vazio e um Terser para ele.
            var ack = new ACK();
            var terserAck = new Terser(ack);

            // 5. Preenchemos o cabeçalho (segmento MSH) da nossa resposta.
            // Trocamos as informações de remetente e destinatário.
            terserAck.Set("MSH-3-1", terserOriginal.Get("MSH-5-1")); // Receiving App -> Sending App
            terserAck.Set("MSH-4-1", terserOriginal.Get("MSH-6-1")); // Receiving Facility -> Sending Facility
            terserAck.Set("MSH-5-1", terserOriginal.Get("MSH-3-1")); // Sending App -> Receiving App
            terserAck.Set("MSH-6-1", terserOriginal.Get("MSH-4-1")); // Sending Facility -> Receiving Facility
            // MSH-7: Data e hora da nossa mensagem. Formatamos como o HL7 espera.
            terserAck.Set("MSH-7-1", DateTime.Now.ToString("yyyyMMddHHmmss"));
            // MSH-9: O tipo da mensagem. É uma ACK.
            terserAck.Set("MSH-9-1", "ACK");
            // MSH-10: O ID de controle da nossa mensagem. Geramos um novo e único.
            terserAck.Set("MSH-10-1", Guid.NewGuid().ToString("N").Substring(0, 20));
            // MSH-11: Código de processamento. 'P' para Produção.
            terserAck.Set("MSH-11-1", "P");
            // MSH-12: Versão do HL7.
            terserAck.Set("MSH-12-1", "2.5.1");

            // 6. Preenchemos o segmento de status da mensagem (MSA).
            // MSA-1: Código de confirmação. "AA" significa "Application Accept" (Sucesso).
            terserAck.Set("MSA-1-1", "AA");
            // MSA-2: O ID da mensagem original que estamos respondendo.
            terserAck.Set("MSA-2-1", originalMessageControlId);

            // 7. Convertemos nosso objeto ACK de volta para uma string e a retornamos.
            return _parser.Encode(ack);
        }
        catch (Exception e)
        {
            // Se qualquer coisa deu errado, retornamos um NAK genérico construído na mão.
            return $"MSH|^~\\&|||||{DateTime.Now:yyyyMMddHHmmss}||ACK||P|2.5.1\rMSA|AE||{e.Message}";
        }
    }

    // ----------------------------------------------------------------------------------
    // MÉTODO: CreateNak - Cria uma mensagem de confirmação de FALHA.
    // ----------------------------------------------------------------------------------
    /// <summary>
    /// Gera uma mensagem de confirmação negativa (NAK - Negative Acknowledgment).
    /// </summary>
    /// <param name="originalMessage">A mensagem original recebida.</param>
    /// <param name="errorMessage">A mensagem de erro a ser incluída.</param>
    /// <returns>Uma string contendo a mensagem NAK no formato HL7.</returns>
    public string CreateNak(string originalMessage, string errorMessage)
    {
        // A lógica é quase idêntica à do CreateAck.
        try
        {
            var original = _parser.Parse(originalMessage);
            var terserOriginal = new Terser(original);
            var originalMessageControlId = terserOriginal.Get("MSH-10-1");

            var nak = new ACK(); // A estrutura base de um NAK é um ACK.
            var terserNak = new Terser(nak);

            // Preenchemos os mesmos campos do MSH.
            terserNak.Set("MSH-3-1", terserOriginal.Get("MSH-5-1"));
            terserNak.Set("MSH-4-1", terserOriginal.Get("MSH-6-1"));
            terserNak.Set("MSH-5-1", terserOriginal.Get("MSH-3-1"));
            terserNak.Set("MSH-6-1", terserOriginal.Get("MSH-4-1"));
            terserNak.Set("MSH-7-1", DateTime.Now.ToString("yyyyMMddHHmmss"));
            terserNak.Set("MSH-9-1", "ACK");
            terserNak.Set("MSH-10-1", Guid.NewGuid().ToString("N").Substring(0, 20));
            terserNak.Set("MSH-11-1", "P");
            terserNak.Set("MSH-12-1", "2.5.1");

            // A grande diferença está aqui:
            // MSA-1: "AE" significa "Application Error" (Erro na Aplicação).
            terserNak.Set("MSA-1-1", "AE");
            terserNak.Set("MSA-2-1", originalMessageControlId);
            // MSA-3: Usamos este campo para colocar a mensagem de erro que recebemos.
            terserNak.Set("MSA-3-1", errorMessage);

            return _parser.Encode(nak);
        }
        catch (Exception)
        {
            // Se a mensagem original for tão inválida que nem conseguimos processá-la.
            return $"MSH|^~\\&|||||{DateTime.Now:yyyyMMddHHmmss}||ACK||P|2.5.1\rMSA|AE||Mensagem HL7 mal formada.";
        }
    }

    // ----------------------------------------------------------------------------------
    // MÉTODO: ParseOruR01 - O coração do parser. Extrai os dados.
    // ----------------------------------------------------------------------------------
    /// <summary>
    /// Faz o parse de uma mensagem ORU_R01 e extrai os dados do paciente e dos resultados.
    /// </summary>
    /// <param name="hl7Message">A mensagem ORU_R01 em formato de texto.</param>
    /// <param name="patient">O objeto Paciente que será preenchido.</param>
    /// <param name="results">A lista de Resultados que será preenchida.</param>
    public void ParseOruR01(string hl7Message, out Patient patient, out List<ObservationResult> results)
    {
        // Fazemos o parse e garantimos que a mensagem é do tipo ORU_R01.
        var oruMessage = _parser.Parse(hl7Message) as ORU_R01
            ?? throw new ArgumentException("A mensagem fornecida não é um ORU_R01 válido.");

        var terser = new Terser(oruMessage);

        // Preenchemos nosso objeto de domínio 'Patient' usando o Terser para buscar os dados.
        // A sintaxe com "/" é um "caminho" dentro da estrutura da mensagem HL7.
        patient = new Patient
        {
            MedicalRecordNumber = terser.Get("/PATIENT_RESULT/PATIENT/PID-3-1"),
            FullName = $"{terser.Get("/PATIENT_RESULT/PATIENT/PID-5-1")} {terser.Get("/PATIENT_RESULT/PATIENT/PID-5-2")}",
            DateOfBirth = ParseHl7Date(terser.Get("/PATIENT_RESULT/PATIENT/PID-7-1"))
        };

        results = new List<ObservationResult>();

        // Esta é a forma correta e final de iterar sobre grupos que se repetem.
        // Pegamos a referência para o grupo PATIENT_RESULT.
        var patientResult = oruMessage.GetPATIENT_RESULT();

        // Usamos um laço 'for' para percorrer todos os grupos de "ordem de exame".
        for (int i = 0; i < patientResult.ORDER_OBSERVATIONRepetitionsUsed; i++)
        {
            // Pegamos a ordem de exame na posição 'i'.
            var orderObservation = patientResult.GetORDER_OBSERVATION(i);

            // Dentro de cada ordem, usamos outro laço 'for' para percorrer os resultados.
            for (int j = 0; j < orderObservation.OBSERVATIONRepetitionsUsed; j++)
            {
                // Pegamos o resultado na posição 'j'.
                var observation = orderObservation.GetOBSERVATION(j);
                // E de dentro dele, pegamos o segmento OBX, que contém os dados que queremos.
                var obx = observation.OBX;

                // Criamos nosso objeto de domínio e o adicionamos à lista de resultados.
                results.Add(new ObservationResult
                {
                    ObservationId = obx.ObservationIdentifier.Identifier.Value,
                    ObservationText = obx.ObservationIdentifier.Text.Value,
                    ObservationValue = obx.GetObservationValue(0).Data.ToString() ?? string.Empty,
                    Units = obx.Units.Text.Value,
                    // Usamos nosso método auxiliar para converter a data de forma segura.
                    ObservationDateTime = ParseHl7Date(obx.DateTimeOfTheObservation.Time.Value.ToString()),
                    Status = obx.ObservationResultStatus.Value
                });
            }
        }
    }

    // ----------------------------------------------------------------------------------
    // MÉTODO: ParseHl7Date - Um método auxiliar privado.
    // ----------------------------------------------------------------------------------
    /// <summary>
    /// Converte uma data no formato HL7 (string) para um objeto DateTime? do C#.
    /// </summary>
    private DateTime? ParseHl7Date(string? hl7Date)
    {
        // Se a data vier vazia ou nula, retornamos nulo.
        if (string.IsNullOrEmpty(hl7Date))
        {
            return null;
        }

        // Tentamos fazer o parse com os formatos mais comuns de data/hora e só data.
        if (DateTime.TryParseExact(hl7Date, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) ||
            DateTime.TryParseExact(hl7Date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            // Se um dos formatos funcionar, retornamos o resultado.
            return result;
        }

        // Se nenhum formato funcionar, retornamos nulo.
        return null;
    }
}