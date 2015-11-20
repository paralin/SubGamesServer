namespace SubGames.Server.Twitch.Chat.Enums
{
    /// <summary>
    /// Chatbot state
    /// </summary>
    public enum State
    {
        /// <summary>
        /// General umbrella state
        /// </summary>
        Conceived,

        /// <summary>
        /// Signed off
        /// </summary>
        SignedOff,

        /// <summary>
        /// Waiting to retry connection
        /// </summary>
        RetryConnection,

        /// <summary>
        /// Connecting to twitch
        /// </summary>
        Twitch,

        /// <summary>
        /// Authenticating
        /// </summary>
        Authenticating,

        /// <summary>
        /// Ready to join channel / etc
        /// </summary>
        Ready
    }
}
