using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.EdgeAuth
{
    public class EdgeAuthHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<EdgeAuthHostedService> _logger;
        private readonly ISessionManager _sessions;
        private readonly IEdgeAuthStore _store;
        private readonly EdgeAuthServer _server;

        public EdgeAuthHostedService(
            ILogger<EdgeAuthHostedService> logger,
            ISessionManager sessions,
            IEdgeAuthStore store,
            EdgeAuthServer server)
        {
            _logger = logger;
            _sessions = sessions;
            _store = store;
            _server = server;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EdgeAuthHostedService starting…");

            // Start localhost Kestrel (127.0.0.1:5577)
            await _server.StartAsync(cancellationToken).ConfigureAwait(false);

            // Revoke temp IP allow when a real session starts
            _sessions.SessionStarted += OnSessionStarted;

            // Warn if admin secret not set
            var env = Environment.GetEnvironmentVariable("EDGEAUTH_ADMIN_SECRET");
            if (string.IsNullOrWhiteSpace(env))
                _logger.LogWarning("EdgeAuth: EDGEAUTH_ADMIN_SECRET not set; /allow will return 500 unless set in env or plugin config.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EdgeAuthHostedService stopping…");
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
