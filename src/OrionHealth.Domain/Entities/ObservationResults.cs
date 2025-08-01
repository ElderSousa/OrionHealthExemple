namespace OrionHealth.Domain.Entities;

public class ObservationResult
{
    // Chave primária do resultado no nosso banco de dados.
    public long Id { get; set; }

    // Esta é a "Chave Estrangeira" (Foreign Key). Ela liga este resultado a um Paciente.
    // Todo resultado pertence a um paciente.
    public long PatientId { get; set; }

    // O código universal do exame. Ex: "GLUC" para Glicose.
    public string ObservationId { get; set; } = null!;

    // O nome do exame por extenso. Ex: "Nível de Glicose Sanguínea".
    // Pode ser nulo, pois nem sempre vem na mensagem.
    public string? ObservationText { get; set; }

    // O valor que o exame retornou. Ex: "105", "NEGATIVO", etc.
    public string ObservationValue { get; set; } = null!;

    // A unidade de medida do resultado. Ex: "mg/dL".
    public string? Units { get; set; }

    // A data e hora em que o exame foi realizado ou o resultado foi emitido.
    public DateTime? ObservationDateTime { get; set; }

    // O status do resultado. Ex: "F" para Final, "C" para Corrigido.
    public string Status { get; set; } = null!;

}