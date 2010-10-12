using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExoRule
{
	/// <summary>
	/// Implemented by classes that expose one or more rules to support
	/// automatic discovery and registration.
	/// </summary>
	public interface IRuleProvider
	{
		IEnumerable<Rule> GetRules(Type sourceType, string name);
	}
}
