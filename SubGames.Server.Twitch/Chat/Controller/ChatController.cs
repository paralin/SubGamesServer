using System;
using System.Threading.Tasks;
using log4net;
using Stateless;
using SubGames.Server.Twitch.Chat.Controller.Enums;
using SubGames.Server.Twitch.Chat.Twitch;
using SubGames.Server.Twitch.Model;

namespace SubGames.Server.Twitch.Chat.Controller
{
    /// <summary>
    /// Twitch chatbot.
    /// </summary>
    public class ChatController
    {
        /// <summary>
        /// State machine
        /// </summary>
        private readonly StateMachine<State, Trigger> _stateMachine;
        private readonly ILog _log = LogManager.GetLogger("ChatController");

        /// <summary>
        /// Running
        /// </summary>
        private bool _running;

        /// <summary>
        /// Whisper bot
        /// </summary>
        public ChatBot Whisperer { get; }

        /// <summary>
        /// Talker bot
        /// </summary>
        public ChatBot Talker { get; }

        /// <summary>
        /// Event - on ready
        /// </summary>
        public event EventHandler Ready;

        /// <summary>
        /// Event - on unready
        /// </summary>
        public event EventHandler Unready;

        /// <summary>
        /// State of the bot
        /// </summary>
        public State State => _stateMachine.State;

        /// <summary>
        /// Create a Twitch chatbot.
        /// </summary>
        public ChatController(AuthInfo info)
        {
            var _state = _stateMachine = new StateMachine<State, Trigger>(State.SignedOff);
            _state.OnTransitioned((transition =>
            {
                _log.DebugFormat("{0} => {1}", transition.Source.ToString("G"), transition.Destination.ToString("G"));
            }));

            _state.Configure(State.Conceived)
                .Permit(Trigger.DisconnectRequested, State.SignedOff);

            _state.Configure(State.SignedOff)
                .SubstateOf(State.Conceived)
                .OnEntry(() => Task.Run(() => Whisperer.Stop()))
                .OnEntry(() => Task.Run(() => Talker.Stop()))
                .Ignore(Trigger.ChatbotsUnready)
                .Permit(Trigger.ConnectRequested, State.Connecting);

            _state.Configure(State.Connecting)
                .SubstateOf(State.Conceived)
                .OnEntry(() => Task.Run(() => Whisperer.Start()))
                .OnEntry(() => Task.Run(() => Talker.Start()))
                .Ignore(Trigger.ConnectRequested)
                .Ignore(Trigger.ChatbotsUnready)
                .Permit(Trigger.ChatbotsReady, State.Ready);

            _state.Configure(State.Ready)
                .SubstateOf(State.Conceived)
                .Ignore(Trigger.ChatbotsReady)
                .OnEntry(() => Ready?.Invoke(this, EventArgs.Empty))
                .OnExit(() => Unready?.Invoke(this, EventArgs.Empty));

            Whisperer = new ChatBot(info, true);
            Whisperer.Ready += CheckBotStates;
            Whisperer.Unready += CheckBotStates;
            Talker = new ChatBot(info, false);
            Talker.Ready += CheckBotStates;
            Talker.Unready += CheckBotStates;
        }

        private void CheckBotStates(object sender, EventArgs eventArgs)
        {
            if (Whisperer.State == Twitch.Enums.State.Ready && Talker.State == Twitch.Enums.State.Ready)
                _stateMachine.Fire(Trigger.ChatbotsReady);
            else
                _stateMachine.Fire(Trigger.ChatbotsUnready);
        }

        /// <summary>
        /// Send a whisper to a user
        /// </summary>
        /// <param name="user">username of the target</param>
        /// <param name="text">whisper message text</param>
        public void WhisperTo(string user, string text)
        {
            Whisperer.Client.LocalUser.SendMessage("#" + Whisperer.GroupChatRoom.IrcChannel, ".w " + user + " " + text);
        }

        public void Start()
        {
            if (_running) return;
            _running = true;
            _stateMachine.Fire(Trigger.ConnectRequested);
        }

        public void Stop()
        {
            if (_running)
            {
                _running = false;
                _stateMachine.Fire(Trigger.DisconnectRequested);
            }
        }
    }
}
