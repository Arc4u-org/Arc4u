using System.Globalization;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using System.Text.Json.Serialization;

namespace Arc4u.Security.Principal;

/// <summary>
/// The class contains the main information on a User. The informations are 
/// retrieve from the AD but can be constructed from any other sources.
/// The class is binay and xml serialisable.
/// </summary>
//  [DataContract]
public sealed class UserProfile : IXmlSerializable
{
    /// <summary>
    /// Create an empty UserProfile object which is serializable!
    /// </summary>
    /// <returns>A valide empty UserpProfile</returns>
    public static UserProfile Empty
    {
        get
        {
            return new UserProfile("", "", "", "", "", "", "S-1-0-0", "", "", "", "", "", "", "", "", "", "", "", "", CultureInfo.InvariantCulture, "", "");
        }
    }

    // XmlSerializable
    /// <summary>
    /// Initializes a new, empty instance of the <see cref="UserProfile"/> class. It is used by the serializers.
    /// </summary>
    public UserProfile()
    {
        DisplayName = string.Empty;
        Email = string.Empty;
        Department = string.Empty;
        Company = string.Empty;
        GivenName = string.Empty;
        SurName = string.Empty;
        Sid = string.Empty;
        State = string.Empty;
        Mobile = string.Empty;
        Telephone = string.Empty;
        InternalPhone = string.Empty;
        Fax = string.Empty;
        PrincipalName = string.Empty;
        PostalCode = string.Empty;
        Street = string.Empty;
        Room = string.Empty;
        Initials = string.Empty;
        SamAccountName = string.Empty;
        Domain = string.Empty;
        _culture = CultureInfo.InvariantCulture.Name;
        _currentCulture = CultureInfo.InvariantCulture.Name;
        CommonName = string.Empty;
        Description = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserProfile"/> class.
    /// </summary>
    /// <param name="displayName">The display name.</param>
    /// <param name="email">The email.</param>
    /// <param name="department">The department.</param>
    /// <param name="company">The company.</param>
    /// <param name="givenName">Name of the given.</param>
    /// <param name="surName">Name of the sur.</param>
    /// <param name="sid">The sid.</param>
    /// <param name="state">The state.</param>
    /// <param name="mobile">The mobile.</param>
    /// <param name="telephone">The telephone.</param>
    /// <param name="internalPhone">The internal phone.</param>
    /// <param name="fax">The fax.</param>
    /// <param name="principalName">Name of the principal.</param>
    /// <param name="postalCode">The postal code.</param>
    /// <param name="street">Th street.</param>
    /// <param name="room">The room.</param>
    /// <param name="initials">The initials.</param>
    /// <param name="samAccountName">Name of the sam account.</param>
    /// <param name="domain">The Active Directory name.</param>
    /// <param name="culture">The culture.</param>
    /// <param name="commonName">Name of the common.</param>
    /// <param name="description">The description.</param>
    public UserProfile(
        string displayName,
        string email,
        string department,
        string company,
        string givenName,
        string surName,
        string sid,
        string state,
        string mobile,
        string telephone,
        string internalPhone,
        string fax,
        string principalName,
        string postalCode,
        string street,
        string room,
        string initials,
        string samAccountName,
        string domain,
        CultureInfo culture,
        string commonName,
        string description)
    {
        ArgumentNullException.ThrowIfNull(sid);
        ArgumentNullException.ThrowIfNull(culture);

        DisplayName = displayName;
        Email = email;
        Department = department;
        Company = company;
        GivenName = givenName;
        SurName = surName;
        Sid = sid;
        State = state;
        Mobile = mobile;
        Telephone = telephone;
        InternalPhone = internalPhone;
        Fax = fax;
        PrincipalName = principalName;
        PostalCode = postalCode;
        Street = street;
        Room = room;
        Initials = initials;
        SamAccountName = samAccountName;
        Domain = domain;
        Culture = culture;
        CurrentCulture = culture;
        CommonName = commonName;
        Description = description;
        _culture = culture.Name;
        _currentCulture = culture.Name;
    }

    #region IProfile Members

    /// <summary>
    /// Gets the display name.
    /// </summary>
    /// <value>The display name.</value>
    [DataMember]
    [JsonInclude]
    public string DisplayName { get; private set; }

    /// <summary>
    /// Gets the email.
    /// </summary>
    /// <value>The email.</value>
    [DataMember]
    [JsonInclude]
    public string Email { get; private set; }

    /// <summary>
    /// Gets the department.
    /// </summary>
    /// <value>The department.</value>
    [DataMember]
    [JsonInclude]
    public string Department { get; private set; }

    /// <summary>
    /// Gets the company.
    /// </summary>
    /// <value>The company.</value>
    [DataMember]
    [JsonInclude]
    public string Company { get; private set; }

    /// <summary>
    /// Gets the name of the given.
    /// </summary>
    /// <value>The name of the given.</value>
    [DataMember]
    [JsonInclude]
    public string GivenName { get; private set; }

    /// <summary>
    /// Gets the surname.
    /// </summary>
    /// <value>The surname.</value>
    [DataMember]
    [JsonInclude]
    public string SurName { get; private set; }

    /// <summary>
    /// Gets the sid.
    /// </summary>
    /// <value>The sid.</value>
    [DataMember]
    [JsonInclude]
    public string Sid { get; private set; }

    /// <summary>
    /// Gets the state.
    /// </summary>
    /// <value>The state.</value>
    [DataMember]
    [JsonInclude]
    public string State { get; private set; }

    /// <summary>
    /// Gets the mobile.
    /// </summary>
    /// <value>The mobile.</value>
    [DataMember]
    [JsonInclude]
    public string Mobile { get; private set; }
    /// <summary>
    /// Gets the telephone.
    /// </summary>
    /// <value>The telephone.</value>
    [DataMember]
    [JsonInclude]
    public string Telephone { get; private set; }
    /// <summary>
    /// Gets the internal phone.
    /// </summary>
    /// <value>The internal phone.</value>
    [DataMember]
    [JsonInclude]
    public string InternalPhone { get; private set; }
    /// <summary>
    /// Gets the fax.
    /// </summary>
    /// <value>The fax.</value>
    [DataMember]
    [JsonInclude]
    public string Fax { get; private set; }
    /// <summary>
    /// Gets the name of the principal.
    /// </summary>
    /// <value>The name of the principal.</value>
    [DataMember]
    [JsonInclude]
    public string PrincipalName { get; private set; }
    /// <summary>
    /// Gets the postal code.
    /// </summary>
    /// <value>The postal code.</value>
    [DataMember]
    [JsonInclude]
    public string PostalCode { get; private set; }

    /// <summary>
    /// Get the Street defined in the address.
    /// </summary>
    [DataMember]
    [JsonInclude]
    public string Street { get; private set; }
    /// <summary>
    /// Gets the room.
    /// </summary>
    /// <value>The room.</value>
    [DataMember]
    [JsonInclude]
    public string Room { get; private set; }
    /// <summary>
    /// Gets the initials.
    /// </summary>
    /// <value>The initials.</value>
    [DataMember]
    [JsonInclude]
    public string Initials { get; private set; }
    /// <summary>
    /// Gets the name of the sam account.
    /// </summary>
    /// <value>The name of the sam account.</value>
    [DataMember]
    [JsonInclude]
    public string SamAccountName { get; private set; }

    /// <summary>
    /// Get the domain.
    /// </summary>
    [DataMember]
    [JsonInclude]
    public string Domain { get; private set; }
    /// <summary>
    /// Gets the culture.
    /// </summary>
    /// <value>The culture.</value>
    [DataMember(Name = "Culture")]
    [JsonInclude]
    [JsonPropertyName("Culture")]
    private string _culture;

    /// <summary>
    /// Gets the culture of the user.
    /// </summary>
    /// <value>The culture.</value>
    [IgnoreDataMember]
    [XmlIgnore]
    [JsonIgnore]
    public CultureInfo Culture
    {
        get => new CultureInfo(_culture);
        private set => _culture = value.Name;
    }
    /// <summary>
    /// Gets or sets the current culture.
    /// </summary>
    /// <value>The current culture.</value>
    [DataMember(Name = "CurrentCulture")]
    [JsonPropertyName("CurrentCulture")]
    [JsonInclude]
    private string _currentCulture;

    /// <summary>
    /// Gets or sets the culture currently used by the user (initially the same as <see cref="Culture"/>).
    /// </summary>
    /// <value>The current culture.</value>
    [IgnoreDataMember]
    [XmlIgnore]
    [JsonIgnore]
    public CultureInfo CurrentCulture
    {
        get => new CultureInfo(_currentCulture);
        set => _currentCulture = value.Name;
    }

    /// <summary>
    /// Gets the name of the common.
    /// </summary>
    /// <value>The name of the common.</value>
    [DataMember]
    [JsonInclude]
    public string CommonName { get; private set; }
    /// <summary>
    /// Gets the description.
    /// </summary>
    /// <value>The description.</value>
    [DataMember]
    [JsonInclude]
    public string Description { get; private set; }

    #endregion

    /// <summary>
    /// Give the full principal name based on the domain and the sammaccountname.
    /// </summary>
    ///
    [JsonIgnore]
    public string Name
    {
        get { return string.Format(CultureInfo.InvariantCulture, @"{0}\{1}", Domain, SamAccountName); }
    }

    #region IXmlSerializable Members

    /// <exclude/>	
    public System.Xml.Schema.XmlSchema? GetSchema()
    {
        return null;
    }

    /// <exclude/>	
    public void ReadXml(XmlReader reader)
    {
        reader.ReadStartElement();

        DisplayName = ReadElement(reader, "displayName");
        Email = ReadElement(reader, "email");
        Department = ReadElement(reader, "department");
        Company = ReadElement(reader, "company");
        GivenName = ReadElement(reader, "givenName");
        SurName = ReadElement(reader, "surName");
        Sid = ReadElement(reader, "sid");
        State = ReadElement(reader, "state");
        Mobile = ReadElement(reader, "mobile");
        Telephone = ReadElement(reader, "telephone");
        InternalPhone = ReadElement(reader, "internalPhone");
        Fax = ReadElement(reader, "fax");
        PrincipalName = ReadElement(reader, "principalName");
        PostalCode = ReadElement(reader, "postalCode");
        Room = ReadElement(reader, "room");
        Initials = ReadElement(reader, "initials");
        SamAccountName = ReadElement(reader, "samAccountName");
        Domain = ReadElement(reader, "domain");
        Culture = new CultureInfo(ReadElement(reader, "culture"));
        CurrentCulture = new CultureInfo(ReadElement(reader, "currentCulture"));
        CommonName = ReadElement(reader, "commonName");
        Description = ReadElement(reader, "description");
        reader.ReadEndElement();
    }

    /// <exclude/>
    private static string ReadElement(XmlReader reader, string elementName)
    {
        return reader.ReadElementContentAsString(elementName, reader.NamespaceURI);
    }

    /// <exclude/>	
    public void WriteXml(XmlWriter writer)
    {
        WriteElement(writer, "displayName", DisplayName);
        WriteElement(writer, "email", Email);
        WriteElement(writer, "department", Department);
        WriteElement(writer, "company", Company);
        WriteElement(writer, "givenName", GivenName);
        WriteElement(writer, "surName", SurName);
        WriteElement(writer, "sid", Sid);
        WriteElement(writer, "state", State);
        WriteElement(writer, "mobile", Mobile);
        WriteElement(writer, "telephone", Telephone);
        WriteElement(writer, "internalPhone", InternalPhone);
        WriteElement(writer, "fax", Fax);
        WriteElement(writer, "principalName", PrincipalName);
        WriteElement(writer, "postalCode", PostalCode);
        WriteElement(writer, "room", Room);
        WriteElement(writer, "initials", Initials);
        WriteElement(writer, "samAccountName", SamAccountName);
        WriteElement(writer, "domain", Domain);
        WriteElement(writer, "culture", Culture.Name);
        WriteElement(writer, "currentCulture", CurrentCulture.Name);
        WriteElement(writer, "commonName", CommonName);
        WriteElement(writer, "description", Description);
    }

    /// <exclude/>	
    private static void WriteElement(XmlWriter writer, string elementName, string value)
    {
        writer.WriteStartElement(elementName);
        if (!string.IsNullOrWhiteSpace(value))
        {
            writer.WriteString(value);
        }

        writer.WriteEndElement();
    }

    #endregion
}
