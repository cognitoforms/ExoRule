using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExoRule
{
	/// <summary>
	/// Base class for types that provide a filtered view of conditions.
	/// </summary>
	public abstract class ConditionFilter
	{
		/*
		RuleExceptionList exceptions;
		bool hasPendingChanges;

		/// <summary>
		/// Creates a new filter and subscribes to changes to the context filter.
		/// </summary>
		/// <remarks>
		/// Subclasses must call <see cref="Reset"/> in order to build the initial filter.
		/// </remarks>
		protected RuleExceptionFilter()
		{ }

		/// <summary>
		/// Gets the list of exceptions exposed by this filter.
		/// </summary>
		public RuleExceptionList Exceptions
		{
			get
			{
				if (exceptions == null)
					Reset();
				return exceptions;
			}
		}

		/// <summary>
		/// Notifies filter subscribers that the filtered exceptions have changed.
		/// </summary>
		public event ExceptionFilterChangedEventHandler Changed;

		/// <summary>
		/// Processes changes to the context list of exceptions to determine if the exception should be included in the filter.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Exceptions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			bool changed = false;

			// Process added exceptions
			if (e.NewItems != null)
			{
				foreach (Condition added in e.NewItems)
				{
					added.JoinPoints.CollectionChanged += new NotifyCollectionChangedEventHandler(JoinPoints_CollectionChanged);
					if (IncludeInFilter(added))
					{
						exceptions.Add(added);
						changed = true;
					}
				}
			}

			// Process removed exception
			if (e.OldItems != null)
			{
				foreach (Condition removed in e.OldItems)
				{
					removed.JoinPoints.CollectionChanged -= new NotifyCollectionChangedEventHandler(JoinPoints_CollectionChanged);
					if (exceptions.Contains(removed))
					{
						exceptions.Remove(removed);
						changed = true;
					}
				}
			}

			// Notify subscribers that the filter has changed.
			if (changed)
				OnChanged();
		}

		/// <summary>
		/// Evaluates whether an exception should be included in the filter due to changes to join points.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void JoinPoints_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			Condition exception = null;
			if (e.NewItems != null && e.NewItems.Count > 0)
				exception = ((RuleExceptionJoinPoint)e.NewItems[0]).Exception;
			if (e.OldItems != null && e.OldItems.Count > 0)
				exception = ((RuleExceptionJoinPoint)e.OldItems[0]).Exception;
			if (exception == null)
				return;

			if (IncludeInFilter(exception))
			{
				if (!exceptions.Contains(exception))
					exceptions.Add(exception);
				OnChanged();
			}
			else
			{
				if (exceptions.Contains(exception))
				{
					exceptions.Remove(exception);
					OnChanged();
				}
			}
		}

		/// <summary>
		/// Allows subclasses to evaluate whether an exception should be included in the list of filtered exceptions.
		/// </summary>
		/// <param name="exception"></param>
		/// <returns></returns>
		protected abstract bool IncludeInFilter(Condition exception);

		/// <summary>
		/// Resets the filter by clearing the exception list and reevaluating all exceptions
		/// currently registered with the current context.
		/// </summary>
		protected void Reset()
		{
			// Initialize or clear the exception list
			if (exceptions == null)
			{
				exceptions = new RuleExceptionList();

				// Subscribe to context exception changes
				Context.Current.Exceptions.CollectionChanged += new NotifyCollectionChangedEventHandler(Exceptions_CollectionChanged);
			}
			else
				exceptions.Clear();

			// Process each exception in the context exception list to see if it should be included in the filter
			foreach (Condition exception in Context.Current.Exceptions)
			{
				if (IncludeInFilter(exception))
				{
					exceptions.Add(exception);
					exception.JoinPoints.CollectionChanged += new NotifyCollectionChangedEventHandler(JoinPoints_CollectionChanged);
				}
			}

			// Notify subscribers that the filter has changed
			OnChanged();
		}

		/// <summary>
		/// Notify subscribers that the filtered list of exceptions has changed.
		/// </summary>
		void OnChanged()
		{
			// Exit immediately if change notifications are already pending
			if (hasPendingChanges)
				return;

			//Defer change notifications due to graph changes until they are complete
			if (GraphChangeScope.Current.IsActive)
			{
				hasPendingChanges = true;
				GraphChangeScope.Current.Exited += new GraphChangeScopeExitedHandler(scope_Exited);
			}

			// Otherwise, immediately raise the change event
			else
				RaiseOnChanged();
		}

		/// <summary>
		/// Raises deferred change notifications when the last graph change is complete.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void scope_Exited(object sender, GraphChangeScopeExitedEventArgs e)
		{
			hasPendingChanges = false;
			RaiseOnChanged();
		}

		/// <summary>
		/// Notify subscribers that the filtered list of exceptions has changed.
		/// </summary>
		void RaiseOnChanged()
		{
			if (Changed != null)
				Changed(this, new ExceptionFilterChangedEventArgs(this));
		}

		#region IDisposable Members

		/// <summary>
		/// Called by <see cref="IDisposable.Dispose"/> to allow subclasses to perform cleanup.
		/// </summary>
		protected virtual void Dispose()
		{ }

		/// <summary>
		/// Disposes of the filter and all object/event references
		/// </summary>
		void IDisposable.Dispose()
		{
			// Call the virtual dispose to allow subclasses to perform cleanup
			this.Dispose();

			// Unsubscribe from exception changes
			Context.Current.Exceptions.CollectionChanged += new NotifyCollectionChangedEventHandler(Exceptions_CollectionChanged);
			foreach (Condition removed in exceptions)
				removed.JoinPoints.CollectionChanged -= new NotifyCollectionChangedEventHandler(JoinPoints_CollectionChanged);
			exceptions = null;
		}

		#endregion
	}

	#endregion

	#region FilterChangedEvent

	/// <summary>
	/// Delegate that handles notifications when a filtered list of exceptions has changed.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public delegate void ExceptionFilterChangedEventHandler(object sender, ExceptionFilterChangedEventArgs e);

	/// <summary>
	/// Stores that arguments that are passed with the <see cref="RuleExceptionFilter.FilterChanged"/> event handler.
	/// </summary>
	public class ExceptionFilterChangedEventArgs : EventArgs
	{
		RuleExceptionFilter filter;

		/// <summary>
		/// Creates a new argument instance for the specified filter.
		/// </summary>
		/// <param name="filter"></param>
		internal ExceptionFilterChangedEventArgs(RuleExceptionFilter filter)
		{
			this.filter = filter;
		}

		/// <summary>
		/// Gets the filter that has changed.
		/// </summary>
		public RuleExceptionFilter Filter
		{
			get
			{
				return filter;
			}
		}
	}

	#endregion

	#region RuleExceptionGraphFilter

	/// <summary>
	/// Filters the context-based exception list by exposing a specific set of errors that
	/// have been joined to a specific set of objects in the graph.
	/// </summary>
	public class RuleExceptionGraphFilter : RuleExceptionFilter
	{
		GraphFilter graph;
		Dictionary<ConditionType, ConditionType> errors = new Dictionary<ConditionType, ConditionType>();

		protected RuleExceptionGraphFilter()
		{
		}

		/// <summary>
		/// Creates a new <see cref="RuleExceptionGraphFilter"/> instance.
		/// </summary>
		/// <param name="graph"></param>
		/// <param name="errors"></param>
		public RuleExceptionGraphFilter(GraphFilter graph, ConditionType[] errors)
		{
			this.graph = graph;
			graph.Changed += new GraphFilterChangedEventHandler(graph_Changed);
			foreach (ConditionType error in errors)
			{
				if (!this.errors.ContainsKey(error))
					this.errors.Add(error, error);
			}
		}

		protected GraphFilter Graph
		{
			get { return graph; }
		}

		/// <summary>
		/// Determines whether the specified exception should be included in the filter
		/// based on whether the error is in the set of errors tracked by the filter and
		/// the join points are on at least one object in the graph the filter is assigned to.
		/// </summary>
		/// <param name="exception"></param>
		/// <returns></returns>
		protected override bool IncludeInFilter(Condition exception)
		{
			// First check the error list
			if (!errors.ContainsKey(exception.Error))
				return false;

			// Then check the graph
			foreach (RuleExceptionJoinPoint joinPoint in exception.JoinPoints)
			{
				if (joinPoint.ErrorObject is IRoot && graph.IsInGraph((IRoot)joinPoint.ErrorObject))
					return true;
			}

			// Otherwise, return false;
			return false;
		}

		/// <summary>
		/// Updates the exception filter based on changes to the object graph.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void graph_Changed(object sender, GraphFilterChangedEventArgs e)
		{
			Reset();
		}

		/// <summary>
		/// Unsubscribes from the object graph change notifications.
		/// </summary>
		protected override void Dispose()
		{
			graph.Changed -= new GraphFilterChangedEventHandler(graph_Changed);
			base.Dispose();
		}
	}

	#endregion

	#region RuleExceptionAllErrorsFilter

	/// <summary>
	/// Simple filter subclass that exposes all exceptions in the context-based exception list.
	/// </summary>
	public class RuleExceptionAllErrorsFilter : RuleExceptionFilter
	{
		protected override bool IncludeInFilter(Condition exception)
		{
			return true;
		}
	}

	#endregion

	#region RuleExceptionAllErrorsGraphFilter

	/// <summary>
	/// Simple filter subclass that exposes all exceptions in the context-based exception list that
	/// have been joined to a specific set of objects in the graph.
	/// </summary>
	public class RuleExceptionAllErrorsGraphFilter : RuleExceptionGraphFilter
	{
		public RuleExceptionAllErrorsGraphFilter(GraphFilter graph)
			: base(graph, new ConditionType[]{})
		{
		}

		/// <summary>
		/// Returns true if the exception is joined to one of the specific objects in the graph; otherwise
		/// reutrns false.
		/// </summary>
		/// <param name="exception"></param>
		/// <returns></returns>
		protected override bool IncludeInFilter(Condition exception)
		{
			foreach (RuleExceptionJoinPoint joinPoint in exception.JoinPoints)
			{
				if (joinPoint.ErrorObject is IRoot && base.Graph.IsInGraph((IRoot)joinPoint.ErrorObject))
					return true;
			}

			return false;
		}
		*/
	}
}
