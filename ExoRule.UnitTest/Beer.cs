using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExoRule.UnitTest
{
	internal class Beer : Product
	{
		public decimal Gallons
		{
			get { return Get(() => Gallons); }
			set { Set(() => Gallons, value); }
		}
	}
}
