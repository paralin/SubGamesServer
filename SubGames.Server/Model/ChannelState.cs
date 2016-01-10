namespace SubGames.Server.Model
{
    /// <summary>
    /// Channel state
    /// </summary>
    public class ChannelState
    {
        /// <summary>
        /// If currently in a lobby
        /// </summary>
        public ulong LobbyId { get; set; }

        /// <summary>
        /// If currently in a party
        /// </summary>
        public ulong PartyId { get; set; }
    }
}
