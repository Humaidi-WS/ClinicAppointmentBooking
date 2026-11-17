using System.Net;
using System.Net.Http.Json;
using ClinicAppointmentBooking.API.DTOs;
using ClinicAppointmentBooking.Tests.Infrastructure;

namespace ClinicAppointmentBooking.Tests;

[Collection(ClinicApiCollection.Name)]
public sealed class ScheduleEndpointTests : ApiTestBase
{
    private static readonly string[] SlotTimes =
    [
        "08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00", "11:30",
        "12:00", "12:30", "13:00", "13:30", "14:00", "14:30", "15:00", "15:30"
    ];

    public ScheduleEndpointTests(ClinicApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetSchedule_FutureWeekday_ReturnsSixteenOpenSlots()
    {
        var doctor = await DoctorNamed("James", "Anderson");

        var slots = await GetScheduleAsync(doctor.Id, new DateOnly(2026, 11, 22));

        Assert.Equal(SlotTimes, slots.Select(slot => slot.Time).ToArray());
        Assert.All(slots, slot => Assert.True(slot.IsAvailable));
    }

    [Fact]
    public async Task GetSchedule_TodayBeforeOpening_AllSlotsAreAvailable()
    {
        SetClock(2026, 11, 17, 7, 59);
        var doctor = await DoctorNamed("Emily", "Carter");

        var slots = await GetScheduleAsync(doctor.Id, new DateOnly(2026, 11, 17));

        Assert.All(slots, slot => Assert.True(slot.IsAvailable));
    }

    [Fact]
    public async Task GetSchedule_TodayAtOpening_OpeningSlotIsClosed()
    {
        SetClock(2026, 11, 17, 8, 0);
        var doctor = await DoctorNamed("Emily", "Carter");

        var slots = await GetScheduleAsync(doctor.Id, new DateOnly(2026, 11, 17));

        Assert.False(slots.Single(slot => slot.Time == "08:00").IsAvailable);
        Assert.True(slots.Single(slot => slot.Time == "08:30").IsAvailable);
    }

    [Fact]
    public async Task GetSchedule_TodayDuringClinic_PastSlotsAreClosed()
    {
        SetClock(2026, 11, 17, 9, 0);
        var doctor = await DoctorNamed("Sarah", "Mitchell");

        var slots = await GetScheduleAsync(doctor.Id, new DateOnly(2026, 11, 17));

        Assert.All(slots.Where(slot => string.CompareOrdinal(slot.Time, "09:00") <= 0), slot => Assert.False(slot.IsAvailable));
        Assert.All(slots.Where(slot => string.CompareOrdinal(slot.Time, "09:30") >= 0), slot => Assert.True(slot.IsAvailable));
    }

    [Fact]
    public async Task GetSchedule_PastDate_ReturnsNoSlots()
    {
        var doctor = await DoctorNamed("Daniel", "Wilson");

        var slots = await GetScheduleAsync(doctor.Id, new DateOnly(2026, 11, 16));

        Assert.Empty(slots);
    }

    [Theory]
    [InlineData(2026, 11, 20)]
    [InlineData(2026, 11, 21)]
    public async Task GetSchedule_ClosedDays_ReturnsNoSlots(int year, int month, int day)
    {
        var doctor = await DoctorNamed("Michael", "Roberts");

        var slots = await GetScheduleAsync(doctor.Id, new DateOnly(year, month, day));

        Assert.Empty(slots);
    }

    [Fact]
    public async Task GetSchedule_UnknownDoctor_StillReturnsOpenSlots()
    {
        var slots = await GetScheduleAsync(doctorId: 9999, new DateOnly(2026, 11, 18));

        Assert.Equal(16, slots.Count);
        Assert.All(slots, slot => Assert.True(slot.IsAvailable));
    }

    [Fact]
    public async Task GetSchedule_BookedSlotIsUnavailableUntilCancelled()
    {
        var doctor = await DoctorNamed("James", "Anderson");
        var booked = await BookAsync(doctor.Id, At(2026, 11, 18, 10, 0));

        var beforeCancel = await GetScheduleAsync(doctor.Id, new DateOnly(2026, 11, 18));
        Assert.False(beforeCancel.Single(slot => slot.Time == "10:00").IsAvailable);
        Assert.True(beforeCancel.Single(slot => slot.Time == "10:30").IsAvailable);

        var cancel = await CancelAsync(booked.AppointmentId, booked.PatientId);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        var afterCancel = await GetScheduleAsync(doctor.Id, new DateOnly(2026, 11, 18));
        Assert.True(afterCancel.Single(slot => slot.Time == "10:00").IsAvailable);
    }

    [Fact]
    public async Task GetSchedule_BookingForAnotherDoctor_DoesNotBlockThisDoctor()
    {
        var james = await DoctorNamed("James", "Anderson");
        var sarah = await DoctorNamed("Sarah", "Mitchell");
        await BookAsync(james.Id, At(2026, 11, 18, 11, 0));

        var slots = await GetScheduleAsync(sarah.Id, new DateOnly(2026, 11, 18));

        Assert.True(slots.Single(slot => slot.Time == "11:00").IsAvailable);
    }

    [Fact]
    public async Task GetSchedule_MissingDate_ReturnsNoSlots()
    {
        var doctor = await DoctorNamed("James", "Anderson");
        var response = await Client.GetAsync($"/api/Appointments/doctors/{doctor.Id}/schedule");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var slots = await response.Content.ReadFromJsonAsync<List<TimeSlotDto>>(Json);
        Assert.NotNull(slots);
        Assert.Empty(slots);
    }

    [Fact]
    public async Task GetSchedule_InvalidDate_ReturnsBadRequest()
    {
        var doctor = await DoctorNamed("James", "Anderson");

        var response = await Client.GetAsync($"/api/Appointments/doctors/{doctor.Id}/schedule?date=not-a-date");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<List<TimeSlotDto>> GetScheduleAsync(int doctorId, DateOnly date)
    {
        var response = await Client.GetAsync($"/api/Appointments/doctors/{doctorId}/schedule?date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var slots = await response.Content.ReadFromJsonAsync<List<TimeSlotDto>>(Json);
        Assert.NotNull(slots);
        return slots;
    }
}
