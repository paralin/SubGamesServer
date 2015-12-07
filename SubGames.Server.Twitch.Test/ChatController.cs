using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using IrcDotNet;
using SubGames.Server.Twitch.Chat.Controller.Enums;
using SubGames.Server.Twitch.Test.Utils;
using Xunit;
using Xunit.Abstractions;

namespace SubGames.Server.Twitch.Test
{
    /// <summary>
    /// Tests the twitch client.
    /// </summary>
    [TestCaseOrderer("SubGames.Server.Twitch.Test.Utils.PriorityOrderer", "SubGames.Server.Twitch.Test")]
    [Collection("Chat controller")]
    public class ChatController : LogOutputTester, IClassFixture<ChatControllerTestContext>
    {
        public ChatControllerTestContext Context { get; set; }
        private ITestOutputHelper output;

        public ChatController(ChatControllerTestContext ctx, ITestOutputHelper helper) : base(helper)
        {
            this.Context = ctx;
            this.output = helper;
        }

        /// <summary>
        /// The bots should connect to the server.
        /// </summary>
        [Fact(DisplayName = "Connect"), TestPriority(-1)]
        public void A_BotsShouldConnect()
        {
            Stopwatch startedTime = new Stopwatch();
            startedTime.Start();
            Task.Run(() => Context.Master.Start());
            Task.Run(() => Context.Slave.Start());
            while (startedTime.Elapsed < TimeSpan.FromSeconds(30)
                && (Context.Master.State != State.Ready
                || Context.Slave.State != State.Ready))
            {
                Thread.Sleep(500);
            }
            Assert.Equal(Context.Master.State, State.Ready);
            Assert.Equal(Context.Slave.State, State.Ready);
        }

        /// <summary>
        /// Test whispering from master to slave
        /// </summary>
        [Fact(DisplayName = "WhisperAtoB")]
        public void WhisperAtoB()
        {
            bool receivedSentMessage = false;
            var slaveName = Context.Slave.Talker.Client.LocalUser.NickName;
            var handleMessage = new EventHandler<IrcRawMessageEventArgs>(
                delegate (object sender, IrcRawMessageEventArgs args)
                {
                    if (args.Message.Command == "WHISPER" && args.Message.Source.Name.ToLower() == Context.Master.Talker.Client.LocalUser.NickName)
                        receivedSentMessage = true;
                });
            Context.Slave.Whisperer.Client.RawMessageReceived += handleMessage;

            Stopwatch startedTime = new Stopwatch();
            startedTime.Start();
            Context.Master.WhisperTo(slaveName, "Testing @ unit tests at " + DateTime.Now.ToString(CultureInfo.InvariantCulture));
            while (!receivedSentMessage && startedTime.Elapsed < TimeSpan.FromSeconds(10))
            {
                Thread.Sleep(500);
            }

            Context.Slave.Whisperer.Client.RawMessageReceived -= handleMessage;

            Assert.True(receivedSentMessage);
        }
    }
}
