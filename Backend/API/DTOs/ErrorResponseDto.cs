namespace ClinicAppointmentBooking.API.DTOs;

/// <summary>
/// Error response returned when a request fails validation or business constraints.
/// </summary>
/// <param name="Error">Descriptive error message explaining why the request failed.</param>
public record ErrorResponse(string Error);
