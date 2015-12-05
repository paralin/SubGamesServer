using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using IrcDotNet;
using log4net;
using Newtonsoft.Json;
using Stateless;
using SubGames.Server.Twitch.Api;
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
        public ChatBot Whisperer { get; private set; }

        /// <summary>
        /// Talker bot
        /// </summary>
        public ChatBot Talker { get; private set; }

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
                .Ignore(Trigger.ChatbotsReady);

            Whisperer = new ChatBot(info, true);
            Whisperer.Ready += CheckBotStates;
            Whisperer.Unready += CheckBotStates;
            Talker = new ChatBot(info, false);
            Talker.Ready += CheckBotStates;
            Talker.Unready += CheckBotStates;

            Talker.MessageReceived += (sender, args) =>
            {
                Whisperer.Client.LocalUser.SendMessage("#" + Whisperer.GroupChatRoom.IrcChannel, ".w paralin [" + args.Source.Name + "] " + args.Text);
            };
        }

        private void CheckBotStates(object sender, EventArgs eventArgs)
        {
            if (Whisperer.State == Twitch.Enums.State.Ready && Talker.State == Twitch.Enums.State.Ready)
                _stateMachine.Fire(Trigger.ChatbotsReady);
            else
                _stateMachine.Fire(Trigger.ChatbotsUnready);
        }

        public void Start()
        {
            if (!_running)
            {
                _running = true;
                _stateMachine.Fire(Trigger.ConnectRequested);
            }
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
