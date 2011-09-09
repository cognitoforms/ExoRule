using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExoRule
{
	/// <summary>
	/// Indicates where the rule is expected to execute in a distributed rule execution architecture.
	/// </summary>
	[Flags]
	public enum RuleExecutionLocation
	{
		Server = 1,
		Client = 2,
		ServerAndClient = 3
	}
}
