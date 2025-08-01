using OrionHealth.Application.Interfaces.Persistence;
using OrionHealth.Domain.Entities;
using OrionHealth.Infrastructure.Persistence.Context;

namespace OrionHealth.Infrastructure.Persistence.Repositories;

// Implementação simples do repositório de resultados.
public class ObservationResultRepository : IObservationResultRepository
{
    private readonly ApplicationDbContext _context;

    public ObservationResultRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // A única responsabilidade deste repositório, por enquanto, é adicionar.
    public void Add(ObservationResult observationResult)
    {
        _context.ObservationResults.Add(observationResult);
    }
}