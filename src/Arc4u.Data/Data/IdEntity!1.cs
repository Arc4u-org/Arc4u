using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;

namespace Arc4u.Data;

/// <summary>
/// Represents a <see cref="PersistEntity"/> self-issuing its identification information.
/// </summary>
/// <typeparam name="TId">The type of the <see cref="Id"/> property.</typeparam>
/// <remarks>Storage members are marked as protected in order to enable LinqToSql mappings.</remarks>

[DataContract(Name = "IdEntityOf{0}")]
public abstract class IdEntity<TId>
    : PersistEntity
    , IIdEntity<TId>
    , IEquatable<IdEntity<TId>>
{
    /// <summary>The name of the <see cref="Id"/> property, used when raising property change notifications.</summary>
    protected const string IdPropertyName = "Id";

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="IdEntity&lt;TId&gt;"/> class 
    /// <see cref="PersistChange"/> is defaulted to None.
    /// </summary>
    public IdEntity() : this(PersistChange.None)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IdEntity&lt;TId&gt;"/> class 
    /// with the specified <see cref="PersistChange"/> 
    /// </summary>
    /// <param name="persistChange">The persist change.</param>
    protected IdEntity(PersistChange persistChange) : base(persistChange)
    {
        _id = default!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IdEntity&lt;TId&gt;"/> class
    /// copied from the specified <paramref name="entity"/>.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entity"/> is null.</exception>
    protected IdEntity(IdEntity<TId> entity) : base(entity)
    {
        _id = entity._id;
    }

    #endregion

    #region IIdEntity<TId> Members

    /// <summary>Backing field of the <see cref="Id"/> property.</summary>
    protected TId _id;

    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    /// <value>The entity Id.</value>
    [DataMember(EmitDefaultValue = false)]
    public virtual TId Id
    {
        get { return _id; }
        set
        {
            if (!object.Equals(_id, value))
            {
                _id = value;
                RaisePropertyChanged(IdPropertyName);
            }
        }
    }
    #endregion

    #region Overriden Members

    /// <summary>
    /// Converts the value of the current <see cref="IdEntity&lt;TId&gt;"/> object to its equivalent string representation. 
    /// </summary>
    /// <returns>A string representation of the value of the current <see cref="IdEntity&lt;TId&gt;"/> object.</returns>
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "Id = {0}, {1}"
                                                         , Id
                                                         , base.ToString());
    }

    /// <summary>
    /// Returns a hash code for this instance based on the <see cref="Id"/> property.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
    /// </returns>
    public override int GetHashCode()
    {
        return Id?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="Object"/>.
    /// </summary>
    /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="Object"/>.</param>
    /// <returns><c>true</c> if the specified <see cref="Object"/> is equal to the current <see cref="Object"/> ; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is IdEntity<TId> other && Equals(other);
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
            && !string.Equals(e.PropertyName, IdPropertyName, StringComparison.Ordinal);
    }

    #endregion

    #region IEquatable<IdEntity<TId>> Members

    /// <summary>
    /// Indicates whether the current entity <see cref="Id"/> is equal to another entity <see cref="Id"/>.
    /// </summary>
    /// <param name="other">An entity to compare with this entity.</param>
    /// <returns><c>true</c> if the current entity <see cref="Id"/> is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.</returns>
    public bool Equals(IdEntity<TId>? other)
    {
        return ReferenceEquals(this, other) ||
               (other != null
                && object.Equals(Id, other.Id));
    }

    #endregion

}
