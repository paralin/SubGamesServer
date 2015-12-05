using System.Threading;
using log4net.Config;
using SubGames.Server.Twitch.Chat;
using SubGames.Server.Twitch.Model;
using SubGames.Server.Twitch.Test;

namespace SubGames.Server.Twitch.TestBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            TwitchTest test = new TwitchTest();
            test.A_TestConnection();
        }
    }
}
