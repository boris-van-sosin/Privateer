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

    public bool AddStrikeCraft(StrikeCraft s)
    {
        if (_ships.Count >= Positions.Length || s.Owner != Owner || _ships.Contains(s))
        {
            return false;
        }
        _ships.Add(s);
        _AICache.Add(s, new ValueTuple<ShipBase, ShipAIController>(s, s.GetComponent<StrikeCraftAIController>()));
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
}
