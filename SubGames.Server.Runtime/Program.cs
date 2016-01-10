using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using SteamKit2;
using SubGames.Server.Channel;
using SubGames.Server.Model;
using SubGames.Server.Twitch.Model;

namespace SubGames.Server.Runtime
{
    public class Program
    {
        private static ILog log = LogManager.GetLogger("Program");
        public static void Main(string[] args)
        {
            BasicConfigurator.Configure();

            log.Info("Initializing...");

            // ChannelManager manager = new ChannelManager();
            ChannelInstance channel = new ChannelInstance(new ChannelConfig()
            {
                Channel = "quantumdota",
                DotaAuth = new SteamUser.LogOnDetails()
                {
                    Username = "webleaguetest",
                    Password = "OkayCupid"
                },
                TwitchAuth = new AuthInfo()
                {
                    Username = "subgamesbot_test",
                    Password = "oauth:sx36zdj5rpbqrof40m57j8z4pucbut"
                },
                OwnerSteamId = 76561198029304414
            });
            channel.Start();
            Thread.Sleep(Timeout.InfiniteTimeSpan);
        }
    }
}
