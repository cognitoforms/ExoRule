using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExoRule
{
	/// <summary>
	/// Factory class supporting implicit conversion from a delegate returning rules into a <see cref="IRuleProvider"/> implementation.
	/// </summary>
	public class RuleProvider : IRuleProvider
	{
		Rule[] rules;
		readonly Func<IEnumerable<Rule>> provider;

		public RuleProvider(Func<IEnumerable<Rule>> provider)
		{
			this.provider = provider;
		}
			
		IEnumerable<Rule> IRuleProvider.GetRules(Type sourceType, string name)
		{
			if (rules == null)
				rules = provider().ToArray();

			return rules;
		}
	}
}
