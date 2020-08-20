using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Faction : MonoBehaviour
{
    void Awake()
    {
        RegisterFaction();
        UsedNames = new List<string>();
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

        ValueTuple<Faction, Faction> dictKey = (other._factionIdx < _factionIdx) ? new ValueTuple<Faction, Faction>(other, this) : new ValueTuple<Faction, Faction>(this, other);
        FactionRelationType res;
        if (_factionRelations.TryGetValue(dictKey, out res))
        {
            return res;
        }
        Debug.LogWarningFormat("Faction relations not found: {0},{1}", this, other);
        return FactionRelationType.Neutral;
    }

    public void SetRelationsWith(Faction other, FactionRelationType relation)
    {
        ValueTuple<Faction, Faction> dictKey = (other._factionIdx < _factionIdx) ? new ValueTuple<Faction, Faction>(other, this) : new ValueTuple<Faction, Faction>(this, other);
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
                _factionRelations.Add(new ValueTuple<Faction, Faction>(f, this), FactionRelationType.Enemy); // should be neutral
            }
            else
            {
                _factionRelations.Add(new ValueTuple<Faction, Faction>(this, f), FactionRelationType.Enemy); // should be neutral
            }
        }
        _allFactions.Add(this);
    }

    public List<string> UsedNames { get; set; }

    public bool PlayerFaction;

    public Color FactionColor;

    private int _factionIdx = -1;
    private static int _nextFactionIdx = 0;

    private static HashSet<Faction> _allFactions = new HashSet<Faction>();
    private static Dictionary<ValueTuple<Faction, Faction>, FactionRelationType> _factionRelations = new Dictionary<ValueTuple<Faction, Faction>, FactionRelationType>();

    public enum FactionRelationType { Neutral, Ally, Enemy };
}
