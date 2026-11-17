using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicAppointmentBooking.API.Models;

public class Patient : Person
{
    public required string PhoneNumber { get; set; } = string.Empty;
    public required DateOnly DateOfBirth { get; set; }

    [NotMapped]
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
