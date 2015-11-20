using System;
using System.Timers;
using IrcDotNet;
using log4net;
using Stateless;
using SubGames.Server.Twitch.Chat.Enums;
using SubGames.Server.Twitch.Model;

namespace SubGames.Server.Twitch.Chat
{
    /// <summary>
    /// Twitch chatbot.
    /// </summary>
    public class ChatBot
    {
        /// <summary>
        /// State machine
        /// </summary>
        private readonly StateMachine<State, Trigger> _stateMachine;
        private readonly ILog _log = LogManager.GetLogger("ChatBot");

        /// <summary>
        /// Running
        /// </summary>
        private bool _running = false;

        /// <summary>
        /// Invalid credentials event
        /// </summary>
        public event EventHandler InvalidCreds;

        /// <summary>
        /// Reconnect timer
        /// </summary>
        private readonly Timer _reconnectTimer;

        /// <summary>
        /// IF we should reconnect
        /// </summary>
        private readonly bool _shouldReconnect;

        /// <summary>
        /// IRC Client
        /// </summary>
        private TwitchIrcClient Client { get; set; }
        
        /// <summary>
        /// Register timeout
        /// </summary>
        private Timer RegisterTimeout { get; set; }

        /// <summary>
        /// Auth info
        /// </summary>
        private AuthInfo _authInfo;

        /// <summary>
        /// Create a Twitch chatbot.
        /// </summary>
        public ChatBot(AuthInfo info, int reconnectTime = 3000)
        {
            _authInfo = info;
            _shouldReconnect = reconnectTime > 0;
            if (_shouldReconnect)
            {
                _reconnectTimer = new System.Timers.Timer(reconnectTime);
                _reconnectTimer.Elapsed += (sender, args) =>
                {
                    _reconnectTimer.Stop();
                    _stateMachine.Fire(Trigger.ConnectRequested);
                };
            }

            var _state = _stateMachine = new StateMachine<State, Trigger>(State.SignedOff);
            _state.OnTransitioned((transition =>
            {
                _log.DebugFormat("{0} => {1}", transition.Source.ToString("G"), transition.Destination.ToString("G"));
            }));

            _state.Configure(State.Conceived)
                .Permit(Trigger.DisconnectRequested, State.SignedOff);

            _state.Configure(State.SignedOff)
                .SubstateOf(State.Conceived)
                .Ignore(Trigger.Disconnected)
                .OnEntryFrom(Trigger.AuthInvalid, () => InvalidCreds?.Invoke(this, EventArgs.Empty))
                .PermitIf(Trigger.ConnectRequested, State.Twitch, () => _running);

            _state.Configure(State.RetryConnection)
                .SubstateOf(State.SignedOff)
                .OnExit(() => _reconnectTimer.Stop())
                .OnEntry(() => _reconnectTimer.Start())
                .Permit(Trigger.ConnectRequested, State.Twitch);

            _state.Configure(State.Twitch)
                .SubstateOf(State.Conceived)
                .Permit(Trigger.Connected, State.Authenticating)
                .PermitDynamic(Trigger.Disconnected,
                    () => _shouldReconnect ? State.RetryConnection : State.SignedOff)
                .Permit(Trigger.AuthInvalid, State.SignedOff)
                .OnEntry(InitializeTwitchConnection)
                .OnExit(ReleaseTwitchConnection);

            _state.Configure(State.Authenticating)
                .SubstateOf(State.Twitch)
                .Permit(Trigger.SignedIn, State.Ready)
                .PermitReentry(Trigger.AuthRetry)
                .Permit(Trigger.AuthInvalid, State.SignedOff);

            _state.Configure(State.Ready)
                .SubstateOf(State.Twitch);
        }

        /// <summary>
        /// Initialize the twitch connection
        /// </summary>
        private void InitializeTwitchConnection()
        {
            var server = "irc.twitch.tv";
            _log.Debug("Connecting to server " + server + "...");
            var client = Client = new TwitchIrcClient
            {
                FloodPreventer = new IrcStandardFloodPreventer(4, 2000)
            };
            client.Disconnected += TwitchOnDisconnected;
            client.Registered += TwitchOnRegistered;
            client.Connected += (sender, args) => _log.Debug("Connected, awaiting registration...");
            client.Connect(server, false, new IrcUserRegistrationInfo()
            {
                NickName = _authInfo.Username,
                Password = _authInfo.Password,
                UserName = _authInfo.Username
            });
            RegisterTimeout = new Timer(10000);
            RegisterTimeout.Elapsed += (sender, args) =>
            {
                RegisterTimeout.Stop();
                _log.Debug("Registration timed out...");
                _stateMachine.Fire(Trigger.Disconnected);
            };
            RegisterTimeout.Start();
        }

        /// <summary>
        /// Callback for Twitch registered event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void TwitchOnRegistered(object sender, EventArgs eventArgs)
        {
            RegisterTimeout?.Dispose();
            RegisterTimeout = null;

            _log.Debug("Registered successfully.");
            _stateMachine.Fire(Trigger.SignedIn);

            Client.LocalUser.NoticeReceived += OnNoticeReceived;
            Client.LocalUser.MessageReceived += OnMessageReceived;
            Client.LocalUser.JoinedChannel += OnJoinedChannel;
            Client.LocalUser.LeftChannel += OnLeftChannel;
        }

        /// <summary>
        /// Event: on left channel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ircChannelEventArgs"></param>
        private void OnLeftChannel(object sender, IrcChannelEventArgs ircChannelEventArgs)
        {
            // Unregister the channel events
        }

        /// <summary>
        /// Event: on joined channel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ircChannelEventArgs"></param>
        private void OnJoinedChannel(object sender, IrcChannelEventArgs ircChannelEventArgs)
        {
            // Register the channel events
        }

        /// <summary>
        /// Event: on message received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ircMessageEventArgs"></param>
        private void OnMessageReceived(object sender, IrcMessageEventArgs ircMessageEventArgs)
        {
        }

        /// <summary>
        /// Event: on notice received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ircMessageEventArgs"></param>
        private void OnNoticeReceived(object sender, IrcMessageEventArgs ircMessageEventArgs)
        {
        }

        /// <summary>
        /// Twitch on disconnected.
        /// </summary>
        /// <param name="sender">Sender twitch client</param>
        /// <param name="eventArgs">Event arguments</param>
        private void TwitchOnDisconnected(object sender, EventArgs eventArgs)
        {
            _log.Debug("Disconnected from twitch.");
            _stateMachine.Fire(Trigger.Disconnected);
        }

        /// <summary>
        /// Shutdown / release it.
        /// </summary>
        private void ReleaseTwitchConnection()
        {
            RegisterTimeout?.Dispose();
            RegisterTimeout = null;

            Client?.Disconnect();
            Client?.Dispose();
            Client = null;
        }
    }
}
