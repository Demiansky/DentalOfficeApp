namespace DentalPatientApp.Models;

public class PatientRecord
{
    public int Id { get; set; }
    public Guid PatientId { get; set; }  // This is the only patient reference we'll keep
    public DateTime RecordDate { get; set; } = DateTime.Now;
    public string RecordType { get; set; } = string.Empty;  // e.g., "Cleaning", "Exam", "X-Ray", "Procedure"
    public string Description { get; set; } = string.Empty;
    public string Treatment { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Prescription { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string DentistName { get; set; } = string.Empty;
    
    // Remove the navigation property completely
    // public Patient Patient { get; set; } = null!;
}