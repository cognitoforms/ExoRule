using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExoModel;

namespace ExoRule
{
    ///// <summary>
    ///// Provides a filtered view of conditions and raises events when the set of filtered conditions changes.
    ///// </summary>
    //public class ConditionFilter
    //{
    //    ConditionTypeSet conditionTypes;
    //    ModelFilter model;

    //    /// <summary>
    //    /// Creates a new <see cref="RuleConditionModelFilter"/> instance.
    //    /// </summary>
    //    /// <param name="model"></param>
    //    /// <param name="errors"></param>
    //    public ConditionFilter(ConditionTypeSet conditionTypes, ModelFilter model)
    //    {
    //        this.model = model;
    //        this.conditionTypes = conditionTypes;

    //        model.Changed += model_Changed;
    //    }

    //    /// <summary>
    //    /// Determines whether the specified condition should be included in the filter
    //    /// based on whether the error is in the set of errors tracked by the filter and
    //    /// the join points are on at least one object in the model the filter is assigned to.
    //    /// </summary>
    //    /// <param name="condition"></param>
    //    /// <returns></returns>
    //    bool IncludeInFilter(Condition condition)
    //    {
    //        // First check the error list
    //        if (!conditionTypes.ContainsKey(condition.Error))
    //            return false;

    //        // Then check the model
    //        foreach (RuleConditionJoinPoint joinPoint in condition.JoinPoints)
    //        {
    //            if (joinPoint.ErrorObject is IRoot && model.IsInModel((IRoot)joinPoint.ErrorObject))
    //                return true;
    //        }

    //        // Otherwise, return false;
    //        return false;
    //    }

    //    /// <summary>
    //    /// Updates the condition filter based on changes to the object model.
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    void model_Changed(object sender, ModelFilterChangedEventArgs e)
    //    {
    //        Reset();
    //    }
        
    //    List<Condition> conditions;
    //    bool hasPendingChanges;

    //    /// <summary>
    //    /// Creates a new filter and subscribes to changes to the context filter.
    //    /// </summary>
    //    /// <remarks>
    //    /// Subclasses must call <see cref="Reset"/> in order to build the initial filter.
    //    /// </remarks>
    //    protected ConditionFilter()
    //    { }

    //    /// <summary>
    //    /// Gets the list of conditions exposed by this filter.
    //    /// </summary>
    //    public IEnumerable<Condition> Conditions
    //    {
    //        get
    //        {
    //            if (conditions == null)
    //                Reset();
    //            return conditions;
    //        }
    //    }

    //    /// <summary>
    //    /// Notifies filter subscribers that the filtered conditions have changed.
    //    /// </summary>
    //    public event ConditionFilterChangedEventHandler Changed;

    //    /// <summary>
    //    /// Processes changes to the context list of conditions to determine if the condition should be included in the filter.
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    void Conditions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    //    {
    //        bool changed = false;

    //        // Process added conditions
    //        if (e.NewItems != null)
    //        {
    //            foreach (Condition added in e.NewItems)
    //            {
    //                added.JoinPoints.CollectionChanged += new NotifyCollectionChangedEventHandler(JoinPoints_CollectionChanged);
    //                if (IncludeInFilter(added))
    //                {
    //                    conditions.Add(added);
    //                    changed = true;
    //                }
    //            }
    //        }

    //        // Process removed condition
    //        if (e.OldItems != null)
    //        {
    //            foreach (Condition removed in e.OldItems)
    //            {
    //                removed.JoinPoints.CollectionChanged -= new NotifyCollectionChangedEventHandler(JoinPoints_CollectionChanged);
    //                if (conditions.Contains(removed))
    //                {
    //                    conditions.Remove(removed);
    //                    changed = true;
    //                }
    //            }
    //        }

    //        // Notify subscribers that the filter has changed.
    //        if (changed)
    //            OnChanged();
    //    }

    //    /// <summary>
    //    /// Evaluates whether an condition should be included in the filter due to changes to join points.
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    void JoinPoints_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    //    {
    //        Condition condition = null;
    //        if (e.NewItems != null && e.NewItems.Count > 0)
    //            condition = ((RuleConditionJoinPoint)e.NewItems[0]).Condition;
    //        if (e.OldItems != null && e.OldItems.Count > 0)
    //            condition = ((RuleConditionJoinPoint)e.OldItems[0]).Condition;
    //        if (condition == null)
    //            return;

    //        if (IncludeInFilter(condition))
    //        {
    //            if (!conditions.Contains(condition))
    //                conditions.Add(condition);
    //            OnChanged();
    //        }
    //        else
    //        {
    //            if (conditions.Contains(condition))
    //            {
    //                conditions.Remove(condition);
    //                OnChanged();
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// Allows subclasses to evaluate whether an condition should be included in the list of filtered conditions.
    //    /// </summary>
    //    /// <param name="condition"></param>
    //    /// <returns></returns>
    //    protected abstract bool IncludeInFilter(Condition condition);

