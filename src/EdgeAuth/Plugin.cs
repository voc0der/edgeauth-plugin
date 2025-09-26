using System;
using System.Linq;
using System.Threading;
using MediaBrowser.Common;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.EdgeAuth
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public override string Name => "EdgeAuth";
        public override string Description => "Reverse-proxy auth bridge (token + ephemeral IP allow).";

        public Plugin(IApplicationHost host) : base(host) { }

        public override void RegisterServices(IServiceCollection services)
        {
            // Core services for EdgeAuth
            services.AddSingleton<IEdgeAuthStore, MemoryEdgeAuthStore>();
            services.AddSingleton<EdgeAuthServer>();

            // Expose the current plugin configuration instance to DI so other services can resolve it directly.
            services.AddSingleton((PluginConfiguration)Configuration);
        }

        public override void OnApplicationStartup(IServiceProvider services)
        {
            // Start internal minimal Kestrel listener on localhost:5577 (POC)
            var server = services.GetRequiredService<EdgeAuthServer>();
            _ = server.StartAsync(CancellationToken.None);

            // Revoke IP temp allow upon new session as a convenience
            var sessions = services.GetRequiredService<ISessionManager>();
            var store = services.GetRequiredService<IEdgeAuthStore>();
            sessions.SessionStarted += (sender, e) =>
            {
                var ep = e.RemoteEndPoint ?? "";
                var ip = ep.Contains(':') ? ep.Split(':').FirstOrDefault() : ep;
                if (!string.IsNullOrWhiteSpace(ip))
                    store.RevokeByIp(ip);
            };
        }
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
        public string AdminSecret { get; set; } = "";
        public int DefaultTtlSeconds { get; set; } = 300;
        public int AdminHmacSkewSeconds { get; set; } = 60;
        public bool StrictLoginOnly { get; set; } = false;
    }
}
