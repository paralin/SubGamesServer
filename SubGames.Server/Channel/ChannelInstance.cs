using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Dota2.GC.Dota.Internal;
using KellermanSoftware.CompareNetObjects;
using log4net;
using Stateless;
using SteamKit2;
using SubGames.Server.Dota2.Bot;
using SubGames.Server.Model;
using SubGames.Server.Twitch.Chat.Controller;
using SubGames.Server.Twitch.Chat.Controller.Enums;
using State = SubGames.Server.Channel.Enums.State;

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
        /// Current state
        /// </summary>
        public State State => _state.State;

        /// <summary>
        /// Channel state
        /// </summary>
        public ChannelState ChannelState { get; private set; }

        /// <summary>
        /// Logger
        /// </summary>
        private ILog _log;

        /// <summary>
        /// Management state update timer
        /// </summary>
        private Timer _lobbyManageTimer;

        /// <summary>
        /// Create a channel instance
        /// <param name="config">Channel config information</param>
        /// </summary>
        public ChannelInstance(ChannelConfig config, ChannelState state)
        {
            ChannelState = state;
            Config = config;
            _log = LogManager.GetLogger(config.Channel);
            Chat = new ChatController(config.TwitchAuth);
            Dota = new DotaBot(config.DotaAuth);

            _lobbyManageTimer = new Timer(5000);
            _lobbyManageTimer.Stop();
            _lobbyManageTimer.Elapsed += UpdateLobbyState;

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
                .OnEntry(() => _log.Debug("Ready"))
                .PermitDynamic(Trigger.DotaEnteredLobby, () => Dota.IsLobbyHost ? State.ManageLobby : State.AcquireOwnershipLobby)
                .OnEntryFrom(Trigger.LobbyDeleteRequsted, () =>
                {
                    Dota.LeaveLobby(true);
                });

            _state.Configure(State.DotaLobby)
                .SubstateOf(State.Ready)
                .Permit(Trigger.DotaLostLobby, State.Ready)
                .Permit(Trigger.LobbyDeleteRequsted, State.Ready);

            _state.Configure(State.AcquireOwnershipLobby)
                .SubstateOf(State.DotaLobby)
                .Permit(Trigger.LobbyBecameOwner, State.ManageLobby)
                .OnEntry(RequestLobbyOwnership);

            _state.Configure(State.ManageLobby)
                .SubstateOf(State.DotaLobby)
                .OnEntry(VerifyLobby)
                .OnEntry(() => SendHelp());

            _state.Configure(State.LobbyPlay)
                .SubstateOf(State.DotaLobby)
                .OnEntry(() => _log.Debug("DOTA started playing."))
                .OnEntry(() => Dota.LeaveLobby());

            Dota.StateTransitioned += (sender, transition) => CheckBotStates();
            Dota.LobbyInviteReceived += (sender, invite) =>
            {
                if (invite.sender_id == Config.OwnerSteamId)
                {
                    _log.Debug("Received lobby invite from owner, accepting.");
                    Dota.DotaGcHandler.RespondLobbyInvite(invite.group_id);
                    return;
                }
                _log.Warn("Received lobby invite from unknown user, declining.");
                Dota.DotaGcHandler.RespondLobbyInvite(invite.group_id, false);
            };
            Dota.PartyInviteReceived += (sender, invite) =>
            {
                if (invite.sender_id == Config.OwnerSteamId)
                {
                    _log.Debug("Received party invite from owner, accepting.");
                    Dota.DotaGcHandler.RespondPartyInvite(invite.group_id);
                    return;
                }
                _log.Warn("Received party invite from unknown user, declining.");
                Dota.DotaGcHandler.RespondPartyInvite(invite.group_id, false);
            };
            Dota.LobbyUpdate += (sender, lobby) =>
            {
                if (lobby == null) return;
                if (_state.CanFire(Trigger.LobbyBecameOwner) && Dota.IsLobbyHost)
                    _state.Fire(Trigger.LobbyBecameOwner);
                var logic = new CompareLogic(new ComparisonConfig()
                {
                    MaxDifferences = int.MaxValue
                });

                var compare = logic.Compare(lobby, Dota.Lobby);
                _log.Debug(compare.DifferencesString);
                if (State == State.ManageLobby)
                {

                }
            };
            Dota.JoinedLobbyChat += (sender, args) =>
            {
                if (_state.CanFire(Trigger.LobbyBecameOwner))
                {
                    var owner = Dota.DotaGcHandler.Lobby.members.FirstOrDefault(m => m.id == Dota.Lobby.leader_id);
                    if (owner == null)
                    {
                        _log.Error("There is no owner for this lobby?");
                        return;
                    }

                    var msg = "Hey, " + owner.name + ", please set me as Lobby Host so I can invite people.";
                    Dota.SendLobbyMessage(msg);
                    if (Dota.SteamFriends.GetFriendRelationship(new SteamID(owner.id)) == EFriendRelationship.Friend)
                        Dota.SteamFriends.SendChatMessage(new SteamID(owner.id), EChatEntryType.ChatMsg, msg);
                }
            };
        }

        private void SendHelp()
        {
            var msgs = new string[] {"Commands:", " - Mark player slot as Passive Bot to request a subscriber.", " - Suggest invite to invite players."};
            foreach (var msg in msgs)
            {
                Dota.SendLobbyMessage(msg);
            }
        }

        private void UpdateLobbyState(object sender, ElapsedEventArgs elapsedEventArgs)
        {
        }

        private void VerifyLobby()
        {
            ChannelState.LobbyId = Dota.DotaGcHandler.Lobby.lobby_id;
        }

        private void RequestLobbyOwnership()
        {
            if (Dota.IsLobbyHost)
            {
                _state.Fire(Trigger.LobbyBecameOwner);
                return;
            }
        }

        /// <summary>
        /// Create an instance with just a config.
        /// </summary>
        /// <param name="config"></param>
        public ChannelInstance(ChannelConfig config) : this(config, new ChannelState())
        {
        }

        private void CheckBotStates()
        {
            if (Dota.State >= Dota2.Bot.Enums.State.DotaMenu && Chat.State == Twitch.Chat.Controller.Enums.State.Ready)
                _state.Fire(Trigger.ChatbotsReady);
            else
                _state.Fire(Trigger.ChatbotsUnready);

            if (Dota.State == Dota2.Bot.Enums.State.DotaLobby && _state.CanFire(Trigger.DotaEnteredLobby))
                _state.Fire(Trigger.DotaEnteredLobby);
            else if(Dota.State < Dota2.Bot.Enums.State.DotaLobby && _state.CanFire(Trigger.DotaLostLobby))
                _state.Fire(Trigger.DotaLostLobby);

            if (Dota.State == Dota2.Bot.Enums.State.DotaPlay && _state.CanFire(Trigger.DotaEnteredPlay))
                _state.Fire(Trigger.DotaEnteredPlay);
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
