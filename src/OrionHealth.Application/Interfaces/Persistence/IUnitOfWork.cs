namespace OrionHealth.Application.Interfaces.Persistence;

// 'IDisposable' é uma interface especial do .NET. Classes que a implementam
// informam que possuem recursos (como uma conexão com o banco) que precisam
// ser liberados corretamente ao final do uso.
public interface IUnitOfWork : IDisposable
{
    // O contrato da Unit of Work expõe os repositórios que ela gerencia.
    // Em vez da nossa aplicação pedir cada repositório separadamente,
    // ela vai pedir a Unit of Work, que já contém todos eles.
    // '{ get; }' significa que esta é uma propriedade somente leitura.
    IPatientRepository Patients { get; }
    IObservationResultRepository ObservationResults { get; }

    // E aqui está a mágica que você apontou!
    // Este é O método que de fato vai ao banco de dados e salva TODAS as
    // alterações que foram marcadas (pelos métodos 'Add').
    // Como ele faz uma operação de I/O, ele é ASSÍNCRONO (retorna uma Task).
    // Ele retorna um 'int', que geralmente é o número de linhas afetadas no banco.
    Task<int> SaveChangesAsync();
}