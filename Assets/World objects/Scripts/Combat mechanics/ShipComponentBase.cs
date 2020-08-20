using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ShipComponentBase : IShipComponent
{
    public ShipBase ContainingShip { get { return _containingShip; } }

    public abstract IEnumerable<string> AllowedSlotTypes { get; }

    protected Ship _containingShip;

    private static DynamicBuff _dummyBuff = DynamicBuff.Default();
    public virtual DynamicBuff ComponentBuff => _dummyBuff;

    public ObjectFactory.ShipSize MinShipSize { get; protected set; }
    public ObjectFactory.ShipSize MaxShipSize { get; protected set; }
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
                Debug.LogFormat("{0} is heavily damaged. Time: {1}", this, Time.time);
            }
            else if (_componentCurrHitPoints <= ComponentMaxHitPoints / 2)
            {
                Status = ComponentStatus.LightlyDamaged;
            }
            else
            {
                Status = ComponentStatus.Undamaged;
            }
            OnHitpointsChanged?.Invoke();
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

    public void ApplyHitPointBuff(StaticBuff.HitPointBuff b)
    {
        if (ComponentHitPoints > 0)
        {
            ComponentMaxHitPoints += b.Component;
        }
    }

    public abstract string SpriteKey { get; }

    public event Action OnHitpointsChanged;

    protected int _componentMaxHitPoints;
    protected int _componentCurrHitPoints;
    protected ComponentStatus _status;
}
