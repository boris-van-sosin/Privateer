using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ShipComponentBase : IShipComponent
{
    public Ship ContainingShip { get { return _containingShip; } }

    public abstract ComponentSlotType ComponentType { get; }

    protected Ship _containingShip;
}

public abstract class ShipActiveComponentBase : ShipComponentBase, IShipActiveComponent
{
    public virtual int ComponentMaxHitPoints
    {
        get
        {
            return _componentMaxHitPoints;
        }
        protected set
        {
            if (value > 0)
            {
                _componentMaxHitPoints = value;
            }
        }
    }
    public virtual int ComponentHitPoints
    {
        get
        {
            return _componentCurrHitPoints;
        }
        set
        {
            _componentCurrHitPoints = System.Math.Max(0, value);
            if (_componentCurrHitPoints == 0)
            {
                Status = ComponentStatus.Destroyed;
            }
            else if (_componentCurrHitPoints <= ComponentMaxHitPoints / 10)
            {
                Status = ComponentStatus.KnockedOut;
            }
            else if (_componentCurrHitPoints <= ComponentMaxHitPoints / 4)
            {
                Status = ComponentStatus.HeavilyDamaged;
                Debug.Log(string.Format("{0} is heavily damaged. Time: {1}", this, Time.time));
            }
            else if (_componentCurrHitPoints <= ComponentMaxHitPoints / 2)
            {
                Status = ComponentStatus.LightlyDamaged;
            }
            else
            {
                Status = ComponentStatus.Undamaged;
            }
            if (OnHitpointsChanged != null)
            {
                OnHitpointsChanged();
            }
        }
    }
    public virtual bool ComponentIsWorking
    {
        get
        {
            switch (Status)
            {
                case ComponentStatus.Undamaged:
                    return true;
                case ComponentStatus.LightlyDamaged:
                    return true;
                case ComponentStatus.HeavilyDamaged:
                    return true;
                case ComponentStatus.KnockedOut:
                    return false;
                case ComponentStatus.Destroyed:
                    return false;
                default:
                    return false;
            }
        }
    }
    public virtual ComponentStatus Status
    {
        get
        {
            return _status;
        }
        protected set
        {
            _status = value;
        }
    }

    public abstract string SpriteKey { get; }

    public event ComponentHitpointsChangedDelegate OnHitpointsChanged;

    protected int _componentMaxHitPoints;
    protected int _componentCurrHitPoints;
    protected ComponentStatus _status;
}
