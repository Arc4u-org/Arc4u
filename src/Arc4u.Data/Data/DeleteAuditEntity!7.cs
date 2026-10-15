using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;

namespace Arc4u.Data;

/// <summary>
/// Represents an <see cref="UpdateAuditEntity&lt;TId,TCreatedBy,TCreatedOn,TUpdatedBy,TUpdatedOn&gt;"/> self-auditing its creation, update and delete information.
/// </summary>
/// <typeparam name="TId">The type of the <see cref="IdEntity&lt;TId&gt;.Id"/> property.</typeparam>
/// <typeparam name="TCreatedBy">The type of the <see cref="CreateAuditEntity&lt;TId,TCreatedBy,TCreatedOn&gt;.CreatedBy"/> property.</typeparam>
/// <typeparam name="TCreatedOn">The type of the <see cref="CreateAuditEntity&lt;TId,TCreatedBy,TCreatedOn&gt;.CreatedOn"/> property.</typeparam>
/// <typeparam name="TUpdatedBy">The type of the <see cref="UpdateAuditEntity&lt;TId,TCreatedBy,TCreatedOn,TUpdatedBy,TUpdatedOn&gt;.UpdatedBy"/> property.</typeparam>
/// <typeparam name="TUpdatedOn">The type of the <see cref="UpdateAuditEntity&lt;TId,TCreatedBy,TCreatedOn,TUpdatedBy,TUpdatedOn&gt;.UpdatedOn"/> property.</typeparam>
/// <typeparam name="TDeletedBy">The type of the <see cref="DeletedBy"/> property.</typeparam>
/// <typeparam name="TDeletedOn">The type of the <see cref="DeletedOn"/> property.</typeparam>
/// <remarks>Storage members are marked as protected in order to enable LinqToSql mappings.</remarks>

[DataContract(Name = "DeleteAuditEntityOf{0}")]
public abstract class DeleteAuditEntity<TId, TCreatedBy, TCreatedOn, TUpdatedBy, TUpdatedOn, TDeletedBy, TDeletedOn>
    : UpdateAuditEntity<TId, TCreatedBy, TCreatedOn, TUpdatedBy, TUpdatedOn>
    , IDeleteAuditEntity<TCreatedBy, TCreatedOn, TUpdatedBy, TUpdatedOn, TDeletedBy, TDeletedOn>
{
    /// <summary>The name of the <see cref="DeletedBy"/> property, used when raising property change notifications.</summary>
    protected const string DeletedByPropertyName = "DeletedBy";
    /// <summary>The name of the <see cref="DeletedOn"/> property, used when raising property change notifications.</summary>
    protected const string DeletedOnPropertyName = "DeletedOn";

    #region IEntityAudit Members

    /// <summary>Backing field of the <see cref="DeletedBy"/> property.</summary>
    protected TDeletedBy _deletedBy = default!;

    /// <summary>
    /// Gets or sets the user who has deleted the current entity.
    /// </summary>
    /// <value>The delete user.</value>
    [DataMember(EmitDefaultValue = false)]
    public virtual TDeletedBy DeletedBy
    {
        get { return _deletedBy; }
        set
        {
            if (!object.Equals(_deletedBy, value))
            {
                _deletedBy = value;
                RaisePropertyChanged(DeletedByPropertyName);
            }
        }
    }

    /// <summary>Backing field of the <see cref="DeletedOn"/> property.</summary>
    protected TDeletedOn _deletedOn = default!;

    /// <summary>
    /// Gets or sets the delete date and time of the current entity.
    /// </summary>
    /// <value>The delete date and time.</value>
    [DataMember(EmitDefaultValue = false)]
    public virtual TDeletedOn DeletedOn
    {
        get { return _deletedOn; }
        set
        {
            if (!object.Equals(_deletedOn, value))
            {
                _deletedOn = value;
                RaisePropertyChanged(DeletedOnPropertyName);
            }
        }
    }
    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteAuditEntity&lt;TId,TCreatedBy,TCreatedOn,TUpdatedBy,TUpdatedOn,TDeletedBy,TDeletedOn&gt;"/> class 
    /// or with an enabled "ChangeTracking".
    /// </summary>
    public DeleteAuditEntity() : this(PersistChange.None)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteAuditEntity&lt;TId,TCreatedBy,TCreatedOn,TUpdatedBy,TUpdatedOn,TDeletedBy,TDeletedOn&gt;"/> class 
    /// with the specified <see cref="PersistChange"/> 
    /// </summary>
    /// <param name="persistChange">The persist change.</param>
    protected DeleteAuditEntity(PersistChange persistChange) : base(persistChange)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteAuditEntity&lt;TId,TCreatedBy,TCreatedOn,TUpdatedBy,TUpdatedOn,TDeletedBy,TDeletedOn&gt;"/> class
    /// copied from the specified <paramref name="entity"/>.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> is null.</exception>
    protected DeleteAuditEntity(DeleteAuditEntity<TId, TCreatedBy, TCreatedOn, TUpdatedBy, TUpdatedOn, TDeletedBy, TDeletedOn> entity)
        : base(entity)
    {
        _deletedBy = entity._deletedBy;
        _deletedOn = entity._deletedOn;
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
                if (_persistChange == PersistChange.Insert && value == PersistChange.Delete ||
                    _persistChange == PersistChange.Delete && value == PersistChange.Insert)
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, InvalidTransition, _persistChange, value));
                }

                _persistChange = value;
                RaisePropertyChanged(PersistChangePropertyName);
            }
        }
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
            && !string.Equals(e.PropertyName, DeletedOnPropertyName, StringComparison.Ordinal)
            && !string.Equals(e.PropertyName, DeletedByPropertyName, StringComparison.Ordinal);
    }

    /// <summary>
    /// Converts the value of the current <see cref="DeleteAuditEntity&lt;TId,TCreatedBy,TCreatedOn,TUpdatedBy,TUpdatedOn,TDeletedBy,TDeletedOn&gt;"/> object to its equivalent string representation. 
    /// </summary>
    /// <returns>A string representation of the value of the current <see cref="DeleteAuditEntity&lt;TId,TCreatedBy,TCreatedOn,TUpdatedBy,TUpdatedOn,TDeletedBy,TDeletedOn&gt;"/> object.</returns>
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}, DeletedBy = {1}, DeletedOn = {2}"
            , base.ToString()
            , DeletedBy
            , DeletedOn);
    }

    #endregion

}
