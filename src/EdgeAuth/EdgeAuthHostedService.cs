// src/EdgeAuth/EdgeAuthHostedService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.EdgeAuth
{
    /// <summary>
    /// Starts the localhost Kestrel listener (127.0.0.1:5577) for reverse-proxy callbacks.
    /// No session event wiring here (10.10 API surface changed).
    /// </summary>
    public sealed class EdgeAuthHostedService : IHostedService, IDisposable
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
            await _server.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EdgeAuthHostedService stopping…");
            // Nothing to stop explicitly; Kestrel is tied to Jellyfin lifetime.
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            // Nothing to dispose for now.
        }
    }
}
