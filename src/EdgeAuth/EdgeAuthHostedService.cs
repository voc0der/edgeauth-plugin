// src/EdgeAuth/EdgeAuthHostedService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Session;      // ISessionManager, SessionEventArgs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.EdgeAuth
{
    public class EdgeAuthHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<EdgeAuthHostedService> _logger;
        private readonly EdgeAuthServer _server;
        private readonly ISessionManager _sessions;
        private readonly IEdgeAuthStore _store;

        public EdgeAuthHostedService(
            ILogger<EdgeAuthHostedService> logger,
            EdgeAuthServer server,
            ISessionManager sessions,
            IEdgeAuthStore store)
        {
            _logger = logger;
            _server = server;
            _sessions = sessions;
            _store = store;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EdgeAuthHostedService starting…");
            await _server.StartAsync(cancellationToken).ConfigureAwait(false);

            // Revoke temp IP allow when a real session starts (10.10+: IP available via e.Session.RemoteEndPoint)
            _sessions.SessionStarted += OnSessionStarted;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EdgeAuthHostedService stopping…");
            _sessions.SessionStarted -= OnSessionStarted;
            return Task.CompletedTask;
        }

        private void OnSessionStarted(object? sender, SessionEventArgs e)
        {
            try
            {
                var ep = e.Session?.RemoteEndPoint ?? string.Empty;
                var ip = ExtractIp(ep);
                if (!string.IsNullOrWhiteSpace(ip))
                {
                    _store.RevokeByIp(ip);
                    _logger.LogDebug("EdgeAuth: revoked temp allow for {Ip} after session start.", ip);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "EdgeAuth: failed to revoke temp allow on session start.");
            }
        }

        // Handles IPv4 "a.b.c.d:port", IPv6 "[::1]:port", bare IPv6 "::1", and plain IPs
        private static string ExtractIp(string remoteEndPoint)
        {
            if (string.IsNullOrWhiteSpace(remoteEndPoint)) return string.Empty;

            // If it's bracketed IPv6 like "[2001:db8::1]:1234"
            if (remoteEndPoint.StartsWith("[") && remoteEndPoint.Contains("]"))
            {
                var end = remoteEndPoint.IndexOf(']');
                return end > 1 ? remoteEndPoint.Substring(1, end - 1) : string.Empty;
            }

            // If it contains exactly one ':' it's probably IPv4:port
            var firstColon = remoteEndPoint.IndexOf(':');
            var lastColon = remoteEndPoint.LastIndexOf(':');

            if (firstColon >= 0 && firstColon == lastColon)
            {
                // "ip:port" -> take part before colon
                return remoteEndPoint.Substring(0, firstColon);
            }

            // Otherwise treat as a bare IP (IPv6 without port or already just an IP)
            return remoteEndPoint;
        }

        public void Dispose()
        {
            _sessions.SessionStarted -= OnSessionStarted;
        }
    }
}
