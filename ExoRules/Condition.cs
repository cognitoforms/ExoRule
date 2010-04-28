using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections;
using ExoGraph;

namespace ExoRule
{
	#region Condition

	/// <summary>
	/// Represents an condition established based on a specific <see cref="ConditionType"/> instance.
	/// </summary>
	public class Condition
	{
		#region Fields

		List<ConditionTarget> targets = new List<ConditionTarget>();

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new <see cref="Condition"/> based on the specified <see cref="ConditionType"/>.
		/// </summary>
		/// <param name="error"></param>
		public Condition(ConditionType type, object target, params string[] properties)
			: this(type, type.Message, target, properties)
		{ }

		/// <summary>
		/// Creates a new <see cref="Condition"/> based on the specified <see cref="ConditionType"/>
		/// and error message.
		/// </summary>
		/// <param name="error"></param>
		public Condition(ConditionType type, string message, object target, params string[] properties)
		{
			this.Type = type;
			this.Message = message;
			this.AddTarget(target, properties);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the <see cref="Message"/> for the condition.
		/// </summary>
		public string Message { get; private set; }

		/// <summary>
		/// Gets the <see cref="ConditionType"/> the condition is for.
		/// </summary>
		public ConditionType Type { get; private set; }

		/// <summary>
		/// Gets the targets of the condition.
		/// </summary>
		public IEnumerable<ConditionTarget> Targets
		{
			get
			{
				return targets;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Adds an additional target to the condition.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="properties"></param>
		public void AddTarget(object target, params string[] properties)
		{
			// Get the root target instance
			GraphInstance root = GraphContext.Current.GetGraphType(target).GetGraphInstance(target);

			// Set the properties to an empty array if null
			if (properties == null)
				properties = new string[0];

			// Create a single condition target if the specified proeprties are all on the root
			if (properties.Length == 0 || properties.FirstOrDefault(property => property.Contains('.')) == null)
				targets.Add(new ConditionTarget(this, root, properties));

			// Otherwise, process the property paths to create the necessary sources
			else
			{
				// Create a dictionary of condition sources
				Dictionary<string, List<ConditionTarget>> paths = new Dictionary<string, List<ConditionTarget>>();
				
				// Process each property path to build up the condition sources
				foreach (string property in properties)
				{
					string[] steps = property.Split('.');
					string leafProperty = steps[steps.Length - 1];
					string sourcePath = steps.Length < 2 ? "" : property.Substring(0, property.Length - leafProperty.Length - 1);
					
					// Create the list of condition sources for the current path
					List<ConditionTarget> pathTargets;
					if (!paths.TryGetValue(sourcePath, out pathTargets))
						paths[sourcePath] = pathTargets =
							GetInstances(steps, 0, new GraphInstance[] { root })
							.Select(instance => 
							{
								ConditionTarget conditionTarget = new ConditionTarget(this, instance, leafProperty);
								targets.Add(conditionTarget);
								return conditionTarget;
							})
							.ToList();

					// Otherwise, add the leaf property to the existing condition sources for the current path
					else
						foreach (ConditionTarget pathTarget in pathTargets)
							pathTarget.AddProperty(leafProperty);
				}
			}
		}

		/// <summary>
		/// Recursively loads the instances at the end of a property path.
		/// </summary>
		/// <param name="steps"></param>
		/// <param name="step"></param>
		/// <param name="parents"></param>
		/// <returns></returns>
		IEnumerable<GraphInstance> GetInstances(string[] steps, int step, IEnumerable<GraphInstance> parents)
		{
			// If there is only one step, just return the root that was passed in
			if (steps.Length == 1)
				yield return parents.First();

			// Otherwise, recursively process the path to return the leaf instances
			else
			{
				// Process each parent instance
				foreach (GraphInstance parent in parents)
				{
					GraphReferenceProperty property = parent.Type.Properties[steps[step]] as GraphReferenceProperty;
					if (property == null)
						continue;

					// Get the list of child instances for the current step
					IEnumerable<GraphInstance> children = null;
					if (property.IsList)
						children = parent.GetList(property);
					else
					{
						GraphInstance child = parent.GetReference((GraphReferenceProperty)property);
						children = child == null ? new GraphInstance[] { } : new GraphInstance[] { child };
					}

					// Either recurse to the end of the path or return the requested instances
					if (step == steps.Length - 2)
						foreach (GraphInstance child in children)
							yield return child;
					else
						foreach (GraphInstance child in GetInstances(steps, step + 1, children))
							yield return child;
				}
			}
		}

		public static IEnumerable<Condition> GetConditions(GraphInstance instance)
		{
			return instance.GetExtension<IRuleRoot>().Manager.GetConditions();
		}

		/// <summary>
		/// Destroys the current exception.
		/// </summary>
		internal void Destroy()
		{
			foreach (ConditionTarget conditionTarget in targets)
				conditionTarget.Target.GetExtension<IRuleRoot>().Manager.ClearCondition(conditionTarget.Condition.Type);
			targets.Clear();
		}


		/// <summary>
		/// Allows conditions to be thrown as if they were exceptions.
		/// </summary>
		/// <param name="condition"></param>
		/// <returns></returns>
		public static implicit operator Exception(Condition condition)
		{
			return new ApplicationException(condition.Message);
		}

		#endregion
	}

	#endregion
}
