using System;
using System.Collections.Concurrent;

namespace Jellyfin.Plugin.EdgeAuth
{
    public interface IEdgeAuthStore
    {
        void Add(string ip, TimeSpan ttl);
        bool Exists(string ip);
        void RevokeByIp(string ip);
    }

    public class MemoryEdgeAuthStore : IEdgeAuthStore
    {
        private readonly ConcurrentDictionary<string, DateTimeOffset> _exp = new();

        public void Add(string ip, TimeSpan ttl)
        {
            if (string.IsNullOrWhiteSpace(ip)) return;
            _exp[ip] = DateTimeOffset.UtcNow.Add(ttl);
        }

        public bool Exists(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return false;
            if (_exp.TryGetValue(ip, out var until))
            {
                if (until > DateTimeOffset.UtcNow) return true;
                _exp.TryRemove(ip, out _);
            }
            return false;
        }

        public void RevokeByIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return;
            _exp.TryRemove(ip, out _);
        }
    }
}