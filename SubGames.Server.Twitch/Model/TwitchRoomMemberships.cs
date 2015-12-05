using Newtonsoft.Json;

namespace SubGames.Server.Twitch.Model
{
    /// <summary>
    /// Twitch room memberships list
    /// </summary>
    public class TwitchRoomMemberships
    {
        /// <summary>
        /// Memberships
        /// </summary>
        [JsonProperty("memberships")]
        public TwitchChatMembership[] Memberships { get; set; }
    }

    /// <summary>
    /// Twitch chat group membership
    /// </summary>
    public class TwitchChatMembership
    {
        /// <summary>
        /// The room
        /// </summary>
        [JsonProperty("room")]
        public TwitchChatRoom Room { get; set; }

        /// <summary>
        /// The user owning the room
        /// </summary>
        [JsonProperty("user")]
        public TwitchUser User { get; set; }

        /// <summary>
        /// Is the bot an owner
        /// </summary>
        [JsonProperty("is_owner")]
        public bool IsOwner { get; set; }

        /// <summary>
        /// Is the bot a mod
        /// </summary>
        [JsonProperty("is_mod")]
        public bool IsMod { get; set; }

        /// <summary>
        /// Is the bot confirmed
        /// </summary>
        [JsonProperty("is_confirmed")]
        public bool IsConfirmed { get; set; }

        /// <summary>
        /// Is the bot banned
        /// </summary>
        [JsonProperty("is_banned")]
        public bool IsBanned { get; set; }

        /// <summary>
        /// Created at time?
        /// </summary>
        [JsonProperty("created_at")]
        public ulong CreatedAt { get; set; }
    }

    /// <summary>
    /// Twitch user
    /// </summary>
    public class TwitchUser
    {
        /// <summary>
        /// ID
        /// </summary>
        [JsonProperty("id")]
        public ulong Id { get; set; }
    }

    public class TwitchChatRoom
    {
        /// <summary>
        /// IRC channel
        /// </summary>
        [JsonProperty("irc_channel")]
        public string IrcChannel { get; set; }

        /// <summary>
        /// Owner ID
        /// </summary>
        [JsonProperty("owner_id")]
        public ulong OwnerId { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Public invites enabled
        /// </summary>
        [JsonProperty("public_invites_enabled")]
        public bool PublicInvitesEnabled { get; set; }

        /// <summary>
        /// Which cluster is this hosted on
        /// </summary>
        [JsonProperty("cluster")]
        public string Cluster { get; set; }

        /// <summary>
        /// Servers list
        /// </summary>
        [JsonProperty("servers")]
        public string[] Servers { get; set; }

        /// <summary>
        /// URL for the chatters list API
        /// </summary>
        [JsonProperty("chatters_list_url")]
        public string ChattersListUrl { get; set; }
    }
}
