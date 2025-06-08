namespace DentalPatientApp.Models;

public record Patient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime LastAppointment { get; set; }
    public DateTime? NextAppointment { get; set; }
    public string Notes { get; set; } = string.Empty;
}