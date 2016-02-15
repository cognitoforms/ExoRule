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
		#region Fields

		string minExpr;
		string maxExpr;
		ModelExpression minExpression;
		ModelExpression maxExpression;

		#endregion

		#region Constructors

		public RangeRule(string rootType, string property, IComparable minimum, IComparable maximum)
			: this(rootType, property, minimum, maximum, RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged)
		{ }

		public RangeRule(string rootType, string property, IComparable minimum, IComparable maximum, RuleInvocationType invocationTypes)
			: base(rootType, property, CreateError(property, minimum, maximum), invocationTypes, property)
		{
			SetRange(minimum, maximum);
		}

		public RangeRule(string rootType, string property, IComparable minimum, IComparable maximum, string errorMessage)
			: this(rootType, property, minimum, maximum, new Error(GetErrorCode(rootType, property, "Range"), errorMessage, null), RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged)
		{ }

		public RangeRule(string rootType, string property, IComparable minimum, IComparable maximum, Error error, RuleInvocationType invocationTypes)
			: base(rootType, property, error, invocationTypes, property)
		{
			SetRange(minimum, maximum);
		}

		public RangeRule(string rootType, string property, string minExpression, string maxExpression, string errorMessage)
			: base(rootType, property, new Error(GetErrorCode(rootType, property, "Range"), errorMessage, null), RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged, property)
		{
			this.minExpr = minExpression;
			this.maxExpr = maxExpression;

			Initialize += (s, e) => SetRange(MinExpression, MaxExpression);
		}

		public RangeRule(string rootType, string property, ModelExpression minimum, ModelExpression maximum, string errorMessage)
			: base(rootType, property, new Error(GetErrorCode(rootType, property, "Range"), errorMessage, null), RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged, property)
		{
			Initialize += (s, e) => SetRange(minimum, maximum);
		}

		#endregion

		#region Properties

		public IComparable Minimum { get; private set; }

		public IComparable Maximum { get; private set; }

		/// <summary>
		/// Gets the <see cref="ModelExpression"/> for the minimum valid value.
		/// </summary>
		public ModelExpression MinExpression
		{
			get
			{
				return minExpression ?? (minExpr != null ? RootType.GetExpression<bool>(minExpr) : null);
			}
		}

		/// <summary>
		/// Gets the <see cref="ModelExpression"/> for the maximum valid value.
		/// </summary>
		public ModelExpression MaxExpression
		{
			get
			{
				return maxExpression ?? (maxExpr != null ? RootType.GetExpression<bool>(maxExpr) : null);
			}
		}

		public string Path { get; private set; }

		#endregion

		#region Methods

		void SetRange(IComparable minimum, IComparable maximum)
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

		void SetRange(ModelExpression minimum, ModelExpression maximum)
		{
			this.minExpression = minimum;
			this.maxExpression = maximum;
			var minPath = minimum != null ? minimum.Path.Path : "";
			var maxPath = maximum != null ? maximum.Path.Path : "";
			if (!string.IsNullOrEmpty(minPath) || !string.IsNullOrEmpty(maxPath))
			{
				minPath = minPath.StartsWith("{") ? minPath.Substring(1, minPath.Length - 2) : minPath;
				maxPath = maxPath.StartsWith("{") ? maxPath.Substring(1, maxPath.Length - 2) : maxPath;
				Path = "{" + (minPath.Length > 0 && maxPath.Length > 0 ? minPath + "," + maxPath : minPath.Length > 0 ? minPath : maxPath) + "}";
				SetPredicates(Property.Name, Path);
			}
		}

		static Func<ModelType, ConditionType> CreateError(string property, IComparable minimum, IComparable maximum)
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

			return (ModelType rootType) => new Error(
				GetErrorCode(rootType.Name, property, "Range"), message, typeof(RangeRule),
				(s) => s
					.Replace("{property}", GetLabel(rootType, property))
					.Replace("{min}", minimum == null ? "" : Format(rootType, property, minimum))
					.Replace("{max}", maximum == null ? "" : Format(rootType, property, maximum)), null);
		}

		protected override bool ConditionApplies(ModelInstance root)
		{
			object value = root[Property];

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
