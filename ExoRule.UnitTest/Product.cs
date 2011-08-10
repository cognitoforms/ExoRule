using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExoGraph.UnitTest;

namespace ExoRule.UnitTest
{
	internal class Product : TestEntity
	{
		public string Name
		{
			get { return Get(() => Name); }
			set { Set(() => Name, value); }
		}

		public string Brand
		{
			get { return Get(() => Brand); }
			set { Set(() => Brand, value); }
		}
	}
}
