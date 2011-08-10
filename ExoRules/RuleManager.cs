using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExoGraph;

namespace ExoRule
{
	/// <summary>
	/// Tracks rule state for each <see cref="GraphInstance"/>.
	/// </summary>
	internal class RuleManager
	{
		HashSet<Rule> pendingInvocation = new HashSet<Rule>();
		Dictionary<string, ConditionTarget> conditions = new Dictionary<string, ConditionTarget>();


		internal ConditionTarget GetCondition(ConditionType error)
		{
			ConditionTarget condition;
			conditions.TryGetValue(error.Code, out condition);
			return condition;
		}

		internal void SetCondition(ConditionTarget conditionTarget)
		{
			conditions[conditionTarget.Condition.Type.Code] = conditionTarget;
		}

		internal void ClearCondition(ConditionType error)
		{
			conditions.Remove(error.Code);
		}

		internal bool IsPendingInvocation(Rule rule)
		{
			return pendingInvocation.Contains(rule);
		}

		internal void SetPendingInvocation(Rule rule, bool isPending)
		{
			if (isPending)
				pendingInvocation.Add(rule);
			else
				pendingInvocation.Remove(rule);
		}

		internal IEnumerable<Condition> GetConditions()
		{
			return conditions.Values.Select(target => target.Condition).Distinct();
		}


		internal void RunPropertyGetRules(GraphInstance instance)
		{
			pendingInvocation.RemoveWhere(rule =>
				{
					if ((rule.InvocationTypes & RuleInvocationType.PropertyGet) > 0)
					{
						rule.Invoke(instance, null);
						return true;
					}
					return false;
				});
		}
	}
}
