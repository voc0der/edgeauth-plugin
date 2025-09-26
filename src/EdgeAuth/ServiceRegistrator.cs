// src/EdgeAuth/ServiceRegistrator.cs
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.EdgeAuth;

public sealed class ServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection services, IServerApplicationHost appHost)
    {
        services.AddSingleton<IEdgeAuthStore, MemoryEdgeAuthStore>();
        services.AddSingleton<EdgeAuthServer>();
        // (Optional) if you later switch to a HostedService to start the Kestrel listener:
        // services.AddHostedService<EdgeAuthHostedService>();
    }
}
