using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using ExoModel;
using ExoRule;

namespace ExoRule.Validation
{
	/// <summary>
	/// Applies conditions when the value of a <see cref="ModelProperty"/> is not within a specified range.
	/// </summary>
	public class RangeRule : PropertyRule
	{
		#region Constructors

		public RangeRule(string rootType, string property, IComparable minimum, IComparable maximum)
			: this(rootType, property, minimum, maximum, RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged)
		{ }

		public RangeRule(string rootType, string property, IComparable minimum, IComparable maximum, RuleInvocationType invocationTypes)
			: this(rootType, property, minimum, maximum, CreateError(rootType, property, minimum, maximum), invocationTypes)
		{}

		public RangeRule(string rootType, string property, IComparable minimum, IComparable maximum, string errorMessage)
			: this(rootType, property, minimum, maximum, new Error(GetErrorCode(rootType, property, "Range"), errorMessage, null), RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged)
		{ }

		public RangeRule(string rootType, string property, IComparable minimum, IComparable maximum, Error error, RuleInvocationType invocationTypes)
			: base(rootType, property, error, invocationTypes)
		{
			this.Minimum = minimum;
			this.Maximum = maximum;

			Initialize += (sender, args) =>
			{
				var propertyType = ((ModelValueProperty)this.Property).PropertyType;

				// If Nullable<T> check the underlying type
				if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
					propertyType = Nullable.GetUnderlyingType(propertyType);

				if (this.Minimum != null && this.Minimum is IConvertible)
					this.Minimum = (IComparable)Convert.ChangeType(this.Minimum, propertyType);

				if (this.Maximum != null && this.Maximum is IConvertible)
					this.Maximum = (IComparable)Convert.ChangeType(this.Maximum, propertyType);
			};
		}

		#endregion

		#region Properties

		public IComparable Minimum { get; private set; }

		public IComparable Maximum { get; private set; }

		#endregion

		#region Methods

		static Error CreateError(string rootType, string property, IComparable minimum, IComparable maximum)
		{
			bool isDate = minimum is DateTime || maximum is DateTime;
			
			string message;
			if (minimum != null && maximum != null)
				message = "range-between";
			else if (minimum != null)
				message = isDate ? "range-on-or-after" : "range-at-least";
			else if (maximum != null)
				message = isDate ? "range-on-or-before" : "range-at-most";
			else
				throw new ArgumentException("Either the minimum or maximum values must be specified for a range rule.");

			return new Error(
			GetErrorCode(rootType, property, "Range"), message, typeof(RangeRule),
			(s) => s
				.Replace("{property}", GetLabel(rootType, property))
				.Replace("{min}", minimum == null ? "" : Format(rootType, property, minimum))
				.Replace("{max}", maximum == null ? "" : Format(rootType, property, maximum)), null);
		}

		protected override bool ConditionApplies(ModelInstance root)
		{
			object value = root.Instance.GetType().GetProperty(Property.Name).GetValue(root.Instance, null);

			if (value == null || (value is double && double.IsNaN((double)value)))
				return false;

			// min <= value <= max
			// CompareTo = 0: equal, >0: instance > value
			if (Minimum != null && Maximum != null)
				return Minimum.CompareTo(value) > 0 && Maximum.CompareTo(value) < 0;
			else if (Minimum != null)
				return Minimum.CompareTo(value) > 0;
			else if (Maximum != null)
				return Maximum.CompareTo(value) < 0;
			else
				return false;
		}

		#endregion
	}
}
