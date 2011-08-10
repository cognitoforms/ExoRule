using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExoRule.UnitTest
{
	internal class Milk : Product
	{
		public decimal Percent
		{
			get { return Get(() => Percent); }
			set { Set(() => Percent, value); }
		}

		public decimal Gallons
		{
			get { return Get(() => Gallons); }
			set { Set(() => Gallons, value); }
		}

		public decimal Fat
		{
			get { return Get(() => Fat); }
			set { Set(() => Fat, value); }
		}

		/// <summary>
		/// Calculates the amount of fat represented by the current milk instance.
		/// </summary>
		static Rule CalculateFat = new Rule<Milk>(m => m.Fat = m.Gallons * m.Percent);
	}
}
