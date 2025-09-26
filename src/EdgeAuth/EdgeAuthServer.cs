using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Session;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Plugin.EdgeAuth
{
    public class EdgeAuthServer
    {
        private readonly IServiceProvider _services;
        private IHost? _host;

        public EdgeAuthServer(IServiceProvider services)
        {
            _services = services;
        }

        public Task StartAsync(CancellationToken token)
        {
            if (_host != null) return Task.CompletedTask;

            _host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(opt =>
                    {
                        // Bind localhost only; keep firewall-closed too.
                        opt.Listen(IPAddress.Loopback, 5577);
                    });

                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            // ----- /validate -----
                            endpoints.MapGet("/validate", async ctx =>
                            {
                                var store    = (IEdgeAuthStore)_services.GetService(typeof(IEdgeAuthStore))!;
                                var sessions = (ISessionManager)_services.GetService(typeof(ISessionManager))!;

                                // Prefer proxy-provided IP; else the socket's RemoteIpAddress
                                var ip = ctx.Request.Headers["X-Forwarded-For"].ToString();
                                if (string.IsNullOrEmpty(ip))
                                    ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "";

                                var token = ExtractToken(ctx.Request);

                                var tokenOk = await TokenValidAsync(sessions, token).ConfigureAwait(false);
                                if (tokenOk || store.Exists(ip))
                                {
                                    ctx.Response.StatusCode = 200;
                                    ctx.Response.Headers["X-Auth-Method"] = tokenOk ? "token" : "ip-temp";
                                    await ctx.Response.CompleteAsync();
                                    return;
                                }

                                ctx.Response.StatusCode = 401;
                            });

                            // ----- /allow (HMAC protected) -----
                            endpoints.MapPost("/allow", async ctx =>
                            {
                                var config = (PluginConfiguration)_services.GetService(typeof(PluginConfiguration))!;
                                var secret = GetAdminSecret(_services); // env > config
                                if (string.IsNullOrWhiteSpace(secret))
                                {
                                    ctx.Response.StatusCode = 500; // misconfigured
                                    await ctx.Response.WriteAsync("{\"error\":\"admin secret not set\"}");
                                    return;
                                }

                                // Required headers
                                if (!ctx.Request.Headers.TryGetValue("X-Admin-Timestamp", out var tsStr) ||
                                    !ctx.Request.Headers.TryGetValue("X-Admin-Signature", out var sigHex))
                                {
                                    ctx.Response.StatusCode = 400; // missing headers
                                    return;
                                }

                                // Parse body (included in string-to-sign fields ip/ttl)
                                var req = await System.Text.Json.JsonSerializer.DeserializeAsync<AllowReq>(ctx.Request.Body)
                                          ?? new AllowReq();

                                // Freshness check
                                if (!long.TryParse(tsStr, out var tsUnix))
                                {
                                    ctx.Response.StatusCode = 400; return;
                                }
                                var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                var skew = Math.Abs(nowUnix - tsUnix);
                                if (skew > Math.Max(1, config.AdminHmacSkewSeconds))
                                {
                                    ctx.Response.StatusCode = 401; // stale/early
                                    return;
                                }

                                // Canonical string
                                var method = "POST";
                                var path   = "/allow";
                                var ipBody = req.Ip ?? "";
                                var ttlStr = req.TtlSeconds.ToString();
                                var toSign = $"{method}\n{path}\n{tsUnix}\n{ipBody}\n{ttlStr}";

                                // Verify HMAC
                                var expected = HmacHex(secret, toSign);
                                if (!ConstantTimeEquals(expected, sigHex.ToString().Trim()))
                                {
                                    ctx.Response.StatusCode = 403; // bad signature
                                    return;
                                }

                                // OK — apply
                                var store = (IEdgeAuthStore)_services.GetService(typeof(IEdgeAuthStore))!;
                                var ttl   = TimeSpan.FromSeconds(req.TtlSeconds > 0 ? req.TtlSeconds : config.DefaultTtlSeconds);
                                store.Add(ipBody, ttl);

                                ctx.Response.ContentType = "application/json";
                                await ctx.Response.WriteAsync("{\"ok\":true}");
                            });
                        });
                    });
                }).Build();

            return _host.StartAsync(token);
        }

        // Prefer env var over persisted config (so secret isn’t in source or config files)
        private static string GetAdminSecret(IServiceProvider services)
        {
            var env = Environment.GetEnvironmentVariable("EDGEAUTH_ADMIN_SECRET");
            if (!string.IsNullOrWhiteSpace(env)) return env;

            var cfg = (PluginConfiguration)services.GetService(typeof(PluginConfiguration))!;
            if (cfg != null && !string.IsNullOrWhiteSpace(cfg.AdminSecret))
                return cfg.AdminSecret;

            return string.Empty;
        }

        private static string ExtractToken(HttpRequest req)
        {
            if (req.Headers.TryGetValue("X-Emby-Token", out var t)) return t!;
            if (req.Headers.TryGetValue("X-MediaBrowser-Token", out t)) return t!;
            if (req.Headers.TryGetValue("Authorization", out var auth) &&
                auth.ToString().StartsWith("MediaBrowser", StringComparison.OrdinalIgnoreCase))
            {
                var s = auth.ToString();
                var i = s.IndexOf("Token=\"", StringComparison.OrdinalIgnoreCase);
                if (i >= 0)
                {
                    var j = s.IndexOf('"', i + 7);
                    if (j > i) return s.Substring(i + 7, j - (i + 7));
                }
            }
            return "";
        }

        private static async Task<bool> TokenValidAsync(MediaBrowser.Controller.Session.ISessionManager sessions, string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            try
            {
                var session = await sessions.GetSessionByAuthenticationToken(token, CancellationToken.None).ConfigureAwait(false);
                return session != null && session.UserId.HasValue;
            }
            catch
            {
                return false;
            }
        }

        private class AllowReq
        {
            public string? Ip { get; set; }
            public int TtlSeconds { get; set; } = 300;
        }

        // --- helpers ---
        private static string HmacHex(string secret, string data)
        {
            using var h = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
            var bytes = System.Text.Encoding.UTF8.GetBytes(data);
            var mac = h.ComputeHash(bytes);
            return BitConverter.ToString(mac).Replace("-", "").ToLowerInvariant();
        }

        private static bool ConstantTimeEquals(string a, string b)
        {
            if (a is null || b is null) return false;
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
