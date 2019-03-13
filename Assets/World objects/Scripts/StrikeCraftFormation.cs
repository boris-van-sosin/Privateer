using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StrikeCraftFormation : MovementBase
{
    void Start()
    {
        ComputeDiameter();
    }

    public bool AddStrikeCraft(StrikeCraft s)
    {
        if (_craft.Count >= Positions.Length || s.Owner != Owner || _craft.Contains(s))
        {
            return false;
        }
        _craft.Add(s);
        _AICache.Add(s, Tuple<StrikeCraft, StrikeCraftAIController>.Create(s, s.GetComponent<StrikeCraftAIController>()));
        return true;
    }

    public bool RemoveStrikeCraft(StrikeCraft s)
    {
        if (_craft.Contains(s))
        {
            _craft.Remove(s);
            _AICache.Remove(s);
            _positionsCache.Clear();
            if (_craft.Count == 0)
            {
                Destroy(gameObject);
            }
            return true;
        }
        return false;
    }

    public Vector3 GetPosition(StrikeCraft s)
    {
        int res;
        if (_positionsCache.TryGetValue(s, out res))
        {
            return Positions[res].position;
        }
        else
        {
            res = _craft.IndexOf(s);
            if (res >= 0)
            {
                _positionsCache[s] = res;
                return Positions[res].position;
            }
        }
        throw new System.Exception("Strike craft not in formation");
    }

    public bool AllInFormation()
    {
        return _craft.All(s => (GetPosition(s) - s.transform.position).sqrMagnitude <= _distThresh * _distThresh);
    }

    public IEnumerable<ValueTuple<StrikeCraft, bool>> InFormationStatus()
    {
        return _craft.Select(s => ValueTuple<StrikeCraft, bool>.Create(s, (GetPosition(s) - s.transform.position).sqrMagnitude <= _distThresh * _distThresh));
    }

    public IEnumerable<StrikeCraft> AllStrikeCraft()
    {
        return _craft;
    }

    void OnDrawGizmos()
    {
        int i = 0;
        foreach (StrikeCraft s in _craft)
        {
            Gizmos.color = colors[i];
            Gizmos.DrawWireSphere(GetPosition(s), 0.1f);
            i = (i + 1) % colors.Length;
        }
    }

    private void ComputeDiameter()
    {
        Diameter = 0;
        for (int i = 0; i < Positions.Length; ++i)
        {
            for (int j = i + 1; j < Positions.Length; ++j)
            {
                float currDistSqr = (Positions[i].position - Positions[j].position).sqrMagnitude;
                Diameter = Mathf.Max(Diameter, currDistSqr);
            }
        }
        Diameter = Mathf.Sqrt(Diameter);
    }
    public float Diameter { get; private set; }

    public IEnumerable<Tuple<StrikeCraft, StrikeCraftAIController>> StrikeCraftAIs
    {
        get
        {
            return _AICache.Values;
        }
    }

    private static readonly Color[] colors = new Color[] { Color.red, Color.blue, Color.green, Color.magenta };
    private List<StrikeCraft> _craft = new List<StrikeCraft>();
    private Dictionary<StrikeCraft, int> _positionsCache = new Dictionary<StrikeCraft, int>();
    private Dictionary<StrikeCraft, Tuple<StrikeCraft, StrikeCraftAIController>> _AICache = new Dictionary<StrikeCraft, Tuple<StrikeCraft, StrikeCraftAIController>>();

    public string ProductionKey;
    public Faction Owner;
    public Transform[] Positions;
    public float MaintainFormationSpeedCoefficient;
    private static readonly float _distThresh = 0.1f;
}
