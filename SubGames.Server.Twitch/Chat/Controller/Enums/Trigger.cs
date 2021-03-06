﻿namespace SubGames.Server.Twitch.Chat.Controller.Enums
{
    public enum Trigger
    {
        ConnectRequested,
        DisconnectRequested,
        ChatbotsReady,
        ChatbotsUnready,

        LobbyRequested,
        DotaEnteredLobby,
        DotaLostLobby,
        DotaEnteredPlay,
        LobbyBecameOwner,
        LobbyDeleteRequsted
    }
}
