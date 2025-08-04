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
using OrionHealth.Application.Interfaces;
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
/// Gera uma mensagem de confirmação positiva (ACK) de forma robusta.
/// Esta versão constrói a mensagem usando as propriedades fortemente tipadas
/// dos objetos do NHapi, o que é mais seguro que usar o Terser.
/// </summary>
/// <param name="originalMessage">A mensagem original recebida.</param>
/// <returns>Uma string contendo a mensagem ACK perfeitamente formatada.</returns>
public string CreateAck(string originalMessage)
{
    // O bloco 'try' garante que, se a mensagem original for inválida,
    // a aplicação não irá quebrar ao tentar processá-la.
    try
    {
        // 1. Parse da Mensagem Original
        // Converte a string da mensagem original em um objeto HL7.
        var original = _parser.Parse(originalMessage);
        // Cria um 'Terser' para facilitar a "pesca" de campos da mensagem original.
        var terserOriginal = new Terser(original);
        // Cria um objeto 'ACK' vazio, que é a estrutura da nossa resposta.
        var ack = new ACK();

        // --- 2. Construindo o Segmento MSH Diretamente ---
        // Pega uma referência direta para o segmento MSH do nosso objeto ACK.
        var msh = ack.MSH;
        // MSH-9: Define o tipo da mensagem como "ACK".
        msh.MessageType.MessageCode.Value = "ACK";
        // Invertendo Remetente e Destinatário:
        // MSH-3 (Nosso Remetente) recebe o valor do MSH-5 (Destinatário) original.
        msh.SendingApplication.NamespaceID.Value = terserOriginal.Get("MSH-5-1");
        // MSH-4 (Nossa Unidade Remetente) recebe o valor do MSH-6 (Unidade Destinatária) original.
        msh.SendingFacility.NamespaceID.Value = terserOriginal.Get("MSH-6-1");
        // MSH-5 (Nosso Destinatário) recebe o valor do MSH-3 (Remetente) original.
        msh.ReceivingApplication.NamespaceID.Value = terserOriginal.Get("MSH-3-1");
        // MSH-6 (Nossa Unidade Destinatária) recebe o valor do MSH-4 (Unidade Remetente) original.
        msh.ReceivingFacility.NamespaceID.Value = terserOriginal.Get("MSH-4-1");
        // MSH-7: Define a data e hora da mensagem com o valor atual, já no formato HL7 correto.
        msh.DateTimeOfMessage.Time.SetLongDateWithSecond(DateTime.Now);
        // MSH-10: Cria um novo ID de controle único para a nossa mensagem de ACK.
        msh.MessageControlID.Value = Guid.NewGuid().ToString().Substring(0, 20);
        // MSH-11: Define o código de processamento como "P" (Produção).
        msh.ProcessingID.ProcessingID.Value = "P";
        // MSH-12: Define a versão do HL7 como "2.5.1".
        msh.VersionID.VersionID.Value = "2.5.1";
        
        // --- 3. Construindo o Segmento MSA Diretamente ---
        // Pega uma referência direta para o segmento MSA (Message Acknowledgment).
        var msa = ack.MSA;
        // MSA-1: Define o código de confirmação como "AA" (Application Accept), indicando sucesso.
        msa.AcknowledgmentCode.Value = "AA";
        // MSA-2: Copia o ID de controle da mensagem original, para que o sistema de origem
        // saiba exatamente qual mensagem estamos confirmando.
        msa.MessageControlID.Value = terserOriginal.Get("MSH-10-1");

        // 4. Codifica e Retorna
        // Converte o objeto 'ack' de volta para uma string no formato HL7 e a retorna.
        return _parser.Encode(ack);
    }
    // O 'catch' é nosso plano B caso a mensagem original seja tão inválida
    // que nem consigamos fazer o parse dela.
    catch (Exception e)
    {
        // Retorna uma string de erro genérica.
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

    // ####################################################################################
    // # MÉTODO: ParseOruR01 - O coração do parser. Extrai os dados.                      #
    // # Esta é a versão final e mais robusta, que acessa os dados diretamente.          #
    // ####################################################################################

    /// <summary>
    /// Faz o parse de uma mensagem ORU_R01 e extrai os dados do paciente e dos resultados.
    /// Este método usa parâmetros 'out', o que significa que ele vai "preencher" as variáveis
    /// 'patient' e 'results' que forem passadas para ele, em vez de retornar um único valor.
    /// </summary>
    /// <param name="hl7Message">A mensagem ORU_R01 em formato de texto.</param>
    /// <param name="patient">O objeto Paciente que será preenchido (saída).</param>
    /// <param name="results">A lista de Resultados que será preenchida (saída).</param>
    public void ParseOruR01(string hl7Message, out Patient patient, out List<ObservationResult> results)
    {
        // ####################################################################################
        // # 1. O Parse Inicial                                                               #
        // ####################################################################################
        // Usamos o parser do NHapi para converter o texto da mensagem em um objeto HL7.
        // 'as ORU_R01' tenta converter ("cast") o objeto para o tipo específico que esperamos.
        // O operador '??' (null-coalescing) é um atalho para: "Se a conversão falhar e o resultado
        // for nulo, lance uma exceção imediatamente". Isso garante que só continuaremos se a
        // mensagem for realmente do tipo ORU_R01.
        var oruMessage = _parser.Parse(hl7Message) as ORU_R01
            ?? throw new ArgumentException("A mensagem fornecida não é um ORU_R01 válido.");

        // ####################################################################################
        // # 2. Acesso Direto aos Segmentos                                                   #
        // ####################################################################################
        // Navegamos diretamente na estrutura do objeto que o NHapi criou. Esta é a forma
        // mais segura e recomendada, pois evita os erros que tivemos com o Terser.
        // O caminho 'GetPATIENT_RESULT().PATIENT.PID' nos dá acesso direto ao segmento PID
        // (Patient Identification), que contém os dados demográficos do paciente.
        var pidSegment = oruMessage.GetPATIENT_RESULT().PATIENT.PID;

        // ####################################################################################
        // # 3. Preenchendo os Dados do Paciente                                              #
        // ####################################################################################
        // Criamos um novo objeto 'Patient' do nosso domínio, que será preenchido a seguir.
        patient = new Patient
        {
            // PID-3 (Patient Identifier List): Este campo pode se repetir.
            // 'GetPatientIdentifierList()' nos retorna a lista de todos os identificadores.
            // '.FirstOrDefault()' pega o primeiro item da lista (ou nulo, se a lista estiver vazia).
            // O operador '?.' (null-conditional) é uma segurança: "Se FirstOrDefault não for nulo,
            // continue para pegar o IDNumber. Se for nulo, a expressão inteira se torna nula".
            // O operador '??' (null-coalescing) é a segurança final: "Se a expressão inteira
            // resultar em nulo, use uma string vazia como valor padrão".
            MedicalRecordNumber = pidSegment.GetPatientIdentifierList().FirstOrDefault()?.IDNumber.Value ?? string.Empty,

            // PID-5 (Patient Name): Este campo também pode se repetir (para nomes alternativos).
            // 'GetPatientName(0)' pega o primeiro e principal nome do paciente.
            // Em seguida, acessamos as partes do nome (FamilyName, GivenName) e as concatenamos.
            FullName = pidSegment.GetPatientName(0).FamilyName.Surname.Value + " " + pidSegment.GetPatientName(0).GivenName.Value,

            // PID-7 (Date/Time of Birth): Acessamos o campo de data de nascimento.
            // O NHapi nos dá a data como uma 'string' no campo '.Time.Value'.
            // Nós DEVEMOS usar nosso método auxiliar 'ParseHl7Date' para converter essa string
            // para um objeto DateTime? de forma segura.
            DateOfBirth = ParseHl7Date(pidSegment.DateTimeOfBirth.Time.Value)
        };

        // ####################################################################################
        // # 4. Preparando a Lista de Resultados                                              #
        // ####################################################################################
        // Inicializamos a lista que guardará todos os resultados de exame encontrados na mensagem.
        results = new List<ObservationResult>();

        // ####################################################################################
        // # 5. Iterando sobre os Resultados                                                  #
        // ####################################################################################
        // Usamos um laço 'for' clássico, que é a maneira fundamental e garantida de
        // iterar sobre grupos que se repetem no NHapi.

        // 'oruMessage.GetPATIENT_RESULT().ORDER_OBSERVATIONRepetitionsUsed' nos diz QUANTOS
        // grupos de "ordem de exame" existem na mensagem.
        for (int i = 0; i < oruMessage.GetPATIENT_RESULT().ORDER_OBSERVATIONRepetitionsUsed; i++)
        {
            // 'GetORDER_OBSERVATION(i)' nos dá o grupo específico na posição 'i' do laço.
            var orderObservation = oruMessage.GetPATIENT_RESULT().GetORDER_OBSERVATION(i);
            
            // Dentro de cada ordem, pode haver múltiplos resultados (observações).
            // Repetimos a mesma lógica de laço 'for' para o grupo de observação.
            for (int j = 0; j < orderObservation.OBSERVATIONRepetitionsUsed; j++)
            {
                // Pegamos o grupo de observação específico na posição 'j'.
                var observation = orderObservation.GetOBSERVATION(j);
                // E de dentro dele, pegamos o segmento OBX, que contém os dados que realmente queremos.
                var obx = observation.OBX;

                // ####################################################################################
                // # 6. Preenchendo os Dados do Resultado do Exame                                    #
                // ####################################################################################
                // Com os dados do segmento OBX em mãos, criamos um novo objeto 'ObservationResult'
                // do nosso domínio e o adicionamos à nossa lista de resultados.
                results.Add(new ObservationResult
                {
                    // OBX-3: Identificador do Exame (ex: "GLUC" para Glicose).
                    ObservationId = obx.ObservationIdentifier.Identifier.Value,
                    // OBX-3: Texto do Exame (ex: "Nível de Glicose Sanguínea").
                    ObservationText = obx.ObservationIdentifier.Text.Value,
                    // OBX-5: O valor do resultado. 'GetObservationValue(0).Data' pega o primeiro
                    // valor do resultado. Convertemos para string de forma segura com '?? string.Empty'.
                    ObservationValue = obx.GetObservationValue(0).Data.ToString() ?? string.Empty,
                    // OBX-6: As unidades do resultado (ex: "mg/dL").
                    Units = obx.Units.Text.Value,
                    // OBX-14: A data/hora da observação. Usamos nosso helper 'ParseHl7Date' para
                    // converter a string de data que o NHapi nos fornece.
                    ObservationDateTime = ParseHl7Date(obx.DateTimeOfTheObservation.Time.Value),
                    // OBX-11: O status do resultado (ex: "F" para Final, "C" para Corrigido).
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