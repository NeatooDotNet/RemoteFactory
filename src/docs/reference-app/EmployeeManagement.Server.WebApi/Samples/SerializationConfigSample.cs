using EmployeeManagement.Domain.Samples.GettingStarted;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples;

public static class SerializationConfig
{
    #region getting-started-serialization-config
    // Ordinal (default): Compact arrays ["Jane","Smith"] - 40-50% smaller
    public static NeatooSerializationOptions Ordinal =>
        new() { Format = SerializationFormat.Ordinal };

    // Named: Property names {"FirstName":"Jane"} - easier to debug
    public static NeatooSerializationOptions Named =>
        new() { Format = SerializationFormat.Named };
    #endregion

    public static void ConfigureOrdinal(IServiceCollection services)
    {
        services.AddNeatooAspNetCore(Ordinal, typeof(EmployeeModel).Assembly);
    }

    public static void ConfigureNamed(IServiceCollection services)
    {
        services.AddNeatooAspNetCore(Named, typeof(EmployeeModel).Assembly);
    }
}
