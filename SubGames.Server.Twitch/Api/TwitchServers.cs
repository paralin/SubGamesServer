using System.Collections.Generic;
using Newtonsoft.Json;
using RestSharp;
using SubGames.Server.Twitch.Model;

namespace SubGames.Server.Twitch.Api
{
    /// <summary>
    /// Twitch server stores
    /// </summary>
    public static class TwitchServers
    {
        /// <summary>
        /// REST client
        /// </summary>
        private static RestClient Client { get; set; }

        /// <summary>
        /// Server IP addresses by cluster
        /// </summary>
        public static Dictionary<string, TwitchServerList>  ServerClusters { get; set; }

        static TwitchServers()
        {
            Client = new RestClient("http://tmi.twitch.tv");
            ServerClusters = new Dictionary<string, TwitchServerList>();
            RefreshServers("group");
        }

        /// <summary>
        /// Refresh server list for a particular cluster
        /// </summary>
        /// <param name="cluster"></param>
        public static void RefreshServers(string cluster)
        {
            var req = new RestRequest("/servers?cluster="+cluster);
            var resp = Client.Execute(req);
            ServerClusters[cluster] = JsonConvert.DeserializeObject<TwitchServerList>(resp.Content);
        }
    }
}
