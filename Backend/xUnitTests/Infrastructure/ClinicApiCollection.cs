namespace ClinicAppointmentBooking.Tests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class ClinicApiCollection : ICollectionFixture<ClinicApiFactory>
{
    public const string Name = "ClinicApi";
}
