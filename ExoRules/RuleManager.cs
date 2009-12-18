using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExoRule
{
	/// <summary>
	/// Tracks rule state for each <see cref="GraphInstance"/>.
	/// </summary>
	public class RuleManager
	{
		Dictionary<Rule, Rule.RuleState> ruleStates = new Dictionary<Rule,Rule.RuleState>();

		internal Rule.RuleState GetState(Rule rule)
		{
			Rule.RuleState state;
			if (!ruleStates.TryGetValue(rule, out state))
				ruleStates[rule] = state = new Rule.RuleState();
			return state;
		}
	}
}
