namespace SubGames.Server.Twitch.Chat.Twitch.Enums
{
    /// <summary>
    /// Triggers
    /// </summary>
    public enum Trigger
    {
        Disconnected,
        Connected,
        SignedIn,
        AuthInvalid,
        AuthRetry,
        ConnectRequested,
        ApiCheckFailed,
        GroupInfoReceived,
        JoinedGroupChat,
        DisconnectRequested
    }
}
