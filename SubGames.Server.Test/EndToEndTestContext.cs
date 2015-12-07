using System;
using SteamKit2;
using SubGames.Server.Channel;
using SubGames.Server.Model;
using SubGames.Server.Twitch.Model;

namespace SubGames.Server.Test
{
    public class EndToEndTestContext : IDisposable
    {
        public ChannelInstance Instance { get; set; }

        public EndToEndTestContext()
        {
            Instance = new ChannelInstance(new ChannelConfig()
            {
                Channel = "quantumdota",
                DotaAuth = new SteamUser.LogOnDetails()
                {
                    Username = "webleaguetest",
                    Password = "OkayCupid"
                },
                TwitchAuth = new AuthInfo()
                {
                    Username = "subgamesbot",
                    Password = "oauth:6xmi7ksj3l0gl039l5tg68oyb7qoog"
                }
            });
        }

        public void Dispose()
        {
            Instance.Stop();
            Instance = null;
        }
    }
}
