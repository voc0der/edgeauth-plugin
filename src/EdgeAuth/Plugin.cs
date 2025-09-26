using System;
using MediaBrowser.Common;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;   // IHasServiceRegistrations, IServerEntryPoint lives via Controller package
using MediaBrowser.Model.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.EdgeAuth
{
    // Implement IHasServiceRegistrations instead of overriding methods on BasePlugin<>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasServiceRegistrations
    {
        public override string Name => "EdgeAuth";
        public override string Description => "Reverse-proxy auth bridge (token + ephemeral IP allow).";

        public Plugin(IApplicationHost host) : base(host) { }

        // NOT an override in 10.10.x — this is discovered via the interface
        public void RegisterServices(IServiceCollection services)
        {
            // Core services
            services.AddSingleton<IEdgeAuthStore, MemoryEdgeAuthStore>();
            services.AddSingleton<EdgeAuthServer>();

            // Expose current configuration instance
            services.AddSingleton((PluginConfiguration)Configuration);

            // Register our entry point (Jellyfin will call RunAsync/StopAsync)
            services.AddSingleton<IServerEntryPoint, EdgeAuthEntryPoint>();
        }
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
        // Leave blank — provide via env var or persisted plugin config on the server
        public string AdminSecret { get; set; } = "";

        public int DefaultTtlSeconds { get; set; } = 300;
        public int AdminHmacSkewSeconds { get; set; } = 60;

        // Reserved for future use if you want to restrict the IP-allow flow to specific login URIs
        public bool StrictLoginOnly { get; set; } = false;
    }
}
