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
        public string AdminSecret { get; set; } = "";  // set at runtime
        public int DefaultTtlSeconds { get; set; } = 300;
        public int AdminHmacSkewSeconds { get; set; } = 60;
        public bool StrictLoginOnly { get; set; } = false;
    }
}
