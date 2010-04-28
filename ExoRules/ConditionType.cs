using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExoGraph;

namespace ExoRule
{
	/// <summary>
	/// Represents a discrete error that could occur within an application,
	/// causing an <see cref="Condition"/> to be raised.
	/// </summary>
	[Serializable]
	public abstract class ConditionType : IRuleProvider
	{
		#region Fields

		static Dictionary<string, ConditionType> conditionTypes = new Dictionary<string, ConditionType>();
		public static readonly Error DuplicateCodeError = "An error has already been defined with the same error code.";
		public static readonly Error CodeChangeError = "The error code cannot be changed once it has been assigned to an error.";

		string code;
		ConditionCategory category;
		string message;
		Rule conditionRule;

		#endregion

		#region Constructors

		protected ConditionType(string message)
		{
			this.category = ConditionCategory.Error;
			this.message = message;
		}

		protected ConditionType(string code, ConditionCategory category, string message)
		{
			this.Code = code;
			this.category = category;
			this.message = message;
		}

		#endregion

		#region Properties

		public string Code
		{
			get
			{
				return code;
			}
			internal set
			{
				// The code cannot be changed once assigned
				if (code != null)
					throw (Exception)new Condition(CodeChangeError, null);

				// Ignore null codes
				if (value == null)
					return;

				// Set the code
				code = value;

				// Verify that the code has not already been assigned
				if (conditionTypes.ContainsKey(code))
					throw (Exception)new Condition(DuplicateCodeError, null);

				// Register the condition type based on its unique code
				conditionTypes.Add(code, this);
			}
		}

		public ConditionCategory Category
		{
			get
			{
				return category;
			}
		}

		public string Message
		{
			get
			{
				return message;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Allows subclasses to create an condition rule based on a concrete condition predicate.
		/// </summary>
		/// <typeparam name="TRoot"></typeparam>
		/// <param name="condition"></param>
		/// <param name="predicates"></param>
		/// <param name="properties"></param>
		protected void CreateConditionRule<TRoot>(Predicate<TRoot> condition, string[] predicates, string[] properties)
			where TRoot :  class
		{
			// Automatically calculate predicates if they were not specified
			if (predicates == null)
				predicates = PredicateBuilder.GetPredicates(condition.Method, method => Rule<TRoot>.PredicateFilter(condition.Method, method)).ToArray();

			// Automatically default the properties to be the same as the predicates if they are not specified.
			if (properties == null)
				properties = predicates;

			// Create an condition rule based on the specified condition
			this.conditionRule = new Rule<TRoot>(RuleInvocationType.PropertyChanged, predicates, root => When(root, () => condition(root), properties));
		}

		public override string ToString()
		{
			return Code + ": " + Message;
		}

		public override bool Equals(object obj)
		{
			return obj is ConditionType && ((ConditionType)obj).code == code;
		}

		public override int GetHashCode()
		{
			return code.GetHashCode();
		}

		public static bool operator ==(ConditionType e1, ConditionType e2)
		{
			if ((object)e1 == null)
				return (object)e2 == null;
			else
				return e1.Equals(e2);
		}

		public static bool operator !=(ConditionType e1, ConditionType e2)
		{
			return !(e1 == e2);
		}

		public static implicit operator ConditionType(string errorCode)
		{
			ConditionType error;
			conditionTypes.TryGetValue(errorCode, out error);
			return error;
		}

		/// <summary>
		/// Directly invokes the condition rule associated with the current condition type instance.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public void When(object target)
		{
			if (conditionRule == null)
				throw new NotSupportedException("The current condition type, " + Code + ", does not have an associated condition rule.");

			conditionRule.Invoke(GraphContext.Current.GetGraphType(target).GetGraphInstance(target), null);
		}

		/// <summary>
		/// Creates or removes an condition on the specified target based on the specified condition.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="condition"></param>
		/// <param name="properties"></param>
		public Condition When(object target, Func<bool> condition, params string[] properties)
		{
			return When(Message, target, condition, properties);
		}

		/// <summary>
		/// Creates or removes an condition on the specified target based on the specified condition.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="condition"></param>
		/// <param name="properties"></param>
		public Condition When(string message, object target, Func<bool> condition, params string[] properties)
		{
			// Convert the target into a rule root
			IRuleRoot root = GraphContext.Current.GetGraphType(target).GetGraphInstance(target).GetExtension<IRuleRoot>();

			// Get the current condition if it exists
			ConditionTarget conditionTarget = root.Manager.GetCondition(this);

			// Add the condition on the target if it does not exist yet
			if (condition())
			{
				// Create a new condition if one does not exist
				if (conditionTarget == null)
					return new Condition(this, message, target, properties);

				// Return the existing condition
				else
					return conditionTarget.Condition;
			}

			// Destroy the condition if it exists on the target and is no longer valid
			if (conditionTarget != null)
				conditionTarget.Condition.Destroy();

			// Return null to indicate that no condition was created
			return null;
		}

		#endregion

		#region IRuleProvider Members

		/// <summary>
		/// Returns the condition rule associated with the current condition type instance.
		/// </summary>
		/// <returns></returns>
		IEnumerable<Rule> IRuleProvider.GetRules(string name)
		{
			// Initialize the name of the rule if it has not already been set
			if (this.Code == null)
				this.Code = name;

			// Return the condition rule if defined
			if (conditionRule != null)
			{
				foreach (Rule rule in ((IRuleProvider)conditionRule).GetRules(name))
					yield return rule;
			}
		}

		#endregion
	}
}
