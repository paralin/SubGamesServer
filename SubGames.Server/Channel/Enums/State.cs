namespace SubGames.Server.Channel.Enums
{
    public enum State
    {
        /// <summary>
        /// General container state
        /// </summary>
        Conceived,

        /// <summary>
        /// All bots signed off
        /// </summary>
        SignedOff,

        /// <summary>
        /// Waiting for ChatBots to connect
        /// </summary>
        Connecting,

        /// <summary>
        /// Connected to both
        /// </summary>
        Ready,

        /// <summary>
        /// Lobby creation requested.
        /// </summary>
        DotaLobby,

        /// <summary>
        /// Take ownership of the lobby
        /// </summary>
        AcquireOwnershipLobby,

        /// <summary>
        /// Verify and manage lobby
        /// </summary>
        ManageLobby,

        /// <summary>
        /// Lobby is playing
        /// </summary>
        LobbyPlay,

        /// <summary>
        /// Party creation requested.
        /// </summary>
        DotaParty,

        /// <summary>
        /// Create/verify party and invite the host
        /// </summary>
        CreateParty,

        /// <summary>
        /// Currently sitting in the party
        /// </summary>
        ManageParty,
    }
}
