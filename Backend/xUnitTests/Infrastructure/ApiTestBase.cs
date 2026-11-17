using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicAppointmentBooking.API.DTO;
using ClinicAppointmentBooking.API.DTOs;

namespace ClinicAppointmentBooking.Tests.Infrastructure;

[Collection(ClinicApiCollection.Name)]
public abstract class ApiTestBase : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected ApiTestBase(ClinicApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        Clock = factory.Clock;
    }

    protected ClinicApiFactory Factory { get; }

    protected HttpClient Client { get; }

    protected FixedTimeProvider Clock { get; }

    public Task InitializeAsync() => Factory.ResetAsync();

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    protected void SetClock(int year, int month, int day, int hour, int minute, int second = 0)
        => Clock.SetLocal(new DateTime(year, month, day, hour, minute, second));

    protected async Task<IReadOnlyList<DoctorResponse>> GetDoctorsAsync()
    {
        var response = await Client.GetAsync("/api/Appointments/doctors");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doctors = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>(Json);
        Assert.NotNull(doctors);
        return doctors;
    }

    protected async Task<DoctorResponse> DoctorNamed(string firstName, string lastName)
    {
        var doctors = await GetDoctorsAsync();
        var doctor = doctors.SingleOrDefault(d => d.FirstName == firstName && d.LastName == lastName);
        Assert.NotNull(doctor);
        return doctor;
    }

    protected async Task<HttpResponseMessage> PostBookingAsync(
        int doctorId,
        DateTime startTime,
        string firstName = "Ada",
        string lastName = "Lovelace",
        string phone = "5550100",
        DateOnly? dateOfBirth = null)
    {
        return await Client.PostAsJsonAsync("/api/Appointments/book", new BookAppointmentRequest(
            doctorId,
            startTime,
            firstName,
            lastName,
            phone,
            dateOfBirth ?? new DateOnly(1990, 4, 2)), Json);
    }

    protected async Task<BookAppointmentResponse> BookAsync(
        int doctorId,
        DateTime startTime,
        string firstName = "Ada",
        string lastName = "Lovelace",
        string phone = "5550100",
        DateOnly? dateOfBirth = null)
    {
        var response = await PostBookingAsync(doctorId, startTime, firstName, lastName, phone, dateOfBirth);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BookAppointmentResponse>(Json);
        Assert.NotNull(body);
        return body;
    }

    protected async Task<HttpResponseMessage> CancelAsync(int appointmentId, int patientId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/Appointments/cancel")
        {
            Content = JsonContent.Create(new CancelAppointmentRequest(appointmentId, patientId), options: Json)
        };
        return await Client.SendAsync(request);
    }

    protected static async Task<ErrorResponse> ReadErrorAsync(HttpResponseMessage response)
    {
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(Json);
        Assert.NotNull(error);
        return error;
    }

    protected static DateTime At(int year, int month, int day, int hour, int minute, int second = 0, int millisecond = 0)
        => new(year, month, day, hour, minute, second, millisecond, DateTimeKind.Unspecified);
}

public sealed record DoctorResponse(int Id, string FirstName, string LastName, string Specialty);
