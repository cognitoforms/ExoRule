using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExoRule
{
	public interface IPropertyRule
	{
		/// <summary>
		/// Gets the unique name of the rule within the scope of the property to which it is assigned.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the name of the property being calculated.
		/// </summary>
		string Property { get; }

		/// <summary>
		/// Gets the property paths the rule is dependent on.
		/// </summary>
		IEnumerable<string> Predicates { get; }

		/// <summary>
		/// Gets the condition type being asserted by the rule when the condition is met.
		/// </summary>
		ConditionType ConditionType { get; }
	}
}
