using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;

namespace Arc4u
{
    /// <summary>
    /// Represents a bound of an <see cref="Interval&lt;T&gt;"/>. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="T">The <see cref="Value"/> type of the bound.</typeparam>
    [DataContract(Name = "BoundOf{0}")]
    public sealed class Bound<T>
        : IEquatable<Bound<T>>
        , IComparable<Bound<T>>
    {
        #region Properties

        /// <summary>
        /// Gets the bound type.
        /// </summary>
        /// <remarks>The set operation is not private to let the Silverlight runtime
        /// accessing the property during serializing/deserializing operations.</remarks>
        [DataMember(EmitDefaultValue = false)]
        public BoundType Type { get; internal set; }

        /// <summary>
        /// Gets the bound direction.
        /// </summary>
        /// <remarks>The set operation is not private to let the Silverlight runtime
        /// accessing the property during serializing/deserializing operations.</remarks>
        [DataMember(EmitDefaultValue = false)]
        public BoundDirection Direction { get; internal set; }

        /// <summary>
        /// Gets the bound value.
        /// </summary>
        /// <remarks>The set operation is not private to let the Silverlight runtime
        /// accessing the property during serializing/deserializing operations.</remarks>
        [DataMember(EmitDefaultValue = false)]
        public T Value { get; internal set; }

        private static T _lowestValue;

        /// <summary>
        /// Gets the lowest value.
        /// </summary>
        /// <value>The lowest value.</value>
        private static T LowestValue
        {
            get { return _lowestValue; }
        }

        private static T _upmostValue;

        /// <summary>
        /// Gets the upmost value.
        /// </summary>
        /// <value>The upmost value.</value>
        private static T UpmostValue
        {
            get { return _upmostValue; }
        }

        /// <summary>
        /// Gets the lowest <see cref="Bound&lt;T&gt;"/>.
        /// </summary>
        /// <value>The lowest bound.</value>
        internal static Bound<T> LowestBound
        {
            get
            {
                return new Bound<T>(BoundType.Lower
                    , object.Equals(default(T), default)
                        ? BoundDirection.Opened
                        : BoundDirection.Closed
                    , Bound<T>.LowestValue);
            }
        }

        /// <summary>
        /// Gets the upmost <see cref="Bound&lt;T&gt;"/>.
        /// </summary>
        /// <value>The upmost bound.</value>
        internal static Bound<T> UpmostBound
        {
            get
            {
                return new Bound<T>(BoundType.Upper
                    , object.Equals(default(T), default)
                        ? BoundDirection.Opened
                        : BoundDirection.Closed
                    , Bound<T>.UpmostValue);
            }
        }

        #endregion

        #region Constructors

        static Bound()
        {
            InitializeBounds();
        }

        private Bound()
        {
            Value = default!;
        }

        internal Bound(BoundType type, BoundDirection direction, T value, bool checkArguments)
        {
            if (checkArguments && Bound.IsInfinity(value) && direction == BoundDirection.Closed)
            {
                throw new ArgumentException("An infinity bound must define an opened direction.");
            }

            Type = type;
            Direction = direction;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bound&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="type">The bound type.</param>
        /// <param name="direction">The bound direction.</param>
        /// <param name="value">The bound value.</param>
        /// <exception cref="ArgumentException">An infinity bound must define an <see cref="P:Direction.Opened"/> direction.</exception>
        public Bound(BoundType type, BoundDirection direction, T value)
            : this(type, direction, value, true)
        {
        }

        #endregion

        #region Overriden Members

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        /// <remarks>
        /// Type of <typeparamref name="T"/> is not considered for uniformity between types
        /// and because it could result to non displayable string like with <see cref="Char"/> type.
        /// </remarks>
        public override string ToString()
        {
            var format = (Type == BoundType.Lower)
                ? (Direction == BoundDirection.Closed)
                    ? "[{0}"
                    : "]{0}"
                : (Direction == BoundDirection.Closed)
                    ? "{0}]"
                    : "{0}[";
            // if is a nullable type, we need to check if the value is null
            // lowestValue will be null.
            var value = (Type == BoundType.Lower)
                ? object.Equals(Value, LowestValue)
                    ? Bound.Infinity
                    : Value?.ToString() ?? Bound.Infinity
                : object.Equals(Value, UpmostValue)
                    ? Bound.Infinity
                    : Value?.ToString() ?? Bound.Infinity;

            return string.Format(CultureInfo.InvariantCulture, format, value);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            int hash = 0x4043ed47;
            hash = (hash * -1521134295) + Type.GetHashCode();
            hash = (hash * -1521134295) + Direction.GetHashCode();
            hash = (hash * -1521134295) + (object.Equals(Value, default) ? 0 : Value.GetHashCode());
            return hash;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">An object to compare to this instance.</param>
        /// <returns>
        ///     <b>true</b> if this instance equals the <paramref name="obj"/>; otherwise, <b>false</b>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return (obj is Bound<T> bound) && Equals(bound);
        }

        #endregion

        #region IEquatable<Bound<T>> Members

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified <see cref="Bound&lt;T&gt;"/>.
        /// </summary>
        /// <param name="other">A <see cref="Bound&lt;T&gt;"/> to compare to this instance.</param>
        /// <returns>
        ///     <b>true</b> if this instance equals the <paramref name="other"/>; otherwise, <b>false</b>.
        /// </returns>
        public bool Equals(Bound<T> other)
        {
            return object.ReferenceEquals(this, other) ||
                (other is not null
                && object.Equals(Type, other.Type)
                && object.Equals(Direction, other.Direction)
                && object.Equals(Value, other.Value));
        }

        #endregion

        #region IComparable<Bound<T>> Members

        /// <summary>
        /// Compares this instance to a specified <see cref="Bound&lt;T&gt;"/> and returns an indication of their relative values.
        /// </summary>
        /// <param name="other">A <see cref="Bound&lt;T&gt;"/> to compare to this instance.</param>
        /// <returns>
        /// A signed number indicating the relative values of this instance and the <paramref name="other"/>.
        /// <list type="table">
        /// <listheader>
        ///     <term>Value Type</term>
        ///     <description>Condition</description>
        ///  </listheader>
        /// <item>
        ///     <term>Less than zero</term>
        ///     <description>This instance is less than the <paramref name="other"/>.</description>
        /// </item>
        /// <item>
        ///     <term>Zero</term>
        ///     <description>This instance is equal to the <paramref name="other"/>.</description>
        /// </item>
        /// <item>
        ///     <term>Greater than zero</term>
        ///     <description>This instance is greater than the <paramref name="other"/>.</description>
        /// </item>
        ///</list>
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is null.</exception>
        public int CompareTo(Bound<T>? other)
        {
#if NETSTANDARD2_0
            if (other is null)
            {
                throw new ArgumentNullException("other");
            }
#else
            ArgumentNullException.ThrowIfNull(other);
#endif
            //perform value comparaison
            var vCompare = ((Type == BoundType.Upper && Bound.IsInfinity(Value))
                            ||
                            (other.Type == BoundType.Upper && Bound.IsInfinity(other.Value)))
                ? Comparer<T>.Default.Compare(other.Value!, Value!)
                : Comparer<T>.Default.Compare(Value!, other.Value!);

            //consider value comparaison
            if (vCompare != 0)
            {
                return vCompare;
            }
            else
            {
                //perform type comparaison
                var tCompare = Type.CompareTo(other.Type);
                if (tCompare != 0)
                {
                    return tCompare;
                }
                else
                {
                    //perform direction comparaison
                    return (Type == BoundType.Upper)
                        ? Direction.CompareTo(other.Direction)
                        : other.Direction.CompareTo(Direction);
                }
            }
        }

        internal int UnionCompareTo(Bound<T> other)
        {
            //perform value comparaison
            var vCompare = ((Type == BoundType.Upper && Bound.IsInfinity(Value)) || (other.Type == BoundType.Upper && Bound.IsInfinity(other.Value)))
                ? Comparer<T>.Default.Compare(other.Value, Value)
                : Comparer<T>.Default.Compare(Value, other.Value);

            return (vCompare != 0)
                ? vCompare
                : (object.Equals(Direction, BoundDirection.Closed) || object.Equals(other.Direction, BoundDirection.Closed))
                    ? 0
                    : -1;
        }

#endregion

        #region Methods

        internal static void OverrideBounds(T lowestValue, T upmostValue)
        {
            _lowestValue = lowestValue;
            _upmostValue = upmostValue;
        }

        private static void InitializeBounds()
        {
            if (typeof(T).GetTypeInfo().IsEnum)
            {
                InitializeEnumBounds(out _lowestValue, out _upmostValue);
            }
            else
            {
                InitializeBounds(out _lowestValue, out _upmostValue);
            }
        }

        private static void InitializeBounds(out T lowestValue, out T upmostValue)
        {
            var type = typeof(T);

            // Determine if T is nullable and get underlying type
            var isNullable = Nullable.GetUnderlyingType(type) != null;
            // Check if T is a nullable type and get the underlying type
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            var fields = underlyingType.GetTypeInfo().DeclaredFields;
            var lowestField = (from f in fields
                               where string.Equals(f.Name, "NegativeInfinity", StringComparison.Ordinal)
                               || string.Equals(f.Name, "MinValue", StringComparison.Ordinal)
                               orderby f.Name descending
                               select f).FirstOrDefault();

            object? rawValue = lowestField is not null
                                ? lowestField.GetValue(null)
                                : default;

            lowestValue = (rawValue == null)
                            ? default!
                            : isNullable
                                ? (T)(object)rawValue
                                : (T)Convert.ChangeType(rawValue, underlyingType, CultureInfo.InvariantCulture);

            var upmostField = (from f in fields
                               where string.Equals(f.Name, "PositiveInfinity", StringComparison.Ordinal)
                               || string.Equals(f.Name, "MaxValue", StringComparison.Ordinal)
                               orderby f.Name descending
                               select f).FirstOrDefault();

            rawValue = upmostField is not null
                            ? upmostField.GetValue(null)
                            : default;

            upmostValue = (rawValue == null)
                            ? default!
                            : isNullable
                                ? (T)(object)rawValue
                                : (T)Convert.ChangeType(rawValue, underlyingType, CultureInfo.InvariantCulture);
        }

        private static void InitializeEnumBounds(out T lowestValue, out T upmostValue)
        {
            var values = Enum.GetValues(typeof(T));
            var hasFlag = (typeof(T).GetTypeInfo().GetCustomAttributes(typeof(FlagsAttribute), true).Length != 0);
            lowestValue = default!;
            upmostValue = default!;
            var sum = default(ulong);
            var firstValue = true;
            foreach (Enum value in values)
            {
                if (firstValue)
                {
                    lowestValue = (T)Enum.ToObject(typeof(T), value);
                    upmostValue = (T)Enum.ToObject(typeof(T), value);
                    firstValue = false;
                }
                else
                {
                    if (value.CompareTo(lowestValue) <= 0)
                    {
                        lowestValue = (T)Enum.ToObject(typeof(T), value);
                    }

                    if (value.CompareTo(upmostValue) >= 0)
                    {
                        upmostValue = (T)Enum.ToObject(typeof(T), value);
                    }
                }

                if (hasFlag)
                {
                    var ul = ulong.Parse(value.ToString("D"), CultureInfo.InvariantCulture);
                    if (IsPowerOfTwo(ul)) { sum += ul; }
                }
            }

            if (hasFlag)
            {
                upmostValue = (T)Enum.ToObject(typeof(T), sum);
            }
        }

        private static bool IsPowerOfTwo(ulong value)
        {
            return (value != 0) && ((value & (value - 1)) == 0);
        }

        internal bool Contains(T value)
        {
            return Type == BoundType.Lower
                ? Direction == BoundDirection.Closed
                    ? Comparer<T>.Default.Compare(Value, value) <= 0
                    : Comparer<T>.Default.Compare(Value, value) < 0
                : Direction == BoundDirection.Closed
                    ? Bound.IsInfinity(Value)
                        ? Comparer<T>.Default.Compare(value, Value) >= 0
                        : Comparer<T>.Default.Compare(Value, value) >= 0
                    : Bound.IsInfinity(Value)
                        ? Comparer<T>.Default.Compare(value, Value) > 0
                        : Comparer<T>.Default.Compare(Value, value) > 0;
        }

        #endregion

        #region Operators

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left <see cref="Bound&lt;T&gt;"/>.</param>
        /// <param name="right">The right <see cref="Bound&lt;T&gt;"/>.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(Bound<T> left, Bound<T> right)
        {
            return object.ReferenceEquals(left, right) || (!object.Equals(left, null) && !object.Equals(right, null) && left.Equals(right));
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left <see cref="Bound&lt;T&gt;"/>.</param>
        /// <param name="right">The right <see cref="Bound&lt;T&gt;"/>.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(Bound<T> left, Bound<T> right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Implements the operator &lt;.
        /// </summary>
        /// <param name="left">The left <see cref="Bound&lt;T&gt;"/>.</param>
        /// <param name="right">The right <see cref="Bound&lt;T&gt;"/>.</param>
        /// <returns>The result of the operator.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <c>null</c>.</exception>
        public static bool operator <(Bound<T> left, Bound<T> right)
        {
            return (left.CompareTo(right) < 0);

        }

        static void CheckBoundsNotNull(Bound<T> left, Bound<T> right)
        {
#if NETSTANDARD2_0
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }
#else
            ArgumentNullException.ThrowIfNull(left);
            ArgumentNullException.ThrowIfNull(right);
#endif
        }

        /// <summary>
        /// Implements the operator &lt;=.
        /// </summary>
        /// <param name="left">The left <see cref="Bound&lt;T&gt;"/>.</param>
        /// <param name="right">The right <see cref="Bound&lt;T&gt;"/>.</param>
        /// <returns>The result of the operator.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <c>null</c>.</exception>
        public static bool operator <=(Bound<T> left, Bound<T> right)
        {
            CheckBoundsNotNull(left, right);

            return (left.CompareTo(right) <= 0);
        }

        /// <summary>
        /// Implements the operator &gt;.
        /// </summary>
        /// <param name="left">The left <see cref="Bound&lt;T&gt;"/>.</param>
        /// <param name="right">The right <see cref="Bound&lt;T&gt;"/>.</param>
        /// <returns>The result of the operator.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <c>null</c>.</exception>
        public static bool operator >(Bound<T> left, Bound<T> right)
        {
            CheckBoundsNotNull(left, right);

            return (left.CompareTo(right) > 0);
        }

        /// <summary>
        /// Implements the operator &gt;=.
        /// </summary>
        /// <param name="left">The left <see cref="Bound&lt;T&gt;"/>.</param>
        /// <param name="right">The right <see cref="Bound&lt;T&gt;"/>.</param>
        /// <returns>The result of the operator.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <c>null</c>.</exception>
        public static bool operator >=(Bound<T> left, Bound<T> right)
        {
            CheckBoundsNotNull(left, right);

            return (left.CompareTo(right) >= 0);
        }

#endregion
    }
}
