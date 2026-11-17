using System.Net;
using System.Net.Http.Json;
using System.Text;
using ClinicAppointmentBooking.API.DTO;
using ClinicAppointmentBooking.API.DTOs;
using ClinicAppointmentBooking.Tests.Infrastructure;

namespace ClinicAppointmentBooking.Tests;

[Collection(ClinicApiCollection.Name)]
public sealed class CancelEndpointTests : ApiTestBase
{
    public CancelEndpointTests(ClinicApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Cancel_UpcomingAppointment_ReturnsSuccessAndMarksCancelled()
    {
        var doctor = await DoctorNamed("James", "Anderson");
        var booked = await BookAsync(doctor.Id, At(2026, 11, 18, 9, 0));

        var response = await CancelAsync(booked.AppointmentId, booked.PatientId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CancelAppointmentResponse>(Json);
        Assert.NotNull(body);
        Assert.Equal("Appointment cancelled successfully.", body.Message);

        var appointments = await Client.GetAsync($"/api/Appointments/patients/{booked.PatientId}/appointments");
        var list = await appointments.Content.ReadFromJsonAsync<List<PatientAppointmentDto>>(Json);
        Assert.Equal("Cancelled", Assert.Single(list!).AppointmentStatus);
    }

    [Fact]
    public async Task Cancel_UnknownAppointment_ReturnsNotFound()
    {
        var response = await CancelAsync(appointmentId: 9999, patientId: 1);

        await AssertError(response, "Appointment not found.");
    }

    [Fact]
    public async Task Cancel_WrongPatient_ReturnsPermissionDeniedAndLeavesAppointment()
    {
        var doctor = await DoctorNamed("Sarah", "Mitchell");
        var booked = await BookAsync(doctor.Id, At(2026, 11, 18, 13, 30));

        var response = await CancelAsync(booked.AppointmentId, booked.PatientId + 1);

        await AssertError(response, "Permission denied.");
        var appointments = await Client.GetAsync($"/api/Appointments/patients/{booked.PatientId}/appointments");
        var list = await appointments.Content.ReadFromJsonAsync<List<PatientAppointmentDto>>(Json);
        Assert.Equal("Upcoming", Assert.Single(list!).AppointmentStatus);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled_ReturnsAlreadyCancelled()
    {
        var doctor = await DoctorNamed("Emily", "Carter");
        var booked = await BookAsync(doctor.Id, At(2026, 11, 19, 8, 30));
        Assert.Equal(HttpStatusCode.OK, (await CancelAsync(booked.AppointmentId, booked.PatientId)).StatusCode);

        var response = await CancelAsync(booked.AppointmentId, booked.PatientId);

        await AssertError(response, "Already cancelled.");
    }

    [Fact]
    public async Task Cancel_AtStartTime_ReturnsPastError()
    {
        var doctor = await DoctorNamed("Michael", "Roberts");
        var booked = await BookAsync(doctor.Id, At(2026, 11, 18, 10, 0));
        SetClock(2026, 11, 18, 10, 0);

        var response = await CancelAsync(booked.AppointmentId, booked.PatientId);

        await AssertError(response, "Cannot cancel past appointments.");
    }

    [Fact]
    public async Task Cancel_AfterStartTime_ReturnsPastError()
    {
        var doctor = await DoctorNamed("Daniel", "Wilson");
        var booked = await BookAsync(doctor.Id, At(2026, 11, 18, 10, 0));
        SetClock(2026, 11, 19, 9, 0);

        var response = await CancelAsync(booked.AppointmentId, booked.PatientId);

        await AssertError(response, "Cannot cancel past appointments.");
    }

    [Fact]
    public async Task Cancel_OneMinuteBeforeStart_Succeeds()
    {
        var doctor = await DoctorNamed("James", "Anderson");
        var booked = await BookAsync(doctor.Id, At(2026, 11, 18, 10, 0));
        SetClock(2026, 11, 18, 9, 59);

        var response = await CancelAsync(booked.AppointmentId, booked.PatientId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_EmptyBody_ReturnsBadRequest()
    {
        var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/Appointments/cancel")
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_MalformedJson_ReturnsBadRequest()
    {
        var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/Appointments/cancel")
        {
            Content = new StringContent("{", Encoding.UTF8, "application/json")
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task AssertError(HttpResponseMessage response, string expected)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadErrorAsync(response);
        Assert.Equal(expected, error.Error);
    }
}
