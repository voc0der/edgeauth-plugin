// src/EdgeAuth/ServiceRegistrator.cs
using MediaBrowser.Controller;               // IServerApplicationHost
using MediaBrowser.Controller.Plugins;       // IPluginServiceRegistrator
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.EdgeAuth
{
    /// <summary>
    /// Registers EdgeAuth services with Jellyfin's DI container on startup.
    /// </summary>
    public class ServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection services, IServerApplicationHost applicationHost)
        {
            // Core singletons used by the hosted service and minimal Kestrel server
            services.AddSingleton<IEdgeAuthStore, MemoryEdgeAuthStore>();
            services.AddSingleton<EdgeAuthServer>();

            // Background worker that starts/stops the internal listener and handles lifecycle
            services.AddHostedService<EdgeAuthHostedService>();
        }
    }
}
