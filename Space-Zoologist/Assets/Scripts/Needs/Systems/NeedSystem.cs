using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Abstract class that all NeedSystems will inherit from. Every need system will have a list of consumers (Life) 
/// that have the need that the need system is in charge of, and keeps this need up to date for all of its 
/// consumers.
/// </summary>
abstract public class NeedSystem
{
    public NeedType NeedType { get; private set; }
    public bool IsDirty => this.isDirty;

    // Dirty flag is on to force intial update
    protected bool isDirty = true;
    protected HashSet<Life> Consumers = new HashSet<Life>();

    public NeedSystem(NeedType needType)
    {
        NeedType = needType;
    }

    /// <summary>
    /// Mark this system dirty
    /// </summary>
    /// <remarks>Any one can mark a system dirty, but only the system can unmark itself</remarks>
    virtual public void MarkAsDirty()
    {
        this.isDirty = true;
    }

    virtual public void AddConsumer(Life life)
    {
        this.isDirty = true;
        this.Consumers.Add(life);
    }

    virtual public bool RemoveConsumer(Life life)
    {
        this.isDirty = true;
        return this.Consumers.Remove(life);
    }

    // Check the evnoirmental state of the consumers, currently only terrain
    virtual public bool CheckState()
    {
        foreach (Life consumer in this.Consumers)
        {
            if (consumer.GetAccessibilityStatus())
            {
                return true;
            }
        }

        return false;
    }

    abstract public void UpdateSystem();
}