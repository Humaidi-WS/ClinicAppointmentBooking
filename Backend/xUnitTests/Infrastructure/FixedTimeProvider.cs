namespace ClinicAppointmentBooking.Tests.Infrastructure;

/// <summary>
/// Clock fixed to a local date and time. Local and UTC views stay consistent with the machine time zone,
/// which is what <see cref="TimeProvider.System"/> does in the running API.
/// </summary>
public sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTime localTime) => SetLocal(localTime);

    public void SetLocal(DateTime localTime)
    {
        var unspecified = DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified);
        _utcNow = new DateTimeOffset(unspecified, TimeZoneInfo.Local.GetUtcOffset(unspecified)).ToUniversalTime();
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;
}
