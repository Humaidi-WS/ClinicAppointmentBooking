using ClinicAppointmentBooking.API.DTOs;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ClinicAppointmentBooking.API.Models;

public class Doctor : Person
{
    public required string Specialty { get; set; }

    [NotMapped]
    [JsonIgnore]
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

}
