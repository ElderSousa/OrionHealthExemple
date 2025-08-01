namespace OrionHealth.Domain.Entities;

public class Patient
{
    public long Id { get; set; }
    public string MedicalRecordNumber { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public DateTime? DateOfBirth { get; set; }
}