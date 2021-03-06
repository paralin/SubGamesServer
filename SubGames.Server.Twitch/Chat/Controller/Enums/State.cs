﻿namespace SubGames.Server.Twitch.Chat.Controller.Enums
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
        /// Waiting for Bots to connect
        /// </summary>
        Connecting,

        /// <summary>
        /// Connected and ready.
        /// </summary>
        Ready
    }
}
