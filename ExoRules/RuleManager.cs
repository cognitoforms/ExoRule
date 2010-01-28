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
		Dictionary<Rule, Rule.RuleState> ruleStates = new Dictionary<Rule, Rule.RuleState>();
		Dictionary<string, ConditionTarget> conditions = new Dictionary<string, ConditionTarget>();


		internal ConditionTarget GetCondition(ConditionType error)
		{
			return GetCondition(error, error.Message);
		}

		internal ConditionTarget GetCondition(ConditionType error, string message)
		{
			ConditionTarget condition;
			conditions.TryGetValue(error.Code + "|" + message, out condition);
			return condition;
		}

		internal void SetCondition(ConditionTarget conditionTarget)
		{
			conditions[conditionTarget.Condition.Type.Code + "|" + conditionTarget.Condition.Message] = conditionTarget;
		}

		internal void ClearCondition(ConditionType error)
		{
			ClearCondition(error, error.Message);
		}

		internal void ClearCondition(ConditionType error, string message)
		{
			conditions.Remove(error.Code + "|" + message);
		}

		internal Rule.RuleState GetState(Rule rule)
		{
			Rule.RuleState state;
			if (!ruleStates.TryGetValue(rule, out state))
				ruleStates[rule] = state = new Rule.RuleState();
			return state;
		}
	}
}
