namespace Arc4u.OAuth2.TicketStore
{
    /// <summary>Configures the <see cref="FileTicketStore"/>.</summary>
    public class FileTicketStoreOptions
    {
        /// <summary>Gets or sets the directory in which the tickets are stored. Required.</summary>
        public DirectoryInfo? StorePath { get; set; } = default!;

        /// <summary>
        /// Gets or sets the Data Protection purpose under which the tickets are encrypted. The default is
        /// <see cref="TicketStoreDataProtection.DefaultPurpose"/> (<c>Arc4u.TicketStore.v1</c>). Changing it makes every stored ticket unreadable,
        /// so every user signs in again once.
        /// </summary>
        public string DataProtectionPurpose { get; set; } = TicketStoreDataProtection.DefaultPurpose;
    }
}
