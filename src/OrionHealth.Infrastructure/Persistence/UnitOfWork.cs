using OrionHealth.Application.Interfaces.Persistence;
using OrionHealth.Infrastructure.Persistence.Context;
using OrionHealth.Infrastructure.Persistence.Repositories;
using System.Data; // Para o IDbConnection

namespace OrionHealth.Infrastructure.Persistence;

// Nossa Unit of Work implementa o contrato IUnitOfWork.
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IDbConnection _dbConnection;

    // As propriedades dos repositórios. Usamos uma técnica chamada "backing field"
    // para inicializar os repositórios apenas na primeira vez que eles forem pedidos.
    private IPatientRepository? _patientRepository;
    private IObservationResultRepository? _observationResultRepository;

    public UnitOfWork(ApplicationDbContext context, IDbConnection dbConnection)
    {
        _context = context;
        _dbConnection = dbConnection;
    }

    // Implementação da propriedade do repositório de pacientes.
    public IPatientRepository Patients =>
        // O operador '??=' é um atalho para:
        // "Se _patientRepository for nulo, crie um novo PatientRepository e atribua a ele.
        // Depois, retorne o valor."
        _patientRepository ??= new PatientRepository(_context, _dbConnection);

    public IObservationResultRepository ObservationResults =>
        _observationResultRepository ??= new ObservationResultRepository(_context);

    // A implementação do método que salva tudo no banco.
    public async Task<int> SaveChangesAsync()
    {
        // Chamamos o método do EF Core, que é assíncrono e faz todo o trabalho pesado.
        return await _context.SaveChangesAsync();
    }

    // Implementação do método Dispose, exigido pela interface IDisposable.
    // Garante que a conexão com o banco seja fechada corretamente.
    public void Dispose()
    {
        _context.Dispose();
        _dbConnection.Dispose();
        // GC.SuppressFinalize(this) é uma otimização para o Garbage Collector do .NET.
        GC.SuppressFinalize(this);
    }
}