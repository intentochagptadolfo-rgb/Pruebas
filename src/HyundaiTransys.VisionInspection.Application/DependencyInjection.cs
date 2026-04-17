using HyundaiTransys.VisionInspection.Application.Services;
using HyundaiTransys.VisionInspection.Application.StateMachine;
using HyundaiTransys.VisionInspection.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace HyundaiTransys.VisionInspection.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddInspectionApplication(this IServiceCollection services)
    {
        services.AddSingleton<IJobResolver, JobResolver>();
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<IInspectionOrchestrator, InspectionOrchestrator>();
        return services;
    }
}
