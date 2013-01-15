using System.ComponentModel.DataAnnotations;

namespace ExoRule.DataAnnotations
{
	public abstract class RegularExpressionReformatAttribute : RegularExpressionAttribute
	{
		protected RegularExpressionReformatAttribute(string pattern, string reformatPattern) : base(pattern)
		{
			ReformatExpression = reformatPattern;
		}

		public string ReformatExpression { get; set; }
	}
}
