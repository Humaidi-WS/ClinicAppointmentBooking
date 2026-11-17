namespace ClinicAppointmentBooking.API.DTO;

/// <summary>
/// Request payload to cancel an upcoming appointment.
/// </summary>
/// <param name="AppointmentId">The ID of the appointment to cancel.</param>
/// <param name="PatientId">The patient ID associated with the appointment for verification.</param>
public record CancelAppointmentRequest(int AppointmentId, int PatientId);

/// <summary>
/// Response payload returned after successfully cancelling an appointment.
/// </summary>
/// <param name="Message">Status confirmation message.</param>
public record CancelAppointmentResponse(string Message);
