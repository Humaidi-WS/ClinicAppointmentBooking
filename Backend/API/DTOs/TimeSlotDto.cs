namespace ClinicAppointmentBooking.API.DTOs;

/// <summary>
/// A 30-minute clinic schedule slot.
/// </summary>
/// <param name="Time">Time slot in 24-hour format (e.g. "08:00", "08:30", "15:30").</param>
/// <param name="IsAvailable">True if the slot is open for booking; false if already booked or in the past.</param>
public record TimeSlotDto(string Time, bool IsAvailable);
