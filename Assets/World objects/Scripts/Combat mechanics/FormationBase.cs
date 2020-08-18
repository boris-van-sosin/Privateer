using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class FormationBase : MovementBase
{
    protected virtual void Start()
    {
    }

    public override void PostAwake()
    {
    }

    public void CreatePositions(int n)
    {
        Positions = new Transform[n];
        for (int i = 0; i < n; ++i)
        {
            Positions[i] = new GameObject().transform;
            Positions[i].parent = this.transform;
            Positions[i].transform.localRotation = Quaternion.identity;
        }
    }

    public void SetFormationType(FormationType f)
    {
        int posIdx = 0;
        float currX = 0, currXAbs = 0, currY = 0;
        while (posIdx < Positions.Length)
        {
            Positions[posIdx].localPosition = new Vector3(currX, 0f, currY);
            switch (f)
            {
                case FormationType.LineAhead:
                    currY -= YStep;
                    break;
                case FormationType.LineAbreast:
                    if (posIdx % 2 == 0)
                    {
                        currXAbs -= XStep;
                        currX = currXAbs;
                    }
                    else
                    {
                        currX = -currXAbs;
                    }
                    break;
                case FormationType.DiagonalLeft:
                    currY -= YStep;
                    currX += XStep;
                    break;
                case FormationType.DiagonalRight:
                    currY -= YStep;
                    currX -= XStep;
                    break;
                case FormationType.Vee:
                    if (posIdx % 2 == 0)
                    {
                        currY -= YStep;
                        currXAbs -= XStep;
                        currX = currXAbs;
                    }
                    else
                    {
                        currX = -currXAbs;
                    }
                    break;
                default:
                    break;
            }
            ++posIdx;
        }
        ComputeDiameter();
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

    void OnDrawGizmos()
    {
        int i = 0;
        foreach (StrikeCraft s in _ships)
        {
            Gizmos.color = colors[i];
            if (s.InPositionInFormation())
            {
                Gizmos.DrawWireSphere(GetPosition(s), 0.025f);
            }
            else
            {
                Gizmos.DrawWireSphere(GetPosition(s), 0.1f);
            }
            i = (i + 1) % colors.Length;
        }
    }

    public Vector3 GetPosition(ShipBase s)
    {
        int res;
        if (_positionsCache.TryGetValue(s, out res))
        {
            return Positions[res].position;
        }
        else
        {
            res = _ships.IndexOf(s);
            if (res >= 0)
            {
                _positionsCache[s] = res;
                return Positions[res].position;
            }
        }
        throw new Exception("Strike craft not in formation");
    }

    public bool AllInFormation()
    {
        return _ships.All(s => s.InPositionInFormation());
    }

    public IEnumerable<ValueTuple<ShipBase, bool>> InFormationStatus()
    {
        return _ships.Select(s => new ValueTuple<ShipBase, bool>(s, s.InPositionInFormation()));
    }

    public IEnumerable<ShipBase> AllStrikeCraft()
    {
        return _ships;
    }

    public IEnumerable<ValueTuple<ShipBase, ShipAIController>> StrikeCraftAIs
    {
        get
        {
            return _AICache.Values;
        }
    }

    public float Diameter { get; private set; }

    public override float ObjectSize => Diameter;

    private static readonly Color[] colors = new Color[] { Color.red, Color.blue, Color.green, Color.magenta };

    public string ProductionKey;
    public Faction Owner;
    public float XStep, YStep;
    public float MaintainFormationSpeedCoefficient;

    public Transform[] Positions { get; private set; }

    protected List<ShipBase> _ships = new List<ShipBase>();
    protected Dictionary<ShipBase, int> _positionsCache = new Dictionary<ShipBase, int>();
    protected Dictionary<ShipBase, ValueTuple<ShipBase, ShipAIController>> _AICache = new Dictionary<ShipBase, ValueTuple<ShipBase, ShipAIController>>();

    public enum FormationType { LineAhead, LineAbreast, DiagonalLeft, DiagonalRight, Vee };
}
