using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;

namespace Arc4u.Data;

/// <summary>
/// Represents a <see cref="CreateAuditEntity&lt;TId,TCreatedBy,TCreatedOn&gt;"/> self-auditing its creation and update information.
/// </summary>
/// <typeparam name="TId">The type of the <see cref="IdEntity&lt;TId&gt;.Id"/> property.</typeparam>
/// <typeparam name="TCreatedBy">The type of the <see cref="CreateAuditEntity&lt;TId,TCreatedBy,TCreatedOn&gt;.CreatedBy"/> property.</typeparam>
/// <typeparam name="TCreatedOn">The type of the <see cref="CreateAuditEntity&lt;TId,TCreatedBy,TCreatedOn&gt;.CreatedOn"/> property.</typeparam>
/// <typeparam name="TUpdatedBy">The type of the <see cref="UpdatedBy"/> property.</typeparam>
/// <typeparam name="TUpdatedOn">The type of the <see cref="UpdatedOn"/> property.</typeparam>
/// <remarks>Storage members are marked as protected in order to enable LinqToSql mappings.</remarks>
[DataContract(Name = "UpdateAuditEntityOf{0}")]
public abstract class UpdateAuditEntity<TId, TCreatedBy, TCreatedOn, TUpdatedBy, TUpdatedOn>
    : CreateAuditEntity<TId, TCreatedBy, TCreatedOn>
    , IUpdateAuditEntity<TCreatedBy, TCreatedOn, TUpdatedBy, TUpdatedOn>
{
    /// <summary>The name of the <see cref="UpdatedBy"/> property, used when raising property change notifications.</summary>
    protected const string UpdatedByPropertyName = "UpdatedBy";
    /// <summary>The name of the <see cref="UpdatedOn"/> property, used when raising property change notifications.</summary>
    protected const string UpdatedOnPropertyName = "UpdatedOn";

    /// <summary>
    /// Gets or sets a value indicating whether the "ChangingUpdatedBy" 
    /// and "ChangingUpdatedOn" events are raised 
    /// when creating new instances.
    /// </summary>
    /// <value>
    /// <b>true</b> if the "ChangingUpdatedBy" 
    /// and "ChangingUpdatedOn" events are raised 
    /// when creating new instances; otherwise, <b>false</b>.
    /// </value>
    /// <remarks>The "ChangeTracking" must also be "ChangeTracking.Enabled".</remarks>
    public static bool RaiseUpdateEventsOnCreation { get; set; }

    #region IEntityAudit Members

    /// <summary>Backing field of the <see cref="UpdatedBy"/> property.</summary>
    protected TUpdatedBy _updatedBy = default!;

    /// <summary>
    /// Gets or sets the last user who has updated the current entity.
    /// </summary>
    /// <value>The last updater.</value>
    [DataMember(EmitDefaultValue = false)]
    public virtual TUpdatedBy UpdatedBy
    {
        get { return _updatedBy; }
        set
        {
            if (!object.Equals(_updatedBy, value))
            {
                _updatedBy = value;
                RaisePropertyChanged(UpdatedByPropertyName);
            }
        }
    }

    /// <summary>Backing field of the <see cref="UpdatedOn"/> property.</summary>
    protected TUpdatedOn _updatedOn = default!;

    /// <summary>
    /// Gets or sets the last update date and time of the current entity.
    /// </summary>
    /// <value>The last update date and time.</value>
    [DataMember(EmitDefaultValue = false)]
    public virtual TUpdatedOn UpdatedOn
    {
        get { return _updatedOn; }
        set
        {
            if (!object.Equals(_updatedOn, value))
            {
                _updatedOn = value;
                RaisePropertyChanged(UpdatedOnPropertyName);
            }
        }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAuditEntity&lt;TId,TCreatedBy,TCreatedOn,TUpdatedBy,TUpdatedOn&gt;"/> class 
    /// <see cref="PersistChange"/> is defaulted to None.
    /// </summary>
    public UpdateAuditEntity() : this(PersistChange.None)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAuditEntity&lt;TId,TCreatedBy,TCreatedOn,TUpdatedBy,TUpdatedOn&gt;"/> class 
    /// with the specified <see cref="PersistChange"/> 
    /// </summary>
    /// <param name="persistChange">The persist change.</param>
    protected UpdateAuditEntity(PersistChange persistChange) : base(persistChange)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAuditEntity&lt;TId,TCreatedBy,TCreatedOn,TUpdatedBy,TUpdatedOn&gt;"/> class
    /// copied from the specified <paramref name="entity"/>.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> is null.</exception>
    protected UpdateAuditEntity(UpdateAuditEntity<TId, TCreatedBy, TCreatedOn, TUpdatedBy, TUpdatedOn> entity)
        : base(entity)
    {
        _updatedBy = entity._updatedBy;
        _updatedOn = entity._updatedOn;
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
            && !string.Equals(e.PropertyName, UpdatedOnPropertyName, StringComparison.Ordinal)
            && !string.Equals(e.PropertyName, UpdatedByPropertyName, StringComparison.Ordinal);
    }

    /// <summary>
    /// Converts the value of the current <see cref="UpdateAuditEntity&lt;TId,TCreatedBy,TCreatedOn,TUpdatedBy,TUpdatedOn&gt;"/> object to its equivalent string representation. 
    /// </summary>
    /// <returns>A string representation of the value of the current <see cref="UpdateAuditEntity&lt;TId,TCreatedBy,TCreatedOn,TUpdatedBy,TUpdatedOn&gt;"/> object.</returns>
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}, UpdatedBy = {1}, UpdatedOn = {2}"
            , base.ToString()
            , UpdatedBy
            , UpdatedOn);
    }

    #endregion

}
