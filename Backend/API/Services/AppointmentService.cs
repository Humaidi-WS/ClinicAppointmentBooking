using ClinicAppointmentBooking.API.Data;
using ClinicAppointmentBooking.API.DTOs;
using ClinicAppointmentBooking.API.Interfaces;
using ClinicAppointmentBooking.API.Models;
using Microsoft.EntityFrameworkCore;

public class AppointmentService(AppDbContext context, TimeProvider timeProvider) : IAppointment
{
    public async Task<List<Doctor>> GetAllDoctorsAsync(CancellationToken cancellationToken = default)
    {
        return await context.Doctors
            .AsNoTracking()
            .OrderBy(d => d.FirstName)
            .ThenBy(d => d.LastName)
            .Select(d => d)
            .ToListAsync(cancellationToken);
    }

    public Task<List<DateOnly>> GetDatesAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetLocalNow().LocalDateTime;
        var currentDate = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);

        var startDate = currentTime >= new TimeOnly(16, 0) ? currentDate.AddDays(1) : currentDate;
        var endDate = startDate.AddMonths(1);

        var dates = new List<DateOnly>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek is not DayOfWeek.Friday and not DayOfWeek.Saturday)
            {
                dates.Add(date);
            }
        }

        return Task.FromResult(dates);
    }

    public async Task<List<TimeSlotDto>> GetTimesAsync(int doctorId, DateOnly appointmentDate, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetLocalNow().DateTime;
        var todayDate = DateOnly.FromDateTime(now);

        if (appointmentDate < todayDate || appointmentDate.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday)
            return [];

        var bookedTimes = await context.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorId == doctorId
                     && DateOnly.FromDateTime(a.StartTime) == appointmentDate
                     && a.AppointmentStatus != nameof(AppointmentStatus.Cancelled))
            .Select(a => TimeOnly.FromDateTime(a.StartTime))
            .ToHashSetAsync(cancellationToken);

        var currentTime = TimeOnly.FromDateTime(now);
        var schedule = new List<TimeSlotDto>();

        for (var appointmentTime = new TimeOnly(8, 0); appointmentTime < new TimeOnly(16, 0); appointmentTime = appointmentTime.AddMinutes(30))
        {
            var status = string.Empty;
            var isAvailable = false;

            if (bookedTimes.Contains(appointmentTime) || (appointmentDate == todayDate && appointmentTime <= currentTime))
            {
                isAvailable = false;
            }
            else
            {
                isAvailable = true;
            }

            schedule.Add(new TimeSlotDto(appointmentTime.ToString("HH:mm"), isAvailable));
        }

        return schedule;
    }

    public async Task<(bool Success, string Message, Appointment? Data)> BookAsync(int doctorId, DateTime startTime, string firstName, string lastName, string phoneNumber, DateOnly dateOfBirth, CancellationToken cancellationToken = default)
    {

        var currentTime = timeProvider.GetLocalNow().DateTime;

        if (startTime <= currentTime)
            return (false, "Cannot book an appointment in the past.", null);

        if (startTime.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday)
            return (false, "The clinic is closed on Fridays and Saturdays.", null);

        var timeOfDay = TimeOnly.FromDateTime(startTime);
        if (timeOfDay < new TimeOnly(8, 0) || timeOfDay >= new TimeOnly(16, 0))
            return (false, "Time is outside working hours (08:00 to 16:00).", null);

        if ((timeOfDay.Minute is not 0 and not 30) || timeOfDay.Second is not 0 || timeOfDay.Millisecond is not 0 || timeOfDay.Microsecond is not 0 || timeOfDay.Nanosecond is not 0)
            return (false, "Appointments must be aligned to exact 30-minute slots.", null);

        if (!await context.Doctors.AnyAsync(d => d.Id == doctorId, cancellationToken))
            return (false, "Doctor not found.", null);

        // Create a new patient record for each booking
        var patient = new Patient
        {
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            DateOfBirth = dateOfBirth
        };
        context.Patients.Add(patient);
        await context.SaveChangesAsync(cancellationToken);

        bool isSlotTaken = await context.Appointments
            .AnyAsync(a => a.DoctorId == doctorId
                        && a.StartTime == startTime
                        && a.AppointmentStatus != AppointmentStatus.Cancelled.ToString(),
                      cancellationToken);

        if (isSlotTaken)
            return (false, "This time slot was just booked by someone else.", null);

        var appointment = new Appointment
        {
            DoctorId = doctorId,
            PatientId = patient.Id,
            StartTime = startTime,
            AppointmentStatus = AppointmentStatus.Upcoming.ToString()
        };

        context.Appointments.Add(appointment);
        await context.SaveChangesAsync(cancellationToken);

        return (true, $"Appointment booked successfully. Your Patient ID is {patient.Id} — save it to view your appointments.", appointment);
    }

    public async Task<(bool Success, string Message)> CancelAsync(int appointmentId, int patientId, CancellationToken cancellationToken = default)
    {
        var appointment = await context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return (false, "Appointment not found.");
        }

        if (appointment.PatientId != patientId)
        {
            return (false, "Permission denied.");
        }

        if (appointment.AppointmentStatus == AppointmentStatus.Cancelled.ToString())
        {
            return (false, "Already cancelled.");
        }

        var now = timeProvider.GetLocalNow().DateTime;

        if (appointment.StartTime <= now)
        {
            return (false, "Cannot cancel past appointments.");
        }

        appointment.AppointmentStatus = AppointmentStatus.Cancelled.ToString();
        await context.SaveChangesAsync(cancellationToken);

        return (true, "Appointment cancelled successfully.");
    }

    public async Task<List<PatientAppointmentDto>> GetPatientAppointmentsAsync(int patientId, CancellationToken cancellationToken = default)
    {
        var currentTime = timeProvider.GetLocalNow().DateTime;

        var appointments = await context.Appointments
            .AsNoTracking()
            .Include(a => a.Doctor)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.StartTime)
            .ToListAsync(cancellationToken);

        return appointments.Select(appointment => new PatientAppointmentDto(appointment.Id, appointment.DoctorId, $"{appointment.Doctor!.FirstName} {appointment.Doctor!.LastName}", appointment.Doctor!.Specialty, appointment.StartTime, appointment.AppointmentStatus)).ToList();
    }




}