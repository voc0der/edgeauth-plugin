using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Plugins;   // IServerEntryPoint
using MediaBrowser.Controller.Session;   // ISessionManager
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.EdgeAuth
{
    public class EdgeAuthEntryPoint : IServerEntryPoint, IDisposable
    {
        private readonly ILogger<EdgeAuthEntryPoint> _logger;
        private readonly ISessionManager _sessions;
        private readonly IEdgeAuthStore _store;
        private readonly EdgeAuthServer _server;

        public EdgeAuthEntryPoint(
            ILogger<EdgeAuthEntryPoint> logger,
            ISessionManager sessions,
            IEdgeAuthStore store,
            EdgeAuthServer server)
        {
            _logger = logger;
            _sessions = sessions;
            _store = store;
            _server = server;
        }

        public async Task RunAsync()
        {
            _logger.LogInformation("EdgeAuth entrypoint starting…");
            // Start localhost Kestrel listener (127.0.0.1:5577)
            await _server.StartAsync(CancellationToken.None).ConfigureAwait(false);

            // Revoke temp IP allow when a real session starts
            _sessions.SessionStarted += OnSessionStarted;

            // Optional warning if admin secret not set (env or config)
            var env = Environment.GetEnvironmentVariable("EDGEAUTH_ADMIN_SECRET");
            if (string.IsNullOrWhiteSpace(env))
                _logger.LogWarning("EdgeAuth: EDGEAUTH_ADMIN_SECRET not set; /allow will 500 unless configured in plugin config.");
        }

        public Task StopAsync()
        {
            _logger.LogInformation("EdgeAuth entrypoint stopping…");
            _sessions.SessionStarted -= OnSessionStarted;
            return Task.CompletedTask;
        }

        private void OnSessionStarted(object? sender, SessionEventArgs e)
        {
            var ep = e.RemoteEndPoint ?? "";
            var ip = ep.Contains(':') ? ep.Split(':').FirstOrDefault() : ep;
            if (!string.IsNullOrWhiteSpace(ip))
            {
                _store.RevokeByIp(ip);
                _logger.LogDebug("EdgeAuth: revoked temp allow for {Ip} after session start.", ip);
            }
        }

        public void Dispose()
        {
            _sessions.SessionStarted -= OnSessionStarted;
        }
    }
}
