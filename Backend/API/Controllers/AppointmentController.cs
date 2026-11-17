using ClinicAppointmentBooking.API.DTO;
using ClinicAppointmentBooking.API.DTOs;
using ClinicAppointmentBooking.API.Interfaces;
using ClinicAppointmentBooking.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

/// <summary>
/// Endpoints for managing clinic doctors, schedules, bookings, and patient appointments.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AppointmentsController(IAppointment appointmentService) : ControllerBase
{
    /// <summary>
    /// Retrieve all clinic doctors.
    /// </summary>
    /// <remarks>
    /// Returns a list of all registered doctors ordered alphabetically by first and last name, including their specialty.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of doctors.</returns>
    /// <response code="200">Successfully retrieved the list of doctors.</response>
    [HttpGet("doctors")]
    [EndpointSummary("Get all doctors")]
    [EndpointDescription("Retrieves all clinic doctors ordered alphabetically by first and last name.")]
    [ProducesResponseType(typeof(List<Doctor>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDoctors(CancellationToken cancellationToken)
    {
        var doctors = await appointmentService.GetAllDoctorsAsync(cancellationToken);
        return Ok(doctors);
    }

    /// <summary>
    /// Retrieve selectable booking dates.
    /// </summary>
    /// <remarks>
    /// Returns all available appointment dates starting today (or tomorrow if current time is after 16:00) up to 1 month ahead.
    /// Fridays and Saturdays are excluded as the clinic is closed on weekends.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of selectable dates in YYYY-MM-DD format.</returns>
    /// <response code="200">Successfully retrieved selectable dates.</response>
    [HttpGet("dates")]
    [EndpointSummary("Get selectable appointment dates")]
    [EndpointDescription("Returns bookable dates within the next month, excluding weekends (Friday and Saturday).")]
    [ProducesResponseType(typeof(List<DateOnly>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSelectableDates(CancellationToken cancellationToken)
    {
        var dates = await appointmentService.GetDatesAsync(cancellationToken);
        return Ok(dates);
    }

    /// <summary>
    /// Retrieve daily schedule and slot availability for a specific doctor.
    /// </summary>
    /// <remarks>
    /// Generates 30-minute intervals between 08:00 and 16:00 for the specified doctor and date.
    /// Each slot indicates whether it is currently available or already booked/past.
    /// Returns an empty list if the date is in the past or falls on a weekend.
    /// </remarks>
    /// <param name="doctorId">The unique ID of the doctor.</param>
    /// <param name="date">The appointment date (YYYY-MM-DD).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of 30-minute time slots with availability flags.</returns>
    /// <response code="200">Successfully retrieved the doctor's schedule.</response>
    [HttpGet("doctors/{doctorId:int}/schedule")]
    [EndpointSummary("Get doctor's day schedule")]
    [EndpointDescription("Retrieves 30-minute time slots for a doctor on a given date indicating availability.")]
    [ProducesResponseType(typeof(List<TimeSlotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDaySchedule(int doctorId, [FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var schedule = await appointmentService.GetTimesAsync(doctorId, date, cancellationToken);
        return Ok(schedule);
    }

    /// <summary>
    /// Book a new appointment with a doctor.
    /// </summary>
    /// <remarks>
    /// Creates a new patient record and books the appointment slot.
    /// Validations enforced:
    /// - Slot must not be in the past.
    /// - Clinic is closed on Fridays and Saturdays.
    /// - Working hours are 08:00 to 16:00.
    /// - Must align to exact 30-minute boundaries (e.g. 09:00:00 or 09:30:00).
    /// - Doctor must exist.
    /// - Slot must not already be booked by another patient.
    /// </remarks>
    /// <param name="request">Booking payload containing doctor ID, start time, and patient information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Booking confirmation including generated patient ID and appointment ID.</returns>
    /// <response code="201">Appointment booked successfully.</response>
    /// <response code="400">Invalid booking request or slot unavailable.</response>
    [HttpPost("book")]
    [EndpointSummary("Book an appointment")]
    [EndpointDescription("Registers a patient and reserves an appointment slot with a doctor.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BookAppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentRequest request, CancellationToken cancellationToken)
    {
        var (success, message, appointment) = await appointmentService.BookAsync(
            request.DoctorId,
            request.StartTime,
            request.PatientFirstName,
            request.PatientLastName,
            request.PatientPhoneNumber,
            request.PatientDateOfBirth,
            cancellationToken);

        if (!success)
            return BadRequest(new ErrorResponse(message));

        return CreatedAtAction(
            nameof(GetPatientAppointments),
            new { patientId = appointment!.PatientId },
            new BookAppointmentResponse(message, appointment!.Id, appointment!.PatientId));
    }

    /// <summary>
    /// Retrieve all appointments for a given patient.
    /// </summary>
    /// <remarks>
    /// Returns all appointments (both upcoming and cancelled) associated with the specified patient ID, ordered descending by start time.
    /// </remarks>
    /// <param name="patientId">The patient ID assigned during booking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of patient appointments.</returns>
    /// <response code="200">Successfully retrieved patient appointments.</response>
    [HttpGet("patients/{patientId:int}/appointments")]
    [EndpointSummary("Get patient appointments")]
    [EndpointDescription("Retrieves all appointments for a given patient by ID.")]
    [ProducesResponseType(typeof(List<PatientAppointmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatientAppointments(int patientId, CancellationToken cancellationToken)
    {
        var appointments = await appointmentService.GetPatientAppointmentsAsync(patientId, cancellationToken);
        return Ok(appointments);
    }

    /// <summary>
    /// Cancel an existing appointment.
    /// </summary>
    /// <remarks>
    /// Cancels an upcoming appointment. Requires both appointmentId and patientId to verify ownership.
    /// Appointments in the past or already cancelled cannot be cancelled.
    /// </remarks>
    /// <param name="request">Cancellation payload containing appointment ID and patient ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cancellation confirmation message.</returns>
    /// <response code="200">Appointment cancelled successfully.</response>
    /// <response code="400">Cancellation failed (e.g. permission denied, past appointment, already cancelled).</response>
    [HttpDelete("cancel")]
    [EndpointSummary("Cancel an appointment")]
    [EndpointDescription("Cancels an upcoming appointment given appointment ID and patient ID.")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CancelAppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelAppointment([FromBody] CancelAppointmentRequest request, CancellationToken cancellationToken)
    {
        var (success, message) = await appointmentService.CancelAsync(
            request.AppointmentId,
            request.PatientId,
            cancellationToken);

        if (!success)
            return BadRequest(new ErrorResponse(message));

        return Ok(new CancelAppointmentResponse(message));
    }
}
