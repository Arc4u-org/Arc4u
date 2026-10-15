using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;

namespace Arc4u.Data;

/// <summary>
/// Represents an <see cref="IdEntity&lt;TId&gt;"/> self-auditing its changes.
/// </summary>
/// <typeparam name="TId">The type of the <see cref="IdEntity&lt;TId&gt;.Id"/> property.</typeparam>
/// <typeparam name="TAuditedBy">The type of the <see cref="AuditEntity&lt;TId,TAuditedBy,TAuditedOn&gt;.AuditedBy"/> property.</typeparam>
/// <typeparam name="TAuditedOn">The type of the <see cref="AuditEntity&lt;TId,TAuditedBy,TAuditedOn&gt;.AuditedOn"/> property.</typeparam>
/// <remarks>Storage members are marked as protected in order to enable LinqToSql mappings.</remarks>

[DataContract(Name = "AuditEntityOf{0}")]
public abstract class AuditEntity<TId, TAuditedBy, TAuditedOn>
    : IdEntity<TId>
    , IAuditEntity<TAuditedBy, TAuditedOn>
{
    /// <summary>The name of the <see cref="AuditedBy"/> property, used when raising property change notifications.</summary>
    protected const string AuditedByPropertyName = "AuditedBy";
    /// <summary>The name of the <see cref="AuditedOn"/> property, used when raising property change notifications.</summary>
    protected const string AuditedOnPropertyName = "AuditedOn";

    #region IEntityAudit Members

    /// <summary>Backing field of the <see cref="AuditedBy"/> property.</summary>
    protected TAuditedBy _auditedBy = default!;

    /// <summary>
    /// Gets or sets the user who performed the change.
    /// </summary>
    /// <value>An user identification.</value>
    [DataMember(EmitDefaultValue = false)]
    public virtual TAuditedBy AuditedBy
    {
        get { return _auditedBy; }
        set
        {
            if (!object.Equals(_auditedBy, value))
            {
                _auditedBy = value;
                RaisePropertyChanged(AuditedByPropertyName);
            }
        }
    }

    /// <summary>Backing field of the <see cref="AuditedOn"/> property.</summary>
    protected TAuditedOn _auditedOn = default!;

    /// <summary>
    /// Gets or sets the date and time information when the change has been performed.
    /// </summary>
    /// <value>The date and time information.</value>
    [DataMember(EmitDefaultValue = false)]
    public virtual TAuditedOn AuditedOn
    {
        get { return _auditedOn; }
        set
        {
            if (!object.Equals(_auditedOn, value))
            {
                _auditedOn = value;
                RaisePropertyChanged(AuditedOnPropertyName);
            }
        }
    }
    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditEntity&lt;TId,TAuditedBy,TAuditedOn&gt;"/> class 
    /// <see cref="PersistChange"/> is defaulted to None.
    /// </summary>
    public AuditEntity() : this(PersistChange.None)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditEntity&lt;TId,TAuditedBy,TAuditedOn&gt;"/> class 
    /// with the specified <see cref="PersistChange"/>
    /// </summary>
    /// <param name="persistChange">The persist change.</param>
    protected AuditEntity(PersistChange persistChange)
        : base(persistChange)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditEntity&lt;TId,TAuditedBy,TAuditedOn&gt;"/> class
    /// copied from the specified <paramref name="entity"/>.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> is null.</exception>
    protected AuditEntity(AuditEntity<TId, TAuditedBy, TAuditedOn> entity)
        : base(entity)
    {
        _auditedBy = entity._auditedBy;
        _auditedOn = entity._auditedOn;
    }

    #endregion

    #region Overriden Members

    /// <summary>
    /// Gets or sets the persist change of the current <see cref="PersistEntity"/>.
    /// </summary>
    /// <value>The persist change.</value>
    /// <exception cref="ArgumentException">The transition is not allowed.</exception>
    public override PersistChange PersistChange
    {
        get { return _persistChange; }
        set
        {
            if (_persistChange != value)
            {
                // prevent invalid transitions except when serializing / deserializing
                if (!IgnoreOnPropertyChanged
                    && (_persistChange == PersistChange.Insert && value == PersistChange.Delete
                    || _persistChange == PersistChange.Delete && value == PersistChange.Insert))
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, InvalidTransition, _persistChange, value));
                }

                _persistChange = value;
                RaisePropertyChanged(PersistChangePropertyName);
            }
        }
    }

    /// <summary>
    /// Converts the value of the current <see cref="AuditEntity&lt;TId,TAuditedBy,TAuditedOn&gt;"/> object to its equivalent string representation. 
    /// </summary>
    /// <returns>A string representation of the value of the current <see cref="AuditEntity&lt;TId,TAuditedBy,TAuditedOn&gt;"/> object.</returns>
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}, AuditedBy = {1}, AuditedOn = {2}"
            , base.ToString()
            , AuditedBy
            , AuditedOn);
    }

    /// <summary>
    /// Determines whether if the specified <see cref="PropertyChangedEventArgs"/>
    /// contains a property touching the entity.
    /// </summary>
    /// <param name="e">A <see cref="PropertyChangedEventArgs"/> that contains a <see cref="PropertyChangedEventArgs.PropertyName"/>.</param>
    /// <returns>
    ///   <c>true</c> if the contained property name is touching the entity; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="e"/> is null.</exception>
    protected override bool IsTouchingProperty(PropertyChangedEventArgs e)
    {
        return base.IsTouchingProperty(e)
            && !string.Equals(e.PropertyName, AuditedOnPropertyName, StringComparison.Ordinal)
            && !string.Equals(e.PropertyName, AuditedByPropertyName, StringComparison.Ordinal);
    }

    #endregion

}
