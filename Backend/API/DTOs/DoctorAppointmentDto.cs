using ClinicAppointmentBooking.API.Models;

namespace ClinicAppointmentBooking.API.DTOs;

/// <summary>
/// Detailed appointment record for a doctor.
/// </summary>
/// <param name="AppointmentId">The appointment ID.</param>
/// <param name="PatientId">The patient ID.</param>
/// <param name="Patient">Patient details.</param>
/// <param name="StartTime">Appointment start date and time.</param>
/// <param name="AppointmentStatus">Current status: Upcoming, Completed, or Cancelled.</param>
public record DoctorAppointmentDto(int AppointmentId, int PatientId, Patient Patient, DateTime StartTime, string AppointmentStatus);
