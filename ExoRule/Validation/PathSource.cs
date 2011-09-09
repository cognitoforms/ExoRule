using System;
using System.Linq;
using ExoGraph;

namespace ExoRule.Validation
{
	/// <summary>
	/// Utility class that supports accessing the value of a property along a static or instance source path.
	/// </summary>
	public class PathSource
	{
		GraphType sourceType;
		string[] sourcePath;
		GraphPath instancePath;

		/// <summary>
		/// Creates a new <see cref="PathSource"/> for the specified root type and path.
		/// </summary>
		/// <param name="rootType">The root <see cref="GraphType"/>, which is required for instance paths</param>
		/// <param name="path">The source path, which is either and instance path of a static path</param>
		public PathSource(GraphType rootType, string path)
		{
			// Store the source path
			this.Path = path;

			// Instance Path
			if (rootType != null && rootType.TryGetPath(path, out instancePath))
			{
				this.IsStatic = false;

				this.sourceType = rootType;
				this.sourcePath = path.Split('.');
				this.sourcePath = sourcePath.Take(sourcePath.Length - 1).ToArray();
				
				// Get the last property along the path
				GraphStep step = instancePath.FirstSteps.First();
				while (step.NextSteps.Any())
					step = step.NextSteps.First();
				this.SourceProperty = step.Property;
			}

			// Static Path
			else if (path.Contains('.'))
			{
				this.IsStatic = true;

				this.sourceType = GraphContext.Current.GetGraphType(path.Substring(0, path.LastIndexOf('.')));
				if (sourceType != null)
					this.SourceProperty = sourceType.Properties[path.Substring(path.LastIndexOf('.') + 1)];
			}

			// Raise an error if the specified path is not valid
			if (SourceProperty == null)
				throw new ArgumentException("The specified path, '" + path + "', was not valid for the root type of '" + rootType.Name + "'.");
		}

		/// <summary>
		/// Gets the source path represented by the current instance.
		/// </summary>
		public string Path
		{
			get;
			private set;
		}

		/// <summary>
		/// Indicates whether the source represents a static property.
		/// </summary>
		public bool IsStatic
		{
			get;
			private set;
		}

		public GraphProperty SourceProperty
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the underlying value of the property for the current source path.
		/// </summary>
		/// <param name="root"></param>
		/// <returns></returns>
		public object GetValue(GraphInstance root)
		{
			IGraphPropertySource source = GetSource(root);
			return source == null ? null : source[SourceProperty.Name];
		}

		/// <summary>
		/// Determines whether the value of the property along the source path has a value or not.
		/// </summary>
		/// <param name="root"></param>
		/// <returns>True if the source path has an assigned value, otherwise false.</returns>
		/// <remarks>
		/// If any value along the source path is null, false will be returned. 
		/// If the source property is a list, false will be returned if the list is empty.
		/// </remarks>
		public bool HasValue(GraphInstance root)
		{
			// Get the source
			IGraphPropertySource source = GetSource(root);

			// Return false if the source is null
			if (source == null)
				return false;

			// Get the property off of the source to evaluate
			GraphProperty property = source.Properties[SourceProperty.Name];

			// If the property is a list, determine if the list has items
			if (property is GraphReferenceProperty && property.IsList)
				return source.GetList((GraphReferenceProperty)property).Count > 0;

			// Otherwise, just determine if the property has an assigned value
			else
				return source[property] != null;
		}

		public IGraphPropertySource GetSource(GraphInstance root)
		{
			// Return the source type for static paths
			if (IsStatic)
				return sourceType;

			// Otherwise, walk the source path to find the source instance
			foreach (string step in sourcePath)
			{
				if (root == null)
					return null;
				root = root.GetReference(step);
			}

			// Return the source instance
			return root;
		}

		/// <summary>
		/// Gets the <see cref="GraphInstanceList"/> defined by specified source path.
		/// </summary>
		/// <param name="root"></param>
		/// <returns></returns>
		public GraphInstanceList GetList(GraphInstance root)
		{
			IGraphPropertySource source = GetSource(root);
			return source == null ? null : source.GetList(SourceProperty.Name);
		}
	}
}
