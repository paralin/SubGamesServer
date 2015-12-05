namespace SubGames.Server.Twitch.Chat.Twitch.Enums
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

 #region Whisper

        /// <summary>
        /// Checking the whisper server
        /// </summary>
        TwitchGroupApi,

        /// <summary>
        /// Joining the Twitch IRC channel for the group
        /// </summary>
        JoiningChannel,

        #endregion

        /// <summary>
        /// Ready to do whatever
        /// </summary>
        Ready
    }
}
