namespace Arc4u.OAuth2.TicketStore
{
    /// <summary>Configures the <see cref="CacheTicketStore"/>.</summary>
    public class CacheTicketStoreOptions
    {
        /// <summary>Gets or sets the name of the cache in which the tickets are stored. The default is <c>Default</c>.</summary>
        public string CacheName { get; set; } = "Default";

        /// <summary>Gets or sets the prefix of the keys of the tickets. The default is <c>AuthSessionStore-</c>.</summary>
        public string KeyPrefix { get; set; } = "AuthSessionStore-";
    }
}

