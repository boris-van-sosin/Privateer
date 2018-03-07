﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Faction : MonoBehaviour
{
    void Awake()
    {
        RegisterFaction();
    }

    // Use this for initialization
    void Start ()
    {
	}
	
	// Update is called once per frame
	void Update ()
    {
		if (!_tmpInitialized)
        {
            if (!PlayerFaction)
            {
                Ship[] ships = FindObjectsOfType<Ship>();
                foreach (Ship s in ships)
                {
                    if (s.Owner == this)
                    {
                        if (s.Turrets != null)
                        {
                            foreach (ITurret t in s.Turrets)
                            {
                                t.SetTurretBehavior(TurretBase.TurretMode.Auto);
                            }
                            _tmpInitialized = true;
                        }
                    }
                }
            }
        }
	}

    public bool IsEnemy(Faction other)
    {
        return RelationsWith(other) == FactionRelationType.Enemy;
    }

    public FactionRelationType RelationsWith(Faction other)
    {
        if (other == null)
        {
            return FactionRelationType.Neutral;
        }
        if (this == other)
        {
            return FactionRelationType.Ally;
        }

        Tuple<Faction, Faction> dictKey = (other._factionIdx < _factionIdx) ? new Tuple<Faction, Faction>(other, this) : new Tuple<Faction, Faction>(this, other);
        FactionRelationType res;
        if (_factionRelations.TryGetValue(dictKey, out res))
        {
            return res;
        }
        Debug.LogWarning(string.Format("Faction relations not found: {0},{1}", this, other));
        return FactionRelationType.Neutral;
    }

    public void SetRelationsWith(Faction other, FactionRelationType relation)
    {
        Tuple<Faction, Faction> dictKey = (other._factionIdx < _factionIdx) ? new Tuple<Faction, Faction>(other, this) : new Tuple<Faction, Faction>(this, other);
        _factionRelations[dictKey] = relation;
    }

    private void RegisterFaction()
    {
        if (_factionIdx >= 0)
        {
            return;
        }
        _factionIdx = _nextFactionIdx++;
        foreach (Faction f in _allFactions)
        {
            if (f._factionIdx < _factionIdx)
            {
                _factionRelations.Add(Tuple<Faction, Faction>.Create(f, this), FactionRelationType.Enemy); // should be neutral
            }
            else
            {
                _factionRelations.Add(Tuple<Faction, Faction>.Create(this, f), FactionRelationType.Enemy); // should be neutral
            }
        }
        _allFactions.Add(this);
    }

    public bool PlayerFaction;

    private bool _tmpInitialized = false;

    private int _factionIdx = -1;
    private static int _nextFactionIdx = 0;

    private static HashSet<Faction> _allFactions = new HashSet<Faction>();
    private static Dictionary<Tuple<Faction, Faction>, FactionRelationType> _factionRelations = new Dictionary<Tuple<Faction, Faction>, FactionRelationType>();

    public enum FactionRelationType { Neutral, Ally, Enemy };
}