namespace ClinicAppointmentBooking.API.DTOs;

/// <summary>
/// Patient appointment details returned when querying patient appointments.
/// </summary>
/// <param name="AppointmentId">Unique identifier of the appointment.</param>
/// <param name="DoctorId">ID of the attending doctor.</param>
/// <param name="DoctorName">Full name of the doctor (e.g. James Anderson).</param>
/// <param name="DoctorSpecialty">Medical specialty of the doctor.</param>
/// <param name="StartTime">Appointment start date and time.</param>
/// <param name="AppointmentStatus">Current status: Upcoming, Completed, or Cancelled.</param>
public record PatientAppointmentDto(int AppointmentId, int DoctorId, string DoctorName, string DoctorSpecialty, DateTime StartTime, string AppointmentStatus);
