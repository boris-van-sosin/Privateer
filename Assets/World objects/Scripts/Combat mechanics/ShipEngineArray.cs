using NUnit.Framework;
using System;
using System.Collections.Generic;
using static UnityEngine.EventSystems.EventTrigger;

public struct ShipEngineArray : IUserToggledComponent, IPeriodicActionComponent
{
    public static ShipEngineArray Init()
    {
        ShipEngineArray res = new ShipEngineArray()
        {
            _engineComps = new List<ShipEngine>(),
            _storedMinEnergyPerTick = -1,
            _nextBrake = false,
            _nextDeactivate = false,
            _active = false
        };
        return res;
    }

    public bool ComponentActive
    {
        get
        {
            for (int i = 0; i < _engineComps.Count; ++i)
            {
                if (_engineComps[i].ComponentActive)
                {
                    return true;
                }
            }
            return false;
        }
        set
        {
            bool lclActive;
            _active = false;
            if (value)
            {
                for (int i = 0; i < _engineComps.Count; ++i)
                {
                    lclActive = _engineComps[i].RequestEngine();
                    _active = _active || lclActive;
                }
            }
            else
            {
                for (int i = 0; i < _engineComps.Count; ++i)
                {
                    _engineComps[i].ComponentActive = false;
                }
            }
        }
    }

    public bool RequestEngine()
    {
        bool anyWorking = false, currWorking;
        for (int i = 0; i < _engineComps.Count; ++i)
        {
            currWorking = _engineComps[i].RequestEngine();
            anyWorking = anyWorking || currWorking;
        }
        if (anyWorking)
        {
            _active = true;
            OnToggle(true);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void PeriodicAction()
    {
        if (AnyWorkingEngines && _active)
        {
            OnToggle?.Invoke(true);
            _nextDeactivate = true;
        }
        else if (_nextBrake)
        {
            OnToggle?.Invoke(true);
            _nextDeactivate = true;
        }
        else
        {
            OnToggle?.Invoke(false);
        }
        if (_nextDeactivate)
        {
            _active = _nextBrake = false;
            _nextDeactivate = false;
            OnToggle?.Invoke(false);
        }
    }

    public void SetBraking()
    {
        _nextBrake = true;
    }

    public void AddEngine(ShipEngine engine)
    {
        _engineComps.Add(engine);
    }

    public bool HasAnyEngines => _engineComps.Count > 0;

    public bool AnyWorkingEngines
    {
        get
        {
            for (int i = 0; i < _engineComps.Count; ++i)
            {
                if (_engineComps[i].ComponentIsWorking)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public double TotalWorkingEnginePower
    {
        get
        {
            return 0.0;
        }
    }

    public ShipBase ContainingShip => throw new NotImplementedException();

    public IEnumerable<string> AllowedSlotTypes => throw new NotImplementedException();

    public ObjectFactory.ShipSize MinShipSize => ObjectFactory.ShipSize.Sloop;

    public ObjectFactory.ShipSize MaxShipSize => ObjectFactory.ShipSize.CapitalShip;

    public DynamicBuff ComponentBuff => _dummyBuff;

    public int EnergyPerTick
    {
        get
        {
            int energy = 0;
            for (int i = 0; i < _engineComps.Count; ++i)
            {
                energy += _engineComps[i].EnergyPerTick;
            }
            return energy;
        }
    }

    public int MinEnergyPerTick
    {
        get
        {
            if (_engineComps.Count == 0)
            {
                return 0;
            }
            int energy = _engineComps[0].EnergyPerTick;
            for (int i = 1; i < _engineComps.Count; ++i)
            {
                if (energy < _engineComps[i].EnergyPerTick)
                {
                    energy = _engineComps[i].EnergyPerTick;
                }
            }
            return energy;
        }
    }

    public int StaticMinEnergyPerTick
    {
        get
        {
            if (_storedMinEnergyPerTick >= 0)
            {
                return _storedMinEnergyPerTick;
            }
            if (_engineComps.Count == 0)
            {
                return 0;
            }
            _storedMinEnergyPerTick = 0;
            for (int i = 0; i < _engineComps.Count; ++i)
            {
                if (_storedMinEnergyPerTick < _engineComps[i].EnergyPerTick)
                {
                    _storedMinEnergyPerTick = _engineComps[i].EnergyPerTick;
                }
            }
            return _storedMinEnergyPerTick;
        }
    }

    public int HeatPerTick
    {
        get
        {
            int heat = 0;
            for (int i = 0; i < _engineComps.Count; ++i)
            {
                heat += _engineComps[i].HeatPerTick;
            }
            return heat;
        }
    }

    public event Action<bool> OnToggle;

    private List<ShipEngine> _engineComps;
    private bool _active, _nextDeactivate, _nextBrake;
    private int _storedMinEnergyPerTick;

    private static DynamicBuff _dummyBuff = DynamicBuff.Default();
}