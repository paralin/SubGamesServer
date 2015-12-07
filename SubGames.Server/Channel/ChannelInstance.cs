using System.Threading.Tasks;
using log4net;
using Stateless;
using SubGames.Server.Dota2.Bot;
using SubGames.Server.Model;
using SubGames.Server.Twitch.Chat.Controller;
using SubGames.Server.Twitch.Chat.Controller.Enums;
using State = SubGames.Server.Twitch.Chat.Controller.State;

namespace SubGames.Server.Channel
{
    /// <summary>
    /// A channel instance that corresponds to a Twitch streamer
    /// </summary>
    public class ChannelInstance
    {
        /// <summary>
        /// Chatbot for Twitch
        /// </summary>
        public ChatController Chat { get; private set; }

        /// <summary>
        /// DOTA bot
        /// </summary>
        public DotaBot Dota { get; private set; }

        /// <summary>
        /// Channel config
        /// </summary>
        public ChannelConfig Config { get; private set; }

        /// <summary>
        /// State machine
        /// </summary>
        private StateMachine<State, Trigger> _state;

        /// <summary>
        /// Logger
        /// </summary>
        private ILog _log;

        /// <summary>
        /// Create a channel instance
        /// <param name="config">Channel config information</param>
        /// </summary>
        public ChannelInstance(ChannelConfig config)
        {
            Config = config;
            _log = LogManager.GetLogger(config.Channel);
            Chat = new ChatController(config.TwitchAuth);
            Dota = new DotaBot(config.DotaAuth);

            _state = new StateMachine<State, Trigger>(State.SignedOff);
            _state.OnTransitioned((transition =>
            {
                _log.DebugFormat("{0} => {1}", transition.Source.ToString("G"), transition.Destination.ToString("G"));
            }));

            _state.Configure(State.Conceived)
                .Permit(Trigger.DisconnectRequested, State.SignedOff);

            _state.Configure(State.SignedOff)
                .SubstateOf(State.Conceived)
                .OnEntry(() => Task.Run(() => Dota.Stop()))
                .OnEntry(() => Task.Run(() => Chat.Stop()))
                .Ignore(Trigger.ChatbotsUnready)
                .Permit(Trigger.ConnectRequested, State.Connecting);

            _state.Configure(State.Connecting)
                .SubstateOf(State.Conceived)
                .OnEntry(() => Task.Run(() => Dota.Start()))
                .OnEntry(() => Task.Run(() => Chat.Start()))
                .Ignore(Trigger.ConnectRequested)
                .Ignore(Trigger.ChatbotsUnready)
                .Permit(Trigger.ChatbotsReady, State.Ready);

            _state.Configure(State.Ready)
                .SubstateOf(State.Conceived)
                .Ignore(Trigger.ChatbotsReady)
                .OnEntry(() => _log.Debug("Ready"));

            Dota.StateTransitioned += (sender, transition) => CheckBotStates();
        }

        private void CheckBotStates()
        {
            if (Dota.State >= Dota2.Bot.Enums.State.DotaMenu && Chat.State == Twitch.Chat.Controller.Enums.State.Ready)
                _state.Fire(Trigger.ChatbotsReady);
            else
                _state.Fire(Trigger.ChatbotsUnready);
        }

        /// <summary>
        /// Start the channel
        /// </summary>
        public void Start()
        {
            _state.Fire(Trigger.ConnectRequested);
        }

        /// <summary>
        /// Stop the channel
        /// </summary>
        public void Stop()
        {
            _state.Fire(Trigger.DisconnectRequested);
        }
    }
}
