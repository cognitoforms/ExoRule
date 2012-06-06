using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace ExoRule
{
	public interface ICalculationRule : IPropertyRule
	{
		/// <summary>
		/// Gets the expression tree responsible for calculating the value of the property.
		/// </summary>
		LambdaExpression Calculation { get; }
	}
}
