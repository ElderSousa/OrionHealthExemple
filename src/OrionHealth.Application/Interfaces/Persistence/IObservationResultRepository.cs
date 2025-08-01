using OrionHealth.Domain.Entities;

namespace OrionHealth.Application.Interfaces.Persistence;

public interface IObservationResultRepository
{
    // O contrato para adicionar um novo resultado de exame.
    void Add(ObservationResult observationResult);
}