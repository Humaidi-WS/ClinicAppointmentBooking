using ClinicAppointmentBooking.Tests.Infrastructure;

namespace ClinicAppointmentBooking.Tests;

[Collection(ClinicApiCollection.Name)]
public sealed class DoctorsEndpointTests : ApiTestBase
{
    public DoctorsEndpointTests(ClinicApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetDoctors_ReturnsSeededDoctorsOrderedByName()
    {
        var doctors = await GetDoctorsAsync();

        Assert.Equal(
        [
            new DoctorResponse(doctors[0].Id, "Daniel", "Wilson", "Orthopedics"),
            new DoctorResponse(doctors[1].Id, "Emily", "Carter", "Pediatrics"),
            new DoctorResponse(doctors[2].Id, "James", "Anderson", "Cardiology"),
            new DoctorResponse(doctors[3].Id, "Michael", "Roberts", "Neurology"),
            new DoctorResponse(doctors[4].Id, "Sarah", "Mitchell", "Dermatology")
        ], doctors);
    }

    [Fact]
    public async Task GetDoctors_ReturnsJson()
    {
        var response = await Client.GetAsync("/api/Appointments/doctors");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetDoctors_AllowsAngularDevServerOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Appointments/doctors");
        request.Headers.Add("Origin", "http://localhost:4200");

        var response = await Client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http://localhost:4200", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task GetDoctors_DoesNotAllowUnknownOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Appointments/doctors");
        request.Headers.Add("Origin", "http://evil.example");

        var response = await Client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task BookPreflight_AllowsPostFromAngularDevServer()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/Appointments/book");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await Client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:4200", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("POST", response.Headers.GetValues("Access-Control-Allow-Methods").Single(), StringComparison.OrdinalIgnoreCase);
    }
}
