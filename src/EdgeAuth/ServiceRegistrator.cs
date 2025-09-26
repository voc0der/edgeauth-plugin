using MediaBrowser.Common.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Plugin.EdgeAuth
{
    public class ServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection services)
        {
            // Core services
            services.AddSingleton<IEdgeAuthStore, MemoryEdgeAuthStore>();
            services.AddSingleton<EdgeAuthServer>();

            // Expose the current plugin configuration instance from Jellyfin
            services.AddSingleton(sp =>
            {
                var plugin = sp.GetService(typeof(Plugin)) as Plugin;
                return (PluginConfiguration)(plugin?.Configuration ?? new PluginConfiguration());
            });

            // Background startup/cleanup logic
            services.AddHostedService<EdgeAuthHostedService>();
        }
    }
}
