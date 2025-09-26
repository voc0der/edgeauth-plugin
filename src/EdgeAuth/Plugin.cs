using MediaBrowser.Common;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.EdgeAuth
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public override string Name => "EdgeAuth";
        public override string Description => "Reverse-proxy auth bridge (token + ephemeral IP allow).";

        public Plugin(IApplicationHost host) : base(host) { }
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
        // Leave blank — provide at runtime via env var (EDGEAUTH_ADMIN_SECRET) or persisted plugin config.
        public string AdminSecret { get; set; } = "";

        public int DefaultTtlSeconds { get; set; } = 300;
        public int AdminHmacSkewSeconds { get; set; } = 60;

        // Reserved for future use
        public bool StrictLoginOnly { get; set; } = false;
    }
}
