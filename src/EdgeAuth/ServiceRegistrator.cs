using MediaBrowser.Common.Plugins;       // IPluginServiceRegistrator
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Plugin.EdgeAuth
{
    public class ServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection services)
        {
            // Core services for EdgeAuth
            services.AddSingleton<IEdgeAuthStore, MemoryEdgeAuthStore>();
            services.AddSingleton<EdgeAuthServer>();

            // Expose the current plugin configuration instance from Jellyfin
            // Note: Jellyfin will construct Plugin and set Configuration; this adds that instance to DI.
            services.AddSingleton(sp =>
            {
                // Resolve the Plugin itself from Jellyfin’s plugin system
                // (Jellyfin adds BasePlugin<T> to DI). If not available, we still want the app to boot.
                var plugin = sp.GetService(typeof(Plugin)) as Plugin;
                return (PluginConfiguration)(plugin?.Configuration ?? new PluginConfiguration());
            });

            // Background startup/cleanup logic
            services.AddHostedService<EdgeAuthHostedService>();
        }
    }
}
