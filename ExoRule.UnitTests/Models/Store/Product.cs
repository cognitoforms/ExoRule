using ExoModel.Json;

namespace ExoRule.UnitTests.Models.Store
{
	internal class Product : JsonEntity
	{
		public string Name { get; set; }

		public string Brand { get; set; }
	}
}
