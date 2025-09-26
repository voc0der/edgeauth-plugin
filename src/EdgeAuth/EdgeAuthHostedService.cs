// src/EdgeAuth/EdgeAuthHostedService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.EdgeAuth
{
    public class EdgeAuthHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<EdgeAuthHostedService> _logger;
        private readonly EdgeAuthServer _server;

        public EdgeAuthHostedService(
            ILogger<EdgeAuthHostedService> logger,
            EdgeAuthServer server)
        {
            _logger = logger;
            _server = server;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EdgeAuthHostedService starting…");

            // Start localhost Kestrel (127.0.0.1:5577)
            await _server.StartAsync(cancellationToken).ConfigureAwait(false);

            // Note: auto-revoke-on-session-start removed for now (API changed in 10.10).
            // We can re-add once we confirm the correct event/IP surface.
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EdgeAuthHostedService stopping…");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
