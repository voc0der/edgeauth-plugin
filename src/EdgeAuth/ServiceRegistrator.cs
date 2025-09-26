// src/EdgeAuth/ServiceRegistrator.cs
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.EdgeAuth
{
    // Note the fully-qualified interface name below:
    public sealed class ServiceRegistrator : MediaBrowser.Common.Plugins.IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection services)
        {
            // Core services for EdgeAuth
            services.AddSingleton<IEdgeAuthStore, MemoryEdgeAuthStore>();

            // Hosted service that starts the internal Kestrel listener
            services.AddHostedService<EdgeAuthHostedService>();
        }
    }
}
