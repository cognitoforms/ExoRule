using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace ExoRule
{
	public interface IConditionRule
	{
		/// <summary>
		/// Gets the expression tree responsible for calculating the value of the property.
		/// </summary>
		LambdaExpression Condition { get; }

		/// <summary>
		/// Gets the names of the properties being validated.
		/// </summary>
		IEnumerable<string> Properties { get; }

		/// <summary>
		/// Gets the property paths the condition is dependent on.
		/// </summary>
		IEnumerable<string> Predicates { get; }

		/// <summary>
		/// Gets the condition type being asserted by the rule when the condition is met.
		/// </summary>
		ConditionType ConditionType { get; }
	}
}