    //    /// <summary>
    //    /// Resets the filter by clearing the condition list and reevaluating all conditions
    //    /// currently registered with the current context.
    //    /// </summary>
    //    protected void Reset()
    //    {
    //        // Initialize or clear the condition list
    //        if (conditions == null)
    //        {
    //            conditions = new RuleConditionList();

    //            // Subscribe to context condition changes
    //            Context.Current.Conditions.CollectionChanged += new NotifyCollectionChangedEventHandler(Conditions_CollectionChanged);
    //        }
    //        else
    //            conditions.Clear();

    //        // Process each condition in the context condition list to see if it should be included in the filter
    //        foreach (Condition condition in Context.Current.Conditions)
    //        {
    //            if (IncludeInFilter(condition))
    //            {
    //                conditions.Add(condition);
    //                condition.JoinPoints.CollectionChanged += new NotifyCollectionChangedEventHandler(JoinPoints_CollectionChanged);
    //            }
    //        }

    //        // Notify subscribers that the filter has changed
    //        OnChanged();
    //    }

    //    /// <summary>
    //    /// Notify subscribers that the filtered list of conditions has changed.
    //    /// </summary>
    //    void OnChanged()
    //    {
    //        // Exit immediately if change notifications are already pending
    //        if (hasPendingChanges)
    //            return;

    //        //Defer change notifications due to model changes until they are complete
    //        if (ModelChangeScope.Current.IsActive)
    //        {
    //            hasPendingChanges = true;
    //            ModelChangeScope.Current.Exited += new ModelChangeScopeExitedHandler(scope_Exited);
    //        }

    //        // Otherwise, immediately raise the change event
    //        else
    //            RaiseOnChanged();
    //    }

    //    /// <summary>
    //    /// Raises deferred change notifications when the last model change is complete.
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    void scope_Exited(object sender, ModelChangeScopeExitedEventArgs e)
    //    {
    //        hasPendingChanges = false;
    //        RaiseOnChanged();
    //    }

    //    /// <summary>
    //    /// Notify subscribers that the filtered list of conditions has changed.
    //    /// </summary>
    //    void RaiseOnChanged()
    //    {
    //        if (Changed != null)
    //            Changed(this, new ConditionFilterChangedEventArgs(this));
    //    }

    //    #region IDisposable Members

    //    /// <summary>
    //    /// Called by <see cref="IDisposable.Dispose"/> to allow subclasses to perform cleanup.
    //    /// </summary>
    //    protected virtual void Dispose()
    //    { }

    //    /// <summary>
    //    /// Disposes of the filter and all object/event references
    //    /// </summary>
    //    void IDisposable.Dispose()
    //    {
    //        // Call the virtual dispose to allow subclasses to perform cleanup
    //        this.Dispose();

    //        // Unsubscribe from condition changes
    //        Context.Current.Conditions.CollectionChanged += new NotifyCollectionChangedEventHandler(Conditions_CollectionChanged);
    //        foreach (Condition removed in conditions)
    //            removed.JoinPoints.CollectionChanged -= new NotifyCollectionChangedEventHandler(JoinPoints_CollectionChanged);
    //        conditions = null;

    //        model.Changed -= new ModelFilterChangedEventHandler(model_Changed);
    //    }

    //    #endregion
    //}

    //#endregion

    //#region FilterChangedEvent

    ///// <summary>
    ///// Delegate that handles notifications when a filtered list of conditions has changed.
    ///// </summary>
    ///// <param name="sender"></param>
    ///// <param name="e"></param>
    //public delegate void ConditionFilterChangedEventHandler(object sender, ConditionFilterChangedEventArgs e);

    ///// <summary>
    ///// Stores that arguments that are passed with the <see cref="RuleConditionFilter.FilterChanged"/> event handler.
    ///// </summary>
    //public class ConditionFilterChangedEventArgs : EventArgs
    //{
    //    RuleConditionFilter filter;

    //    /// <summary>
    //    /// Creates a new argument instance for the specified filter.
    //    /// </summary>
    //    /// <param name="filter"></param>
    //    internal ConditionFilterChangedEventArgs(RuleConditionFilter filter)
    //    {
    //        this.filter = filter;
    //    }

    //    /// <summary>
    //    /// Gets the filter that has changed.
    //    /// </summary>
    //    public RuleConditionFilter Filter
    //    {
    //        get
    //        {
    //            return filter;
    //        }
    //    }
    //}

    //#endregion
}
