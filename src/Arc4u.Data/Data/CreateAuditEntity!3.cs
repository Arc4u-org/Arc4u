using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;

namespace Arc4u.Data;

/// <summary>
/// Represents an <see cref="IdEntity&lt;TId&gt;"/> self-auditing its creation information.
/// </summary>
/// <typeparam name="TId">The type of the <see cref="IdEntity&lt;TId&gt;.Id"/> property.</typeparam>
/// <typeparam name="TCreatedBy">The type of the <see cref="CreatedBy"/> property.</typeparam>
/// <typeparam name="TCreatedOn">The type of the <see cref="CreatedOn"/> property.</typeparam>
/// <remarks>Storage members are marked as protected in order to enable LinqToSql mappings.</remarks>
[DataContract(Name = "CreateAuditEntityOf{0}")]
public abstract class CreateAuditEntity<TId, TCreatedBy, TCreatedOn>
    : IdEntity<TId>
    , ICreateAuditEntity<TCreatedBy, TCreatedOn>
{
    /// <summary>The name of the <see cref="CreatedBy"/> property, used when raising property change notifications.</summary>
    protected const string CreatedByPropertyName = "CreatedBy";
    /// <summary>The name of the <see cref="CreatedOn"/> property, used when raising property change notifications.</summary>
    protected const string CreatedOnPropertyName = "CreatedOn";

    #region ICreateEntity Members

    /// <summary>Backing field of the <see cref="CreatedBy"/> property.</summary>
    protected TCreatedBy _createdBy = default!;

    /// <summary>
    /// Gets or sets the user who has created the current entity.
    /// </summary>
    /// <value>The creator.</value>
    [DataMember(EmitDefaultValue = false)]
    public virtual TCreatedBy CreatedBy
    {
        get { return _createdBy; }
        set
        {
            if (!object.Equals(_createdBy, value))
            {
                _createdBy = value;
                RaisePropertyChanged(CreatedByPropertyName);
            }
        }
    }

    /// <summary>Backing field of the <see cref="CreatedOn"/> property.</summary>
    protected TCreatedOn _createdOn = default!;

    /// <summary>
    /// Gets or sets the creation date and time of the current entity.
    /// </summary>
    /// <value>The creation date and time.</value>
    [DataMember(EmitDefaultValue = false)]
    public virtual TCreatedOn CreatedOn
    {
        get { return _createdOn; }
        set
        {
            if (!object.Equals(_createdOn, value))
            {
                _createdOn = value;
                RaisePropertyChanged(CreatedOnPropertyName);
            }
        }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAuditEntity&lt;TId,TCreatedBy,TCreatedOn&gt;"/> class
    /// <see cref="PersistChange"/> is defaulted to None.
    /// </summary>
    public CreateAuditEntity() : this(PersistChange.None)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAuditEntity&lt;TId,TCreatedBy,TCreatedOn&gt;"/> class 
    /// with the specified <see cref="PersistChange"/> 
    /// </summary>
    /// <param name="persistChange">The persist change.</param>
    public CreateAuditEntity(PersistChange persistChange) : base(persistChange)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAuditEntity&lt;TId,TCreatedBy,TCreatedOn&gt;"/> class
    /// copied from the specified <paramref name="entity"/>.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> is null.</exception>
    protected CreateAuditEntity(CreateAuditEntity<TId, TCreatedBy, TCreatedOn> entity)
        : base(entity)
    {
        _createdBy = entity._createdBy;
        _createdOn = entity._createdOn;
    }

    #endregion

    #region Overriden Members

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
            && !string.Equals(e.PropertyName, CreatedOnPropertyName, StringComparison.Ordinal)
            && !string.Equals(e.PropertyName, CreatedByPropertyName, StringComparison.Ordinal);
    }

    /// <summary>
    /// Converts the value of the current <see cref="CreateAuditEntity&lt;TId,TCreatedBy,TCreatedOn&gt;"/> object to its equivalent string representation. 
    /// </summary>
    /// <returns>A string representation of the value of the current <see cref="CreateAuditEntity&lt;TId,TCreatedBy,TCreatedOn&gt;"/> object.</returns>
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}, CreatedBy = {1}, CreatedOn = {2}"
            , base.ToString()
            , CreatedBy
            , CreatedOn);
    }

    #endregion

}
