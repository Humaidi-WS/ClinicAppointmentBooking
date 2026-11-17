using System.Net;
using System.Net.Http.Json;
using ClinicAppointmentBooking.API.Data;
using ClinicAppointmentBooking.API.DTOs;
using ClinicAppointmentBooking.API.Models;
using ClinicAppointmentBooking.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicAppointmentBooking.Tests;

[Collection(ClinicApiCollection.Name)]
public sealed class PatientAppointmentsEndpointTests : ApiTestBase
{
    public PatientAppointmentsEndpointTests(ClinicApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetPatientAppointments_UnknownPatient_ReturnsEmptyList()
    {
        var response = await Client.GetAsync("/api/Appointments/patients/9999/appointments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var appointments = await response.Content.ReadFromJsonAsync<List<PatientAppointmentDto>>(Json);
        Assert.NotNull(appointments);
        Assert.Empty(appointments);
    }

    [Fact]
    public async Task GetPatientAppointments_ReturnsThatPatientsAppointmentsNewestFirst()
    {
        var james = await DoctorNamed("James", "Anderson");
        var sarah = await DoctorNamed("Sarah", "Mitchell");
        var earlier = await BookAsync(james.Id, At(2026, 11, 18, 9, 0), "Ada", "Lovelace", "5550100");
        var later = await BookAsync(sarah.Id, At(2026, 11, 19, 15, 0), "Ada", "Lovelace", "5550101");

        var ada = await GetAppointmentsAsync(later.PatientId);
        var firstAda = await GetAppointmentsAsync(earlier.PatientId);

        Assert.Equal(2, ada.Count + firstAda.Count);
        Assert.Single(ada);
        Assert.Single(firstAda);
        Assert.Equal(later.AppointmentId, ada[0].AppointmentId);
        Assert.Equal("Sarah Mitchell", ada[0].DoctorName);
        Assert.Equal("Dermatology", ada[0].DoctorSpecialty);
        Assert.Equal(earlier.AppointmentId, firstAda[0].AppointmentId);
        Assert.Equal("James Anderson", firstAda[0].DoctorName);
    }

    [Fact]
    public async Task GetPatientAppointments_IncludesCancelledAppointments()
    {
        var doctor = await DoctorNamed("Emily", "Carter");
        var upcoming = await BookAsync(doctor.Id, At(2026, 11, 18, 11, 0), phone: "5550100");
        var cancelled = await BookAsync(doctor.Id, At(2026, 11, 18, 11, 30), phone: "5550101");
        Assert.Equal(HttpStatusCode.OK, (await CancelAsync(cancelled.AppointmentId, cancelled.PatientId)).StatusCode);

        var cancelledList = await GetAppointmentsAsync(cancelled.PatientId);
        var upcomingList = await GetAppointmentsAsync(upcoming.PatientId);

        Assert.Equal("Cancelled", Assert.Single(cancelledList).AppointmentStatus);
        Assert.Equal("Upcoming", Assert.Single(upcomingList).AppointmentStatus);
    }

    [Fact]
    public async Task GetPatientAppointments_OrdersByStartTimeDescending()
    {
        var doctor = await DoctorNamed("Michael", "Roberts");
        var morning = await BookAsync(doctor.Id, At(2026, 11, 23, 8, 0));
        var afternoon = await BookAsync(doctor.Id, At(2026, 11, 23, 14, 30), firstName: "Grace", lastName: "Hopper");

        var morningList = await GetAppointmentsAsync(morning.PatientId);
        var afternoonList = await GetAppointmentsAsync(afternoon.PatientId);

        Assert.Equal(At(2026, 11, 23, 8, 0), Assert.Single(morningList).StartTime);
        Assert.Equal(At(2026, 11, 23, 14, 30), Assert.Single(afternoonList).StartTime);
    }

    [Fact]
    public async Task GetPatientAppointments_SamePatientWithTwoBookings_IsNewestFirst()
    {
        var doctor = await DoctorNamed("Daniel", "Wilson");
        var first = await BookAsync(doctor.Id, At(2026, 11, 18, 8, 30));

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Appointments.Add(new Appointment
        {
            DoctorId = doctor.Id,
            PatientId = first.PatientId,
            StartTime = At(2026, 11, 24, 15, 0),
            AppointmentStatus = "Upcoming"
        });
        await db.SaveChangesAsync();

        var appointments = await GetAppointmentsAsync(first.PatientId);

        Assert.Equal(2, appointments.Count);
        Assert.Equal(At(2026, 11, 24, 15, 0), appointments[0].StartTime);
        Assert.Equal(At(2026, 11, 18, 8, 30), appointments[1].StartTime);
        Assert.All(appointments, appointment => Assert.Equal("Daniel Wilson", appointment.DoctorName));
        Assert.All(appointments, appointment => Assert.Equal("Orthopedics", appointment.DoctorSpecialty));
    }

    private async Task<List<PatientAppointmentDto>> GetAppointmentsAsync(int patientId)
    {
        var response = await Client.GetAsync($"/api/Appointments/patients/{patientId}/appointments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var appointments = await response.Content.ReadFromJsonAsync<List<PatientAppointmentDto>>(Json);
        Assert.NotNull(appointments);
        return appointments;
    }
}
