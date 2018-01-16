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
	/// Applies conditions when the length of a list of a <see cref="ModelProperty"/> is
	/// too short or long.
	/// </summary>
	public class ListLengthRule : PropertyRule
	{
		#region Fields

		string minExpr;
		string maxExpr;
		ModelExpression minExpression;
		ModelExpression maxExpression;

		#endregion

		#region Constructors

		public ListLengthRule(string rootType, string property, int minimum, int maximum)
			: this(rootType, property, minimum, maximum, RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged)
		{ }

		public ListLengthRule(string rootType, string property, int minimum, int maximum, RuleInvocationType invocationTypes)
			: base(rootType, property, CreateError(property, minimum, maximum), invocationTypes, property)
		{
			this.Minimum = minimum;
			this.Maximum = maximum;
		}

		public ListLengthRule(string rootType, string property, int minimum, int maximum, string errorMessage)
			: this(rootType, property, minimum, maximum, new Error(GetErrorCode(rootType, property, "ListLength"), errorMessage, null), RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged)
		{ }

		public ListLengthRule(string rootType, string property, int minimum, int maximum, Error error, RuleInvocationType invocationTypes)
			: base(rootType, property, error, invocationTypes, property)
		{
			this.Minimum = minimum;
			this.Maximum = maximum;
		}

		public ListLengthRule(string rootType, string property, string minExpression, string maxExpression, string errorMessage)
			: base(rootType, property, new Error(GetErrorCode(rootType, property, "ListLength"), errorMessage, null), RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged, property)
		{
			this.minExpr = minExpression;
			this.maxExpr = maxExpression;

			Initialize += (s, e) => SetRange(property, MinExpression, MaxExpression);
		}

		public ListLengthRule(string rootType, string property, ModelExpression minimum, ModelExpression maximum, string errorMessage)
			: base(rootType, property, new Error(GetErrorCode(rootType, property, "ListLength"), errorMessage, null), RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged, property)
		{
			Initialize += (s, e) => SetRange(property, minimum, maximum);
		}

		#endregion

		#region Properties

		public int Minimum { get; private set; }

		public int Maximum { get; private set; }

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

		void SetRange(string property, ModelExpression minimum, ModelExpression maximum)
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

		static Func<ModelType, ConditionType> CreateError(string property, int minimum, int maximum)
		{
			string message;
			if (minimum > 0 && maximum > 0)
				message = "listlength-between";
			else if (minimum > 0)
				message = "listlength-at-least";
			else if (maximum > 0)
				message = "listlength-at-most";
			else
				throw new ArgumentException("Either the minimum or maximum values must be specified for a list length rule.");

			return (ModelType rootType) =>
			{
				var label = GetLabel(rootType, property);

				return new Error(GetErrorCode(rootType.Name, property, "ListLength"), message, typeof(ListLengthRule),
				(s) => s
					.Replace("{property}", label)
					.Replace("{min}", minimum.ToString())
					.Replace("{max}", maximum.ToString()), null);
			};
		}

		protected override bool ConditionApplies(ModelInstance root)
		{
			int value = root.GetList((ModelReferenceProperty)Property).Count;

			if (Minimum > 0 && Maximum > 0)
				return Minimum.CompareTo(value) > 0 && Maximum.CompareTo(value) < 0;
			else if (Minimum > 0)
				return Minimum.CompareTo(value) > 0;
			else if (Maximum > 0)
				return Maximum.CompareTo(value) < 0;
			else
				return false;
		}

		#endregion
	}
}