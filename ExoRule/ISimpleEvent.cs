namespace ExoRule
{
	#region ISimpleEvent<TSource>

	/// <summary>
	/// A simple event implementation
	/// </summary>
	/// <typeparam name="TSource">The type of the source object for the event</typeparam>
	public interface ISimpleEvent<TSource>
		where TSource : class
	{
		/// <summary>
		/// Executes event logic for the given source
		/// </summary>
		/// <param name="source">The source object for the event</param>
		void Execute(TSource source);
	}

	#endregion
}
