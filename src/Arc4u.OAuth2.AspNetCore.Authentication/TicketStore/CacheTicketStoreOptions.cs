namespace Arc4u.OAuth2.TicketStore
{
    /// <summary>Configures the <see cref="CacheTicketStore"/>.</summary>
    public class CacheTicketStoreOptions
    {
        /// <summary>Gets or sets the name of the cache in which the tickets are stored. The default is <c>Default</c>.</summary>
        public string CacheName { get; set; } = "Default";

        /// <summary>Gets or sets the prefix of the keys of the tickets. The default is <c>AuthSessionStore-</c>.</summary>
        public string KeyPrefix { get; set; } = "AuthSessionStore-";

        /// <summary>
        /// Gets or sets the Data Protection purpose under which the tickets are encrypted. The default is
        /// <see cref="TicketStoreDataProtection.DefaultPurpose"/> (<c>Arc4u.TicketStore.v1</c>). It is not a secret: it separates the tickets from
        /// the other uses of the key ring. Changing it makes every stored ticket unreadable, so every user signs in again once; an application
        /// whose sessions were encrypted under another purpose (such as Arc4u.Portal's <c>Portal.TicketStore.v1</c>) sets it to keep them.
        /// </summary>
        public string DataProtectionPurpose { get; set; } = TicketStoreDataProtection.DefaultPurpose;
    }
}

