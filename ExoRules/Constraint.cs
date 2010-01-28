using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExoRule
{
	public abstract class Constraint : ConditionType
	{
		internal Constraint(string code, ConditionCategory defaultCategory, string defaultMessage)
			: base(code, defaultCategory, defaultMessage)
		{ }
	}

	public class Constraint<TRoot> : Constraint
		where TRoot : class
	{
		Predicate<TRoot> condition;
		Rule<TRoot> rule;

		public Constraint(string defaultMessage, Predicate<TRoot> condition, params string[] properties)
			: this(null, ConditionCategory.Error, defaultMessage, condition, properties)
		{ }
	
		public Constraint(string code, ConditionCategory defaultCategory, string defaultMessage, Predicate<TRoot> condition, params string[] properties)
			: base(code, defaultCategory, defaultMessage)
		{
			string[] predicates = PredicateBuilder.GetPredicates(condition.Method, method => Rule<TRoot>.PredicateFilter(condition.Method, method)).ToArray();

			this.condition = condition;
			this.rule = new Rule<TRoot>(RuleInvocationType.PropertyChanged, predicates, root => When(root, () => condition(root), properties));
		}
	}
}
