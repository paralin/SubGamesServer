using RestSharp;
using SubGames.Server.Twitch.Model;

namespace SubGames.Server.Twitch.Api
{
    /// <summary>
    /// Twitch chat depot api
    /// </summary>
    public static class TwitchChatDepot
    {
        /// <summary>
        /// Rest client for chat depot
        /// </summary>
        private static RestClient Client { get; set; }

        static TwitchChatDepot()
        {
            Client = new RestClient("http://chatdepot.twitch.tv");
        }

        /// <summary>
        /// Get room memberships with a token
        /// </summary>
        /// <param name="token">User token</param>
        /// <returns>Response</returns>
        public static IRestResponse<TwitchRoomMemberships> GetRoomMemberships(string token)
        {
            if (token.StartsWith("oauth:"))
                token = token.Substring(6);
            var request = new RestRequest("/room_memberships");
            request.AddQueryParameter("oauth_token", token);
            return Client.Execute<TwitchRoomMemberships>(request);
        }
    }
}
