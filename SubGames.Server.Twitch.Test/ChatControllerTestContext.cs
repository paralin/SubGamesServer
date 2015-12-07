using System;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using SubGames.Server.Twitch.Model;

namespace SubGames.Server.Twitch.Test
{
    public class ChatControllerTestContext : IDisposable
    {
        public Chat.Controller.ChatController Master { get; set; }
        public Chat.Controller.ChatController Slave { get; set; }

        public ChatControllerTestContext()
        {
            Master = new Chat.Controller.ChatController(new AuthInfo()
            {
                Username = "subgamesbot",
                Password = "oauth:6xmi7ksj3l0gl039l5tg68oyb7qoog"
            });

            Slave = new Chat.Controller.ChatController(new AuthInfo()
            {
                Username = "subgamesbot_test",
                Password = "oauth:sx36zdj5rpbqrof40m57j8z4pucbut"
            });
        }

        public void Dispose()
        {
            Master.Stop();
            Master = null;
            Slave.Stop();
            Slave = null;
        }
    }
}
