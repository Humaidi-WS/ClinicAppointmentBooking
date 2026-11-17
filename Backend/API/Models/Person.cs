using System.Text.Json.Serialization;

namespace ClinicAppointmentBooking.API.Models;

public abstract class Person
{
    [JsonInclude]
    public int Id { get; protected set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}
