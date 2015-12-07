using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using IrcDotNet;
using SubGames.Server.Channel.Enums;
using SubGames.Server.Test.Utils;
using Xunit;
using Xunit.Abstractions;

namespace SubGames.Server.Test
{
    /// <summary>
    /// End to end tests
    /// </summary>
    [TestCaseOrderer("SubGames.Server.Test.Utils.PriorityOrderer", "SubGames.Server.Test")]
    [Collection("Chat controller")]
    public class EndToEnd : LogOutputTester, IClassFixture<EndToEndTestContext>
    {
        public EndToEndTestContext Context { get; set; }
        private ITestOutputHelper output;

        public EndToEnd(EndToEndTestContext ctx, ITestOutputHelper helper) : base(helper)
        {
            this.Context = ctx;
            this.output = helper;
        }

        /// <summary>
        /// The bots should connect to the server.
        /// </summary>
        [Fact(DisplayName = "ReadyUp"), TestPriority(-1)]
        public void ControllerReadyUp()
        {
            Stopwatch startedTime = new Stopwatch();
            startedTime.Start();
            Task.Run(() => Context.Instance.Start());
            while (startedTime.Elapsed < TimeSpan.FromSeconds(30)
                && (Context.Instance.State != State.Ready))
            {
                Thread.Sleep(500);
            }
            Assert.Equal(Context.Instance.State, State.Ready);
        }
    }
}
