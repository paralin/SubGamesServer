using Newtonsoft.Json;

namespace SubGames.Server.Twitch.Model
{
    public class TwitchServerList
    {
        /// <summary>
        /// Cluster name by string, e.g. "group"
        /// </summary>
        [JsonProperty("cluster")]
        public string Cluster { get; set; }

        /// <summary>
        /// Servers list
        /// </summary>
        [JsonProperty("servers")]
        public string[] Servers { get; set; }

        /// <summary>
        /// Websocket based server list
        /// </summary>
        [JsonProperty("websockets_servers")]
        public string[] WebsocketServers { get; set; }
    }
}
