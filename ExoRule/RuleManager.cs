using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExoModel;

namespace ExoRule
{
	/// <summary>
	/// Tracks rule state for each <see cref="ModelInstance"/>.
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

		internal bool SetPendingInvocation(Rule rule, bool isPending)
		{
			if (isPending)
				return pendingInvocation.Add(rule);
			else
				return pendingInvocation.Remove(rule);
		}

		internal IEnumerable<Condition> GetConditions()
		{
			return conditions.Values.Select(target => target.Condition);
		}

		internal IEnumerable<Condition> GetConditions(Func<ConditionTarget, bool> filter)
		{
			return conditions.Values.Where(filter).Select(target => target.Condition);
		}

		internal void RunPendingPropertyGetRules(ModelInstance instance, Func<ModelProperty, bool> when)
		{
			// First run all rules for return values associated with properties that have not yet been accessed
			foreach (Rule rule in instance.GetRules()
					.Where(rule => (rule.InvocationTypes & RuleInvocationType.PropertyGet) > 0 && rule.ReturnValues.Select(p => rule.RootType.Properties[p])
					.Any(p => when(p) && !instance.HasBeenAccessed(p))))
				rule.Invoke(instance, null);

			// Then run any property get rules that are pending invocation due to changes in the model
			pendingInvocation.RemoveWhere(rule =>
				{
					if ((rule.InvocationTypes & RuleInvocationType.PropertyGet) > 0 && rule.ReturnValues.Select(p => rule.RootType.Properties[p]).Any(when))
					{
						rule.Invoke(instance, null);
						return true;
					}
					return false;
				});
		}
	}
}
