using SteamKit2;
using SubGames.Server.Twitch.Model;

namespace SubGames.Server.Model
{
    /// <summary>
    /// Channel configuration
    /// </summary>
    public class ChannelConfig
    {
        /// <summary>
        /// DOTA authentication
        /// </summary>
        public SteamUser.LogOnDetails DotaAuth { get; set; }

        /// <summary>
        /// Twitch authentication
        /// </summary>
        public AuthInfo TwitchAuth { get; set; }

        /// <summary>
        /// Channel name
        /// </summary>
        public string Channel { get; set; }
    }
}
