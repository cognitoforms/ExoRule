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
	public class RequiredIfRule : PropertyRule
	{
		#region Fields

		ModelSource compareSource;
		string expression;

		#endregion

		#region Constructors

		public RequiredIfRule(string rootType, string property, string compareSource, CompareOperator compareOperator, object compareValue, params ConditionTypeSet[] sets)
			: this(rootType, property, compareSource, compareOperator, compareValue, RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged, sets)
		{ }

		public RequiredIfRule(string rootType, string property, string compareSource, CompareOperator compareOperator, object compareValue, Error error)
			: this(rootType, property, compareSource, compareOperator, compareValue, RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged, error)
		{ }

		public RequiredIfRule(string rootType, string property, string compareSource, CompareOperator compareOperator, object compareValue, RuleInvocationType invocationTypes, params ConditionTypeSet[] sets)
			: base(rootType, property,
				CreateError(property, compareSource, compareOperator, compareValue, sets), invocationTypes)
		{
			this.CompareSource = compareSource;
			this.CompareOperator = compareOperator;
			this.CompareValue = compareValue;
			InitializePredicates(compareSource);
		}

		public RequiredIfRule(string rootType, string property, string compareSource, CompareOperator compareOperator, object compareValue, RuleInvocationType invocationTypes, Error error)
			: base(rootType, property, error, invocationTypes)
		{
			this.CompareSource = compareSource;
			this.CompareOperator = compareOperator;
			this.CompareValue = compareValue;
			InitializePredicates(compareSource);
		}

		public RequiredIfRule(string rootType, string property, string expression, params ConditionTypeSet[] sets)
			: base(rootType, property, CreateError(property, sets:sets), RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged)
		{
			this.expression = expression;

			Initialize += (s, e) =>
			{
				if (RequiredExpression != null)
				{
					Path = RequiredExpression.Path.Path;
					SetPredicates(property, Path);
				}
			};
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the <see cref="ModelExpression"/> to handle the validation.
		/// </summary>
		public ModelExpression RequiredExpression
		{
			get
			{
				return expression != null ? RootType.GetExpression<bool>(expression.StartsWith("=") ? expression.Substring(1) : expression) : null;
			}
		}

		public string Path { get; private set; }

		/// <summary>
		/// Gets the path to the comparison property.
		/// </summary>
		public string CompareSource
		{
			get
			{
				return compareSource == null ? null : compareSource.Path;
			}
			private set
			{
				compareSource = new ModelSource(Property.DeclaringType, value);
			}
		}

		/// <summary>
		/// Indicates whether the compare source is static.
		/// </summary>
		public bool CompareSourceIsStatic
		{
			get
			{
				return compareSource.IsStatic;
			}
		}

		/// <summary>
		/// The value to compare to.
		/// </summary>
		public object CompareValue
		{
			get;
			private set;
		}

		/// <summary>
		/// The type of comparison to perform.
		/// </summary>
		public CompareOperator CompareOperator
		{
			get;
			private set;
		}

		#endregion

		#region Methods

		static Func<ModelType, ConditionType> CreateError(string property, string compareSource = null, CompareOperator compareOperator = CompareOperator.Equal, object compareValue = null, params ConditionTypeSet[] sets)
		{
			return (ModelType rootType) =>
			{
				// Determine the appropriate error message
				// If compareSource is null then a ModelExpression has been specified to handle the validation
				if (compareSource == null)
				{
					return new Error(GetErrorCode(rootType.Name, property, "RequiredIf"), "required", typeof(RequiredIfRule),
						(s) => s
							.Replace("{property}", GetLabel(rootType, property)), sets);
				}
				else
				{
					string message;
					if (compareValue == null)
						message = compareOperator == CompareOperator.Equal ? "required-if-not-exists" : "required-if-exists";
					else
					{
						bool isDate = compareValue is DateTime;
						switch (compareOperator)
						{
							case CompareOperator.Equal:
								message = "required-if-equal";
								break;
							case CompareOperator.NotEqual:
								message = "required-if-not-equal";
								break;
							case CompareOperator.GreaterThan:
								message = isDate ? "required-if-after" : "required-if-greater-than";
								break;
							case CompareOperator.GreaterThanEqual:
								message = isDate ? "required-if-on-or-after" : "required-if-greater-than-or-equal";
								break;
							case CompareOperator.LessThan:
								message = isDate ? "required-if-before" : "required-if-less-than";
								break;
							case CompareOperator.LessThanEqual:
								message = isDate ? "required-if-on-or-before" : "required-if-less-than-or-equal";
								break;
							default:
								throw new ArgumentException("Invalid comparison operator for required if rule");
						}
					}

					// Get the comparison source
					ModelSource source;
					ModelProperty sourceProperty;
					ModelSource.TryGetSource(rootType, compareSource, out source, out sourceProperty);

					// Create and return the error
					var compareValueFormatted = compareValue == null ? "" : ((ModelValueProperty)sourceProperty).FormatValue(compareValue);
					return new Error(GetErrorCode(rootType.Name, property, "RequiredIf"), message, typeof(RequiredIfRule),
						(s) => s
							.Replace("{property}", GetLabel(rootType, property))
							.Replace("{compareSource}", GetSourceLabel(rootType, compareSource))
							.Replace("{compareValue}", compareValueFormatted), sets);
				}
			};
		}

		protected override bool ConditionApplies(ModelInstance root)
		{
			// Exit immediately if the target property has a value
			if (root[Property] != null ||
				(Property is ModelReferenceProperty && Property.IsList && root.GetList((ModelReferenceProperty)Property).Count > 0))
				return false;

			// Invoke the ModelExpression if it exists
			if (RequiredExpression != null)
				return (bool)RequiredExpression.Invoke(root);
			// If the value to compare is null, then evaluate whether the compare source has a value
			else if (CompareValue == null)
				return CompareOperator == CompareOperator.Equal ? !compareSource.HasValue(root) : compareSource.HasValue(root);

			// Otherwise, perform a comparison of the compare source relative to the compare value
			bool? result = CompareRule.Compare(compareSource.GetValue(root), CompareOperator, CompareValue);
			return result.HasValue && result.Value;
		}

		#endregion
	}
}
