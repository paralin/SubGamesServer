using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using IrcDotNet;
using log4net;
using Newtonsoft.Json;
using Stateless;
using SubGames.Server.Twitch.Api;
using SubGames.Server.Twitch.Chat.Twitch.Enums;
using SubGames.Server.Twitch.Model;

namespace SubGames.Server.Twitch.Chat.Twitch
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
        private bool _running;

        /// <summary>
        /// Invalid credentials event
        /// </summary>
        public event EventHandler InvalidCreds;

        /// <summary>
        /// When ready
        /// </summary>
        public event EventHandler Ready;

        /// <summary>
        /// Not ready
        /// </summary>
        public event EventHandler Unready;

        /// <summary>
        /// Message received in an irc channel
        /// </summary>
        public event EventHandler<IrcMessageEventArgs> MessageReceived;

        /// <summary>
        /// State of the bot
        /// </summary>
        public State State => _stateMachine.State;

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
        public TwitchIrcClient Client { get; private set; }

        /// <summary>
        /// Register timeout
        /// </summary>
        private Timer RegisterTimeout { get; set; }

        /// <summary>
        /// Auth info
        /// </summary>
        private AuthInfo _authInfo;

        /// <summary>
        /// Current channel list
        /// </summary>
        private HashSet<string> _joinedChannels;

        /// <summary>
        /// Target group chat room
        /// </summary>
        public TwitchChatRoom GroupChatRoom { get; private set; }

        /// <summary>
        /// Is this a whisper client?
        /// </summary>
        public bool WhisperClient { get; private set; }

        /// <summary>
        /// Create a Twitch chatbot.
        /// </summary>
        public ChatBot(AuthInfo info, bool whisper, int reconnectTime = 3000)
        {
            WhisperClient = whisper;
            _joinedChannels = new HashSet<string>();
            _authInfo = info;
            _shouldReconnect = reconnectTime > 0;
            if (_shouldReconnect)
            {
                _reconnectTimer = new Timer(reconnectTime);
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
                .PermitIf(Trigger.ConnectRequested, whisper ? State.TwitchGroupApi : State.Authenticating, () => _running);

            _state.Configure(State.RetryConnection)
                .SubstateOf(State.SignedOff)
                .OnExit(() => _reconnectTimer.Stop())
                .OnEntry(() => _reconnectTimer.Start())
                .PermitDynamic(Trigger.ConnectRequested, () => whisper ? State.TwitchGroupApi : State.Twitch);

            _state.Configure(State.TwitchGroupApi)
                .SubstateOf(State.Conceived)
                .OnEntry(CheckGroupApi)
                .Permit(Trigger.ApiCheckFailed, State.RetryConnection)
                .Permit(Trigger.GroupInfoReceived, State.Authenticating);

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
                .SubstateOf(State.Twitch)
                .OnEntry(() => Ready?.Invoke(this, EventArgs.Empty))
                .OnExit(() => Unready?.Invoke(this, EventArgs.Empty));
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

        /// <summary>
        /// Initialize the twitch connection
        /// </summary>
        private void InitializeTwitchConnection()
        {
            string server = WhisperClient
                ? TwitchServers.ServerClusters["group"].Servers.OrderBy(qu => Guid.NewGuid()).First().Split(':')[0] : "irc.twitch.tv";

            // Temporary from chatdepot.twitch.tv
            var chatDepotServer = GroupChatRoom?.Servers.FirstOrDefault(m => m.EndsWith("6667") && !m.StartsWith("10."));
            if (chatDepotServer != null)
                server = chatDepotServer.Split(':')[0];

            _log.Debug("Connecting to " + (WhisperClient ? "whisper" : "normal") + " server " + server + "...");
            var client = Client = new TwitchIrcClient
            {
                FloodPreventer = new IrcStandardFloodPreventer(4, 2000)
            };
            client.Disconnected += TwitchOnDisconnected;
            client.ConnectFailed += TwitchOnDisconnected;
            client.Registered += TwitchOnRegistered;
            client.Connected += (sender, args) => _log.Debug("Connected, awaiting registration...");
            client.Connect(server, false, new IrcUserRegistrationInfo()
            {
                NickName = _authInfo.Username.ToLower(),
                Password = _authInfo.Password,
                UserName = _authInfo.Username.ToLower()
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
            _log.Debug("Registered successfully.");

            RegisterTimeout?.Dispose();
            RegisterTimeout = null;

            _stateMachine.Fire(Trigger.SignedIn);

            Client.LocalUser.NoticeReceived += OnNoticeReceived;
            Client.LocalUser.MessageReceived += (o, args) => OnMessageReceived(o, args);
            Client.LocalUser.MessageReceived += (o, args) => MessageReceived?.Invoke(this, args);
            Client.LocalUser.JoinedChannel += OnJoinedChannel;
            Client.LocalUser.LeftChannel += OnLeftChannel;

            _log.Debug("Initializing twitch membership capability...");
            Client.SendRawMessage("CAP REQ :twitch.tv/membership");
            Client.SendRawMessage("CAP REQ :twitch.tv/commands");

            if (!WhisperClient)
                Client.SendRawMessage("JOIN #quantumdota");
            else
                JoinGroupChannel();
        }

        /// <summary>
        /// Event: on left channel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ircChannelEventArgs"></param>
        private void OnLeftChannel(object sender, IrcChannelEventArgs ircChannelEventArgs)
        {
            // Unregister the channel events
            _joinedChannels.Remove(ircChannelEventArgs.Channel.Name);
        }

        /// <summary>
        /// Event: on joined channel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ircChannelEventArgs"></param>
        private void OnJoinedChannel(object sender, IrcChannelEventArgs ircChannelEventArgs)
        {
            _log.Debug("Joined channel " + ircChannelEventArgs.Channel.Name + "...");

            // Register the channel events
            var chan = ircChannelEventArgs.Channel;
            _joinedChannels.Add(ircChannelEventArgs.Channel.Name);
            chan.MessageReceived += (o, args) =>
            {
                OnMessageReceived(o, args, ircChannelEventArgs.Channel);
                MessageReceived?.Invoke(this, args);
            };
            chan.NoticeReceived += OnNoticeReceived;

            // Say hello
            if (!WhisperClient)
                Client.LocalUser.SendMessage("#" + ircChannelEventArgs.Channel.Name, "Hello world");
            // Start periodic message timer (?)

            // Check if joined group channel
            if (GroupChatRoom != null && GroupChatRoom.IrcChannel == ircChannelEventArgs.Channel.Name.Substring(1))
            {
                // _stateMachine.Fire(Trigger.JoinedGroupChat);
                Client.LocalUser.SendMessage("#" + GroupChatRoom.IrcChannel, ".w paralin HeyGuys");
            }
        }

        /// <summary>
        /// Event: on message received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ircMessageEventArgs"></param>
        private void OnMessageReceived(object sender, IrcMessageEventArgs ircMessageEventArgs, IrcChannel channel = null)
        {
            if (channel == null)
                _log.Debug(ircMessageEventArgs.Source.Name + ": " + ircMessageEventArgs.Text);
            else
                _log.Debug("[" + channel.Name + "] " + ircMessageEventArgs.Source.Name + ": " + ircMessageEventArgs.Text);
        }

        /// <summary>
        /// Event: on notice received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ircMessageEventArgs"></param>
        private void OnNoticeReceived(object sender, IrcMessageEventArgs ircMessageEventArgs)
        {
            _log.Debug("NOTICE: " + ircMessageEventArgs.Text);
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
        /// State entry - join the group channel.
        /// </summary>
        private void JoinGroupChannel()
        {
            Client.SendRawMessage("JOIN #" + GroupChatRoom.IrcChannel);
        }

        /// <summary>
        /// State entry - check the group chat api
        /// </summary>
        private void CheckGroupApi()
        {
            var memberships = JsonConvert.DeserializeObject<TwitchRoomMemberships>(TwitchChatDepot.GetRoomMemberships(_authInfo.Password).Content);
            GroupChatRoom = memberships.Memberships.FirstOrDefault()?.Room;
            _stateMachine.Fire(Trigger.GroupInfoReceived);
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

            _joinedChannels.Clear();
        }
    }
}
