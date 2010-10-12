using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace ExoRule
{
	[DataContract]
	public class Warning : ConditionType
	{
		public Warning(string message)
			: this(null, message)
		{ }

		public Warning(string code, string message, params ConditionTypeSet[] sets)
			: base(code, ConditionCategory.Warning, message, sets)
		{ }

		public Warning(string code, string message, Type sourceType, Func<string, string> translator, params ConditionTypeSet[] sets)
			: base(code, ConditionCategory.Warning, message, sourceType, translator, sets)
		{ }

		public static implicit operator Warning(string message)
		{
			return new Warning(message);
		}
	}

	public class Warning<TRoot> : Warning
		where TRoot : class
	{
		public Warning(string message, Predicate<TRoot> condition, params ConditionTypeSet[] sets)
			: this(null, message, condition)
		{ }

		public Warning(string code, string message, Predicate<TRoot> condition, params ConditionTypeSet[] sets)
			: base(code, message)
		{
			CreateConditionRule<TRoot>(condition, null, null);
		}
	}
}
