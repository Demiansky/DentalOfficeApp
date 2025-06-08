using DentalPatientApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalPatientApp.Data;

public class DentalDbContext : DbContext
{
    public DentalDbContext(DbContextOptions<DentalDbContext> options) : base(options) { }

    public DbSet<PatientRecord> PatientRecords { get; set; } = null!;
    
    // Remove any reference to storing patients in PostgreSQL
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure the PatientRecord entity
        modelBuilder.Entity<PatientRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PatientId).IsRequired();  // Store just the ID reference
            entity.Property(e => e.RecordType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Treatment).HasMaxLength(1000);
            entity.Property(e => e.Diagnosis).HasMaxLength(1000);
            entity.Property(e => e.Prescription).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.DentistName).HasMaxLength(100);
            
            // No navigation property or FK relationship
        });
    }
}