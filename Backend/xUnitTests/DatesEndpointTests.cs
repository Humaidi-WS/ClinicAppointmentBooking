using System.Net;
using System.Net.Http.Json;
using ClinicAppointmentBooking.Tests.Infrastructure;

namespace ClinicAppointmentBooking.Tests;

[Collection(ClinicApiCollection.Name)]
public sealed class DatesEndpointTests : ApiTestBase
{
    public DatesEndpointTests(ClinicApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetDates_BeforeClosing_StartsTodayAndRunsOneMonth()
    {
        SetClock(2026, 11, 17, 15, 59);

        var dates = await GetDatesAsync();

        AssertSchedule(dates, new DateOnly(2026, 11, 17), new DateOnly(2026, 12, 17), 23);
    }

    [Fact]
    public async Task GetDates_AtClosing_StartsTomorrow()
    {
        SetClock(2026, 11, 17, 16, 0);

        var dates = await GetDatesAsync();

        AssertSchedule(dates, new DateOnly(2026, 11, 18), new DateOnly(2026, 12, 17), 22);
        Assert.DoesNotContain(new DateOnly(2026, 11, 17), dates);
    }

    [Fact]
    public async Task GetDates_AfterClosing_StartsTomorrow()
    {
        SetClock(2026, 11, 17, 18, 30);

        var dates = await GetDatesAsync();

        Assert.Equal(new DateOnly(2026, 11, 18), dates[0]);
        Assert.DoesNotContain(new DateOnly(2026, 11, 17), dates);
    }

    [Fact]
    public async Task GetDates_OnFriday_SkipsFridayAndSaturday()
    {
        SetClock(2026, 11, 20, 10, 0);

        var dates = await GetDatesAsync();

        Assert.Equal(new DateOnly(2026, 11, 22), dates[0]);
        Assert.Equal(new DateOnly(2026, 12, 20), dates[^1]);
        Assert.Equal(21, dates.Count);
        Assert.DoesNotContain(dates, date => date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday);
    }

    [Fact]
    public async Task GetDates_OnSaturday_FirstOpenDayIsSunday()
    {
        SetClock(2026, 11, 21, 11, 0);

        var dates = await GetDatesAsync();

        Assert.Equal(new DateOnly(2026, 11, 22), dates[0]);
        Assert.DoesNotContain(new DateOnly(2026, 11, 21), dates);
    }

    private async Task<List<DateOnly>> GetDatesAsync()
    {
        var response = await Client.GetAsync("/api/Appointments/dates");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var dates = await response.Content.ReadFromJsonAsync<List<DateOnly>>(Json);
        Assert.NotNull(dates);
        return dates;
    }

    private static void AssertSchedule(IReadOnlyList<DateOnly> dates, DateOnly first, DateOnly last, int count)
    {
        Assert.Equal(first, dates[0]);
        Assert.Equal(last, dates[^1]);
        Assert.Equal(count, dates.Count);
        Assert.Equal(dates.OrderBy(date => date).ToArray(), dates);
        Assert.DoesNotContain(dates, date => date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday);

        for (var date = first; date <= last; date = date.AddDays(1))
        {
            var open = date.DayOfWeek is not DayOfWeek.Friday and not DayOfWeek.Saturday;
            Assert.Equal(open, dates.Contains(date));
        }
    }
}
