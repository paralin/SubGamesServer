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
        Ready
    }
}
