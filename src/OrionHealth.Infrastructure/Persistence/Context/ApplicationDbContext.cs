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

   // ####################################################################################
    // # MÉTODO: OnModelCreating - A "Planta Baixa" do Banco de Dados                     #
    // ####################################################################################

    /// <summary>
    /// Este método é chamado pelo Entity Framework quando ele está construindo o modelo
    /// do banco de dados pela primeira vez. É aqui que definimos todas as regras de
    /// mapeamento entre nossas classes C# e as tabelas do Oracle.
    /// </summary>
    /// <param name="modelBuilder">O "construtor de modelo", a ferramenta que usamos para definir as regras.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Chama a implementação base do método, uma boa prática a ser mantida.
        base.OnModelCreating(modelBuilder);

        // REGRA GERAL: Define um "schema" padrão para o Oracle. Ajuda a organizar as tabelas.
        // Todas as tabelas criadas por este DbContext pertencerão ao schema 'ORIONHEALTH'.
        modelBuilder.HasDefaultSchema("ORIONHEALTH");

        // ####################################################################################
        // # MAPEAMENTO DA ENTIDADE/TABELA: Patient -> PATIENTS                               #
        // ####################################################################################

        // 'modelBuilder.Entity<Patient>()' inicia a configuração para a nossa classe 'Patient'.
        // Usamos uma expressão lambda 'entity => { ... }' para definir todas as regras para ela.
        modelBuilder.Entity<Patient>(entity =>
        {
            // REGRA: Mapeia a classe 'Patient' para uma tabela chamada "PATIENTS".
            entity.ToTable("PATIENTS");

            // REGRA: Define a propriedade 'Id' como a Chave Primária (Primary Key) da tabela.
            entity.HasKey(p => p.Id);

            // REGRA: Configurações detalhadas para a propriedade 'Id'.
            entity.Property(p => p.Id)
                // Define o nome da coluna no banco como "ID" (padrão Oracle em maiúsculas).
                .HasColumnName("ID")
                // Informa ao EF que o valor desta coluna é gerado pelo banco de dados na inserção
                // (ex: IDENTITY, AUTO_INCREMENT).
                .ValueGeneratedOnAdd();

            // REGRA: Configurações para a propriedade 'MedicalRecordNumber'.
            entity.Property(p => p.MedicalRecordNumber)
                .HasColumnName("MEDICAL_RECORD_NUMBER")
                // Garante que esta coluna não pode ser nula.
                .IsRequired();

            // REGRA DE NEGÓCIO CRÍTICA: Garante que não podem existir dois pacientes com o mesmo MRN.
            // Cria um "Índice Único" (Unique Index) na coluna, o que otimiza buscas e impõe a unicidade.
            entity.HasIndex(p => p.MedicalRecordNumber).IsUnique();
            
            // REGRA: Configurações para a propriedade 'FullName'.
            entity.Property(p => p.FullName)
                .HasColumnName("FULL_NAME")
                .IsRequired();

            // REGRA: Configurações para a propriedade 'DateOfBirth'.
            // Como 'DateOfBirth' é um 'DateTime?' (nullable) na classe, o EF já entende
            // que a coluna no banco também pode ser nula, então não precisamos do '.IsRequired(false)'.
            entity.Property(p => p.DateOfBirth)
                .HasColumnName("DATE_OF_BIRTH");
        });

        // ####################################################################################
        // # MAPEAMENTO DA ENTIDADE/TABELA: ObservationResult -> OBSERVATION_RESULTS          #
        // ####################################################################################
        modelBuilder.Entity<ObservationResult>(entity =>
        {
            // REGRA: Mapeia a classe 'ObservationResult' para a tabela "OBSERVATION_RESULTS".
            entity.ToTable("OBSERVATION_RESULTS");

            // REGRA: Define a propriedade 'Id' como a Chave Primária.
            entity.HasKey(or => or.Id);

            // REGRA: Configura a coluna 'Id' para ser gerada pelo banco.
            entity.Property(or => or.Id)
                .HasColumnName("ID")
                .ValueGeneratedOnAdd();
            
            // REGRA: Configura a propriedade 'PatientId', que será nossa Chave Estrangeira (Foreign Key).
            entity.Property(or => or.PatientId)
                .HasColumnName("PATIENT_ID")
                .IsRequired();

            // REGRA: Configura as propriedades que não podem ser nulas.
            entity.Property(or => or.ObservationId).HasColumnName("OBSERVATION_ID").IsRequired();
            entity.Property(or => or.ObservationValue).HasColumnName("OBSERVATION_VALUE").IsRequired();
            entity.Property(or => or.Status).HasColumnName("STATUS").IsRequired();
            
            // REGRA: Configura as propriedades que podem ser nulas.
            entity.Property(or => or.ObservationText).HasColumnName("OBSERVATION_TEXT");
            entity.Property(or => or.Units).HasColumnName("UNITS");
            entity.Property(or => or.ObservationDateTime).HasColumnName("OBSERVATION_DATE_TIME");
            
            // REGRA DE RELACIONAMENTO: Esta é a regra mais importante aqui.
            // Estamos dizendo ao EF como as tabelas 'PATIENTS' e 'OBSERVATION_RESULTS' se conectam.
            entity
                // "Cada ObservationResult TEM UM Patient..."
                .HasOne<Patient>()
                // "...e cada Patient TEM MUITOS ObservationResults."
                .WithMany()
                // "A conexão entre eles é feita através da Chave Estrangeira 'PatientId' na tabela de resultados."
                .HasForeignKey(or => or.PatientId);
        });
    }
}