// Usamos o namespace do Entity Framework Core.
using Microsoft.EntityFrameworkCore;
// E o namespace onde nossas entidades (Patient, ObservationResult) estão definidas.
using OrionHealth.Domain.Entities;

namespace OrionHealth.Infrastructure.Persistence.Context;

// Nossa classe de contexto herda (:) da classe DbContext do Entity Framework.
// Herdar significa que nossa classe já ganha um monte de funcionalidades prontas do EF.
public class ApplicationDbContext : DbContext
{
    // Este é o construtor. Ele recebe as opções de configuração do banco de dados
    // (como a string de conexão) e as passa para a classe base (o DbContext do EF).
    // A Injeção de Dependência vai nos fornecer essas 'options' mais tarde.
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // 'DbSet<T>' é uma propriedade do EF que representa uma tabela no banco de dados.
    // Tudo que quisermos que o EF gerencie (crie, leia, atualize, delete) precisa
    // ser declarado como um DbSet.
    public DbSet<Patient> Patients { get; set; }
    public DbSet<ObservationResult> ObservationResults { get; set; }

    // 'OnModelCreating' é um método especial que o EF chama quando está montando
    // o modelo do banco pela primeira vez. É aqui que podemos configurar detalhes
    // das nossas tabelas e colunas.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Em Oracle, é comum usar "schemas" para organizar tabelas.
        // Aqui, estamos dizendo ao EF para colocar nossas tabelas em um schema específico.
        // Em um projeto real, isso viria de um arquivo de configuração.
        modelBuilder.HasDefaultSchema("ORIONHEALTH");

        // Mapeando nossa entidade 'Patient' para uma tabela chamada 'PATIENTS'.
        modelBuilder.Entity<Patient>().ToTable("PATIENTS");
        // Mapeando nossa entidade 'ObservationResult' para uma tabela chamada 'OBSERVATION_RESULTS'.
        modelBuilder.Entity<ObservationResult>().ToTable("OBSERVATION_RESULTS");

        // Exemplo de configuração mais detalhada:
        modelBuilder.Entity<Patient>(entity =>
        {
            // Dizendo que a propriedade 'Id' é a chave primária da tabela.
            entity.HasKey(p => p.Id);

            // Dizendo que a coluna correspondente à propriedade 'Id' deve ter o nome 'ID'
            // e que o banco de dados é responsável por gerar seu valor (IDENTITY).
            entity.Property(p => p.Id).HasColumnName("ID").ValueGeneratedOnAdd();

            // Dizendo que a coluna para 'MedicalRecordNumber' deve se chamar 'MEDICAL_RECORD_NUMBER'
            // e não pode ser nula. Também criamos um índice único para garantir que não
            // existam dois pacientes com o mesmo número de prontuário.
            entity.Property(p => p.MedicalRecordNumber)
                .HasColumnName("MEDICAL_RECORD_NUMBER")
                .IsRequired();
            entity.HasIndex(p => p.MedicalRecordNumber).IsUnique();

            entity.Property(p => p.FullName).HasColumnName("FULL_NAME").IsRequired();
            entity.Property(p => p.DateOfBirth).HasColumnName("DATE_OF_BIRTH");
        });

        // Você faria configurações similares para a entidade ObservationResult aqui...
    }
}