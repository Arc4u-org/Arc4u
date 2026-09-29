namespace Arc4u.Configuration.Sql;
/// <summary>The options of a SQL Server cache.</summary>
public class SqlCacheOption
{
    /// <summary>Gets or sets the connection string of the SQL Server database.</summary>
    public string? ConnectionString { get; set; }
    /// <summary>Gets or sets the name of the cache table. Default is <c>SqlCache</c>.</summary>
    public string TableName { get; set; } = "SqlCache";
    /// <summary>Gets or sets the schema of the cache table. Default is <c>dbo</c>.</summary>
    public string SchemaName { get; set; } = "dbo";
    /// <summary>Gets or sets the name of the <see cref="Arc4u.Serializer.IObjectSerialization"/> registered with this key to use; when empty or not found, the default serializer is used.</summary>
    public string? SerializerName { get; set; }
}
