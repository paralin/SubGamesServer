using System;
using System.Threading;
using System.Threading.Tasks;
using log4net.Config;
using SubGames.Server.Twitch.Chat;
using SubGames.Server.Twitch.Chat.Controller;
using SubGames.Server.Twitch.Chat.Twitch;
using SubGames.Server.Twitch.Model;
using Xunit;

namespace SubGames.Server.Twitch.Test
{
    /// <summary>
    /// Tests the twitch client.
    /// </summary>
    public class TwitchTest : IDisposable
    {
        private ChatController bot;

        public TwitchTest()
        {
            BasicConfigurator.Configure();
            bot = new ChatController(new AuthInfo()
            {
                Username = "subgamesbot",
                Password = "oauth:6xmi7ksj3l0gl039l5tg68oyb7qoog"
            });
        }

        [Fact]
        public void A_TestConnection()
        {
            Task.Run(() => bot.Start());
            Thread.Sleep(Timeout.InfiniteTimeSpan);
        }

        public void Dispose()
        {
        }
    }
}
