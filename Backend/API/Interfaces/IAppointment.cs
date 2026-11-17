using ClinicAppointmentBooking.API.DTOs;
using ClinicAppointmentBooking.API.Models;

namespace ClinicAppointmentBooking.API.Interfaces;

public interface IAppointment
{
    Task<List<Doctor>> GetAllDoctorsAsync(CancellationToken cancellationToken = default);
    Task<List<DateOnly>> GetDatesAsync(CancellationToken cancellationToken = default);
    Task<List<TimeSlotDto>> GetTimesAsync(int doctorId, DateOnly date, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, Appointment? Data)> BookAsync(int doctorId, DateTime startTime, string firstName, string lastName, string phoneNumber, DateOnly dateOfBirth, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CancelAsync(int appointmentId, int patientId, CancellationToken cancellationToken = default);
    Task<List<PatientAppointmentDto>> GetPatientAppointmentsAsync(int patientId, CancellationToken cancellationToken = default);
}
