using System.Net;
using System.Net.Http.Json;
using System.Text;
using ClinicAppointmentBooking.API.DTOs;
using ClinicAppointmentBooking.Tests.Infrastructure;

namespace ClinicAppointmentBooking.Tests;

[Collection(ClinicApiCollection.Name)]
public sealed class BookingEndpointTests : ApiTestBase
{
    public BookingEndpointTests(ClinicApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Book_ValidSlot_ReturnsCreatedAppointmentAndPatient()
    {
        var doctor = await DoctorNamed("James", "Anderson");
        var start = At(2026, 11, 18, 9, 30);

        var response = await PostBookingAsync(doctor.Id, start, "Ada", "Lovelace", "5550100", new DateOnly(1990, 4, 2));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var booked = await response.Content.ReadFromJsonAsync<BookAppointmentResponse>(Json);
        Assert.NotNull(booked);
        Assert.True(booked.AppointmentId > 0);
        Assert.True(booked.PatientId > 0);
        Assert.Equal(
            $"Appointment booked successfully. Your Patient ID is {booked.PatientId} — save it to view your appointments.",
            booked.Message);
        var location = response.Headers.Location;
        Assert.NotNull(location);
        var locationPath = location.IsAbsoluteUri ? location.PathAndQuery : location.OriginalString;
        Assert.EndsWith($"/api/Appointments/patients/{booked.PatientId}/appointments", locationPath);

        var followUp = await Client.GetAsync(locationPath);
        Assert.Equal(HttpStatusCode.OK, followUp.StatusCode);
        var appointments = await followUp.Content.ReadFromJsonAsync<List<PatientAppointmentDto>>(Json);
        Assert.NotNull(appointments);
        var appointment = Assert.Single(appointments);
        Assert.Equal(booked.AppointmentId, appointment.AppointmentId);
        Assert.Equal(doctor.Id, appointment.DoctorId);
        Assert.Equal("James Anderson", appointment.DoctorName);
        Assert.Equal("Cardiology", appointment.DoctorSpecialty);
        Assert.Equal(start, appointment.StartTime);
        Assert.Equal("Upcoming", appointment.AppointmentStatus);
    }

    [Theory]
    [InlineData(8, 0)]
    [InlineData(15, 30)]
    public async Task Book_OpeningAndLastSlot_Succeed(int hour, int minute)
    {
        SetClock(2026, 11, 18, 7, 0);
        var doctor = await DoctorNamed("Emily", "Carter");

        var booked = await BookAsync(doctor.Id, At(2026, 11, 18, hour, minute));

        Assert.True(booked.AppointmentId > 0);
    }

    [Fact]
    public async Task Book_SundaySlot_Succeeds()
    {
        var doctor = await DoctorNamed("Daniel", "Wilson");

        var booked = await BookAsync(doctor.Id, At(2026, 11, 22, 13, 0));

        Assert.True(booked.AppointmentId > 0);
    }

    [Fact]
    public async Task Book_SameTimeForTwoDoctors_BothSucceed()
    {
        var james = await DoctorNamed("James", "Anderson");
        var sarah = await DoctorNamed("Sarah", "Mitchell");
        var start = At(2026, 11, 19, 14, 0);

        var first = await BookAsync(james.Id, start, "Grace", "Hopper");
        var second = await BookAsync(sarah.Id, start, "Katherine", "Johnson");

        Assert.NotEqual(first.AppointmentId, second.AppointmentId);
        Assert.NotEqual(first.PatientId, second.PatientId);
    }

    [Fact]
    public async Task Book_DateBeyondSelectableWindow_SucceedsWhenOtherRulesPass()
    {
        var doctor = await DoctorNamed("Michael", "Roberts");

        var booked = await BookAsync(doctor.Id, At(2027, 1, 7, 10, 0));

        Assert.True(booked.AppointmentId > 0);
    }

    [Fact]
    public async Task Book_EqualToCurrentTime_ReturnsPastError()
    {
        SetClock(2026, 11, 18, 10, 0);
        var doctor = await DoctorNamed("James", "Anderson");

        var response = await PostBookingAsync(doctor.Id, At(2026, 11, 18, 10, 0));

        await AssertError(response, "Cannot book an appointment in the past.");
    }

    [Fact]
    public async Task Book_EarlierToday_ReturnsPastError()
    {
        var doctor = await DoctorNamed("James", "Anderson");

        var response = await PostBookingAsync(doctor.Id, At(2026, 11, 17, 8, 30));

        await AssertError(response, "Cannot book an appointment in the past.");
    }

    [Theory]
    [InlineData(2026, 11, 20)]
    [InlineData(2026, 11, 21)]
    public async Task Book_ClosedDays_ReturnsClosedError(int year, int month, int day)
    {
        var doctor = await DoctorNamed("Sarah", "Mitchell");

        var response = await PostBookingAsync(doctor.Id, At(year, month, day, 10, 0));

        await AssertError(response, "The clinic is closed on Fridays and Saturdays.");
    }

    [Theory]
    [InlineData(7, 30)]
    [InlineData(16, 0)]
    [InlineData(18, 0)]
    public async Task Book_OutsideWorkingHours_ReturnsHoursError(int hour, int minute)
    {
        SetClock(2026, 11, 18, 7, 0);
        var doctor = await DoctorNamed("Emily", "Carter");

        var response = await PostBookingAsync(doctor.Id, At(2026, 11, 18, hour, minute));

        await AssertError(response, "Time is outside working hours (08:00 to 16:00).");
    }

    [Fact]
    public async Task Book_UnalignedMinute_ReturnsSlotError()
    {
        var doctor = await DoctorNamed("Daniel", "Wilson");

        var response = await PostBookingAsync(doctor.Id, At(2026, 11, 18, 9, 15));

        await AssertError(response, "Appointments must be aligned to exact 30-minute slots.");
    }

    [Fact]
    public async Task Book_NonZeroSeconds_ReturnsSlotError()
    {
        var doctor = await DoctorNamed("Daniel", "Wilson");

        var response = await PostBookingAsync(doctor.Id, At(2026, 11, 18, 9, 0, 1));

        await AssertError(response, "Appointments must be aligned to exact 30-minute slots.");
    }

    [Fact]
    public async Task Book_NonZeroMilliseconds_ReturnsSlotError()
    {
        var doctor = await DoctorNamed("Daniel", "Wilson");

        var response = await PostBookingAsync(doctor.Id, At(2026, 11, 18, 9, 0, 0, 1));

        await AssertError(response, "Appointments must be aligned to exact 30-minute slots.");
    }

    [Fact]
    public async Task Book_UnknownDoctor_ReturnsNotFoundError()
    {
        var response = await PostBookingAsync(9999, At(2026, 11, 18, 9, 0));

        await AssertError(response, "Doctor not found.");
    }

    [Fact]
    public async Task Book_TakenSlot_ReturnsConflictErrorAndKeepsOriginalBooking()
    {
        var doctor = await DoctorNamed("James", "Anderson");
        var start = At(2026, 11, 18, 12, 0);
        var first = await BookAsync(doctor.Id, start, "Ada", "Lovelace");

        var response = await PostBookingAsync(doctor.Id, start, "Grace", "Hopper");

        await AssertError(response, "This time slot was just booked by someone else.");

        var lookup = await Client.GetAsync($"/api/Appointments/patients/{first.PatientId}/appointments");
        var appointments = await lookup.Content.ReadFromJsonAsync<List<PatientAppointmentDto>>(Json);
        Assert.NotNull(appointments);
        var appointment = Assert.Single(appointments);
        Assert.Equal("Upcoming", appointment.AppointmentStatus);
    }

    [Fact]
    public async Task Book_AfterCancellation_SameSlotCanBeBookedAgain()
    {
        var doctor = await DoctorNamed("Sarah", "Mitchell");
        var start = At(2026, 11, 19, 15, 30);
        var first = await BookAsync(doctor.Id, start);
        var cancel = await CancelAsync(first.AppointmentId, first.PatientId);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        var second = await BookAsync(doctor.Id, start, "Grace", "Hopper");

        Assert.NotEqual(first.AppointmentId, second.AppointmentId);
    }

    [Fact]
    public async Task Book_EmptyBody_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(
            "/api/Appointments/book",
            new StringContent("", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Book_MalformedJson_ReturnsBadRequest()
    {
        var response = await Client.PostAsync(
            "/api/Appointments/book",
            new StringContent("{", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Book_MissingPatientName_ReturnsBadRequest()
    {
        var doctor = await DoctorNamed("James", "Anderson");
        var payload = $$"""
            {
              "doctorId": {{doctor.Id}},
              "startTime": "2026-11-18T09:00:00",
              "patientLastName": "Lovelace",
              "patientPhoneNumber": "5550100",
              "patientDateOfBirth": "1990-04-02"
            }
            """;

        var response = await Client.PostAsync(
            "/api/Appointments/book",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task AssertError(HttpResponseMessage response, string expected)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadErrorAsync(response);
        Assert.Equal(expected, error.Error);
    }
}
