using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StrikeCraftFormation : FormationBase
{
    protected override void Awake()
    {
        base.Awake();
        DestroyOnEmpty = true;
    }

    protected override void Start()
    {
    }

    // The strike craft formation is controlled by a NavMeshAgent.
    // Override all movement related methods to do nothing
    /*
    protected override void Update()
    {
        // Do nothing
    }

    public override void ApplyBraking()
    {
        // Do nothing
    }

    protected override void ApplyBrakingInner()
    {
        // Do nothing
    }

    protected override void ApplyMovementManual()
    {
        // Do nothing
    }

    protected override void ApplyThrust()
    {
        // Do nothing
    }

    public override void ApplyTurning(bool left)
    {
        // Do nothing
    }

    public override void ApplyTurningToward(Vector3 vec)
    {
        // Do nothing
    }

    protected override void ApplyUpdateAcceleration()
    {
        // Do nothing
    }

    protected override void ApplyUpdateTurning()
    {
        // Do nothing
    }

    public override void MoveForward()
    {
        // Do nothing
    }

    public override void MoveBackward()
    {
        // Do nothing
    }*/

    public bool AddStrikeCraft(StrikeCraft s)
    {
        if (_ships.Count >= Positions.Length || s.Owner != Owner || _ships.Contains(s))
        {
            return false;
        }
        _ships.Add(s);
        _AICache.Add(s, (s, s.GetComponent<ShipAIHandle>()));
        return true;
    }

    public bool RemoveStrikeCraft(StrikeCraft s)
    {
        if (_ships.Contains(s))
        {
            _ships.Remove(s);
            _AICache.Remove(s);
            _positionsCache.Clear();
            if (_ships.Count == 0 && DestroyOnEmpty)
            {
                HostCarrier.DeactivateFormation(this);
                Destroy(gameObject);
            }
            return true;
        }
        return false;
    }

    public bool DestroyOnEmpty { get; set; }

    public CarrierBehavior HostCarrier { get; set; }

    public bool AllOutOfAmmo()
    {
        if (_ships.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < _ships.Count; ++i)
        {
            if (!((StrikeCraft)_ships[i]).IsOutOfAmmo())
            {
                return false;
            }
        }
        return true;
    }
}
