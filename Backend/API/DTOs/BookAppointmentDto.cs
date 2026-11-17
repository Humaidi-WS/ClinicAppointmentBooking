namespace ClinicAppointmentBooking.API.DTOs;

/// <summary>
/// Request payload for booking a new appointment with a doctor.
/// </summary>
/// <param name="DoctorId">The unique ID of the doctor to book with.</param>
/// <param name="StartTime">The requested appointment date and time (ISO 8601). Must be within clinic hours (08:00-16:00, Sun-Thu) on exact 30-minute intervals.</param>
/// <param name="PatientFirstName">Patient's first name.</param>
/// <param name="PatientLastName">Patient's last name.</param>
/// <param name="PatientPhoneNumber">Patient's contact phone number.</param>
/// <param name="PatientDateOfBirth">Patient's date of birth (YYYY-MM-DD).</param>
public record BookAppointmentRequest(
    int DoctorId,
    DateTime StartTime,
    string PatientFirstName,
    string PatientLastName,
    string PatientPhoneNumber,
    DateOnly PatientDateOfBirth
);

/// <summary>
/// Response payload returned after successfully booking an appointment.
/// </summary>
/// <param name="Message">Confirmation message containing instructions and patient ID.</param>
/// <param name="AppointmentId">The created appointment ID.</param>
/// <param name="PatientId">The unique ID assigned to the patient.</param>
public record BookAppointmentResponse(
    string Message,
    int AppointmentId,
    int PatientId
);
