using OrionHealth.Domain.Entities;

namespace OrionHealth.Application.Interfaces.Persistence;
public interface IPatientRepository
{
    // Este método FAZ uma consulta ao banco, por isso é assíncrono.
    Task<Patient?> FindByMrnAsync(string mrn);

    // Este método NÃO vai ao banco de dados. Ele apenas marca o 'patient'
    // no Entity Framework como 'adicionado'. A operação de salvar de fato
    // será feita pela Unit of Work. Por isso, ele é síncrono (void).
    void Add(Patient patient);
}
