using HyundaiTransys.VisionInspection.Application.Services;
using HyundaiTransys.VisionInspection.Core.Abstractions;
using HyundaiTransys.VisionInspection.Core.Configuration;
using HyundaiTransys.VisionInspection.Infrastructure.Configuration;
using HyundaiTransys.VisionInspection.Infrastructure.Keyence;
using HyundaiTransys.VisionInspection.Infrastructure.Mes;
using HyundaiTransys.VisionInspection.Infrastructure.Persistence;
using HyundaiTransys.VisionInspection.Infrastructure.Security;
using HyundaiTransys.VisionInspection.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HyundaiTransys.VisionInspection.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInspectionInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));

        services.AddSingleton<IMesMessageParser, MesMessageParser>();
        services.AddSingleton<IMesClient, MesTcpClient>();
        services.AddSingleton<IKeyenceClient, KeyenceTcpClient>();
        services.AddSingleton<IImageStorage, LocalImageStorage>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        services.AddSingleton<IConfigurationService>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<JsonConfigurationService>>();
            var path = configuration["ConfigFilePath"]
                ?? Path.Combine(AppContext.BaseDirectory, "secure.config");
            return new JsonConfigurationService(path, logger);
        });

        var dbOptions = configuration
            .GetSection($"{AppSettings.SectionName}:Database")
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        services.AddDbContext<InspectionDbContext>(opt =>
        {
            if (dbOptions.Provider == DatabaseProvider.PostgreSql)
                opt.UseNpgsql(dbOptions.ConnectionString);
            else
                opt.UseSqlite(dbOptions.ConnectionString);
        });

        services.AddScoped<IInspectionRepository, InspectionRepository>();
        services.AddScoped<IJobMappingRepository, JobMappingRepository>();
        services.AddScoped<IUserStore, UserRepository>();

        return services;
    }
}
