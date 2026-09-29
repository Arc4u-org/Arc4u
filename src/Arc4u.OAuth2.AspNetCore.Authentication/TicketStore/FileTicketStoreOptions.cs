namespace Arc4u.OAuth2.TicketStore
{
    /// <summary>Configures the <see cref="FileTicketStore"/>.</summary>
    public class FileTicketStoreOptions
    {
        /// <summary>Gets or sets the directory in which the tickets are stored. Required.</summary>
        public DirectoryInfo? StorePath { get; set; } = default!;
    }
}
