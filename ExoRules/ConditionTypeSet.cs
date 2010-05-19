using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace ExoRule
{

	/// <summary>
	/// A collection of condition types
	/// </summary>
	[Serializable]
	[DataContract]
	public class ConditionTypeSet
	{
		static Dictionary<string, ConditionTypeSet> sets = new Dictionary<string, ConditionTypeSet>();

		#region Constructors
		public ConditionTypeSet(string name)
		{
			sets.Add(name, this);
		}
		#endregion


		#region Properties
		/// <summary>
		/// The unique name of the set of conditions
		/// </summary>
		public string Name { get; private set; }
		#endregion
	}
}
