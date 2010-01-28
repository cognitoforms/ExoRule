using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExoRule
{
	public class Warning : ConditionType
	{
		public Warning(string message)
			: this(null, message)
		{ }

		public Warning(string code, string message)
			: base(code, ConditionCategory.Warning, message)
		{ }

		public static implicit operator Warning(string message)
		{
			return new Warning(message);
		}
	}

	public class Warning<TRoot> : Warning
		where TRoot : class
	{
		public Warning(string message, Predicate<TRoot> condition)
			: this(null, message, condition)
		{ }

		public Warning(string code, string message, Predicate<TRoot> condition)
			: base(code, message)
		{
			CreateConditionRule<TRoot>(condition, null, null);
		}
	}
}
