using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ClinicAppointmentBooking.API.Models;

public enum AppointmentStatus
{
    Upcoming,
    Completed,
    Cancelled,
}
public class Appointment
{
    [JsonInclude]
    public int Id { get; private set; }
    public int DoctorId { get; set; }

    [NotMapped]
    public Doctor? Doctor { get; set; }

    public int PatientId { get; set; }

    [NotMapped]
    public Patient? Patient { get; set; }

    public DateTime StartTime { get; set; }
    public string AppointmentStatus { get; set; } = string.Empty;
}
