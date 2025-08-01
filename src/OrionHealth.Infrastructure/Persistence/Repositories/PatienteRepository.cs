using Microsoft.EntityFrameworkCore;
using OrionHealth.Application.Interfaces.Persistence;
using OrionHealth.Domain.Entities;
using OrionHealth.Infrastructure.Persistence.Context;
using Dapper; // Importamos o Dapper
using System.Data; // Importamos para usar o IDbConnection

namespace OrionHealth.Infrastructure.Persistence.Repositories;

// Esta classe implementa o contrato IPatientRepository.
public class PatientRepository : IPatientRepository
{
    // O contexto do EF, para operações de escrita (Add).
    private readonly ApplicationDbContext _context;
    // A conexão direta com o banco (via Dapper), para leituras de alta performance.
    private readonly IDbConnection _dbConnection;

    public PatientRepository(ApplicationDbContext context, IDbConnection dbConnection)
    {
        _context = context;
        _dbConnection = dbConnection;
    }

    // Implementação do método de adicionar.
    // Note que ele é síncrono (void), pois apenas marca o objeto na memória do EF.
    public void Add(Patient patient)
    {
        _context.Patients.Add(patient);
    }

    // Implementação da busca, usando Dapper para máxima performance.
    public async Task<Patient?> FindByMrnAsync(string mrn)
    {
        // A sintaxe de SQL para o Oracle usa ':' para parâmetros.
        var sql = "SELECT * FROM PATIENTS WHERE MEDICAL_RECORD_NUMBER = :mrn";

        // 'QueryFirstOrDefaultAsync' do Dapper executa a consulta de forma assíncrona
        // e retorna o primeiro resultado encontrado, ou 'null' se não encontrar nada.
        // É exatamente o que nosso contrato pedia (Task<Patient?>).
        return await _dbConnection.QueryFirstOrDefaultAsync<Patient>(sql, new { mrn });
    }
}