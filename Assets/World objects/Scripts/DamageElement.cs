using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[System.Serializable]
public struct Warhead
{
    public int ShieldDamage { get; set; }
    public int ArmourDamage { get; set; }
    public int ArmourPenetration { get; set; }
    public int SystemDamage { get; set; }
    public int HullDamage { get; set; }
    public int HeatGenerated { get; set; }
    public int SystemHitMultiplicity { get; set; }
    public float BlastRadius { get; set; }
    public float EffectVsStrikeCraft { get; set; }
    public float WeaponEffectScale { get; set; }
}

public static class Combat
{
    public static bool DamageShields(int shieldDamage, IEnumerable<IShieldComponent> shields)
    {
        bool hasShield = false;
        do
        {
            hasShield = false;
            IShieldComponent maxShield = null;
            foreach (IShieldComponent shield in shields)
            {
                if (maxShield == null || shield.CurrShieldPoints > maxShield.CurrShieldPoints)
                {
                    maxShield = shield;
                }
                if (shield.CurrShieldPoints > 0)
                {
                    hasShield = true;
                }
            }
            if (hasShield)
            {
                if (maxShield.CurrShieldPoints >= shieldDamage)
                {
                    maxShield.CurrShieldPoints -= shieldDamage;
                    return false;
                }
                else
                {
                    shieldDamage -= maxShield.CurrShieldPoints;
                    maxShield.CurrShieldPoints = 0;
                }
            }
            if (hasShield && shieldDamage <= 0)
            {
                return false;
            }
        } while (shieldDamage > 0 && hasShield);
        return true;
    }

    public static bool ArmourPenetration(int armourRating, int armourPenetration)
    {
        ArmourPenetrationTable penetrationTable = ObjectFactory.GetArmourPenetrationTable();
        float penetrateChance = penetrationTable.PenetrationProbability(armourRating, armourPenetration);
        //float armourDifference = armourPenetration - armourRating;
        //float penetrateChance = _minArmourPenetration + (_maxArmourPenetration - _minArmourPenetration) / (1 + Mathf.Exp(-_armourPenetrationSteepness * armourDifference));
        float penetrateRoll = UnityEngine.Random.value;
        //Debug.Log(string.Format("Hit on armour. Armour rating: {0}. Armour penetration: {1}. Chance to penetrate: {2}. Roll: {3}.", armourRating, armourPenetration, penetrateChance, penetrateRoll));
        return penetrateRoll < penetrateChance;
    }

    public static IEnumerator BoardingCombat(Ship attacker, Ship defender)
    {
        Tuple<Canvas, BoardingProgressPanel> panel = ObjectFactory.CreateBoardingProgressPanel();
        panel.Item1.transform.position = (defender.transform.position + attacker.transform.position) / 2 + GlobalDistances.BoardingPanelOffset;
        panel.Item2.StartBreaching(attacker, defender);
        LinkedList<ShipCharacter> side1Q = new LinkedList<ShipCharacter>(attacker.AllCrew.Where(x => x.Status == ShipCharacter.CharacterStaus.Active).OrderBy(x => x.CombatPriority));
        yield return new WaitForEndOfFrame();
        LinkedList<ShipCharacter> side2Q = new LinkedList<ShipCharacter>(defender.AllCrew.Where(x => x.Status == ShipCharacter.CharacterStaus.Active).OrderBy(x => x.CombatPriority));
        yield return new WaitForEndOfFrame();
        for (int i = 0; i < 100; ++i)
        {
            panel.Item2.UpdateBreaching(i + 1);
            yield return new WaitForSeconds(0.1f);
        }
        int initialAttackerForce = side1Q.Count;
        int initialDefenderForce = side2Q.Count;
        panel.Item2.StartBoarding(initialAttackerForce, initialDefenderForce);
        yield return new WaitForSeconds(0.1f);
        while (side1Q.Count > 0 && side2Q.Count > 0)
        {
            BoardingCombatPulse(side1Q, side2Q);
            panel.Item2.UpdateBoarding(side1Q.Count, side2Q.Count);
            if (side2Q.Count > 0 && side2Q.Count < initialDefenderForce / _defenderSurrenderRatio)
            {
                if (UnityEngine.Random.value < _surrenderChance)
                {
                    // defending ship surrenders
                    foreach (ShipCharacter c in side2Q)
                    {
                        c.Status = ShipCharacter.CharacterStaus.Incapacitated;
                    }
                    side2Q.Clear();
                    break;
                }
            }
            if (side1Q.Count > 0 && side1Q.Count < initialAttackerForce / _attackerSurrenderRatio)
            {
                if (UnityEngine.Random.value < _surrenderChance)
                {
                    // attacking ship surrenders
                    break;
                }
            }
            yield return new WaitForSeconds(1.0f);
        }
        attacker.ResolveBoardingAction(defender, side1Q.Count == 0);
        defender.ResolveBoardingAction(attacker, side2Q.Count == 0);
        UnityEngine.Object.Destroy(panel.Item1.gameObject);
        yield return null;
    }

    private static void BoardingCombatPulse(LinkedList<ShipCharacter> side1, LinkedList<ShipCharacter> side2)
    {
        int side1Strength = 0, side2Strength = 0;
        LinkedListNode<ShipCharacter> side1Iter = side1.Last, side2Iter = side2.Last;
        for (int i = 0; i < _soldiersInCombatPulse; ++i)
        {
            if (side1Iter != null)
            {
                side1Strength += side1Iter.Value.CombatStrength;
                side1Iter = side1Iter.Previous;
            }
            if (side2Iter != null)
            {
                side2Strength += side2Iter.Value.CombatStrength;
                side2Iter = side2Iter.Previous;
            }
        }
        if (CombatResult(side1Strength, side2Strength))
        {
            for (int i = 0; i < _soldiersInCombatPulse; ++i)
            {
                if (side2.Count == 0)
                {
                    break;
                }
                if (UnityEngine.Random.value < _combatDeathChance)
                {
                    side2.Last.Value.Status = ShipCharacter.CharacterStaus.Dead;
                }
                else
                {
                    side2.Last.Value.Status = ShipCharacter.CharacterStaus.Incapacitated;
                }
                side2.RemoveLast();
            }
        }
        else
        {
            for (int i = 0; i < _soldiersInCombatPulse; ++i)
            {
                if (side1.Count == 0)
                {
                    break;
                }
                if (UnityEngine.Random.value < _combatDeathChance)
                {
                    side1.Last.Value.Status = ShipCharacter.CharacterStaus.Dead;
                }
                else
                {
                    side1.Last.Value.Status = ShipCharacter.CharacterStaus.Incapacitated;
                }
                side1.RemoveLast();
            }
        }
    }

    private static bool CombatResult(int side1Strength, int side2Strength)
    {
        int combinedStrength = side1Strength + side2Strength;
        int combatResult = UnityEngine.Random.Range(0, combinedStrength);
        return combatResult < side1Strength;
    }

    private static readonly int _soldiersInCombatPulse = 10;
    private static readonly float _combatDeathChance = 0.2f;
    private static readonly float _surrenderChance = 0.2f;
    private static readonly int _attackerSurrenderRatio = 2;
    private static readonly int _defenderSurrenderRatio = 4;
}

public class ArmourPenetrationTable
{
    public ArmourPenetrationTable(string[][] lines)
    {
        // First line: the warhead keys:
        _armourKeys = new int[lines.Length - 1];
        string[] warheadsLine = lines[0];
        int cols = warheadsLine.Length;
        _penetrationTable = new float[lines.Length - 1, cols - 1];
        _penetrationKeys = new int[cols - 1];
        for (int i = 1; i < cols; ++i)
        {
            _penetrationKeys[i - 1] = int.Parse(warheadsLine[i]);
        }
        for (int i = 1; i < lines.Length; ++i)
        {
            string[] currLine = lines[i];
            if (currLine.Length != cols)
            {
                throw new System.Exception(string.Format("Incorrect number of colums in armour penetration table. Line: {0}", i));
            }
            _armourKeys[i - 1] = int.Parse(currLine[0]);
            for (int j = 1; j < currLine.Length; ++j)
            {
                float currPen = float.Parse(currLine[j]);
                _penetrationTable[i - 1, j - 1] = currPen;
            }
        }
        _minArmour = _armourKeys.First();
        _maxArmour = _armourKeys.Last();
        _minPenetration = _penetrationKeys.First();
        _maxPenetration = _penetrationKeys.Last();
    }

    public float PenetrationProbability(int Armour, int Penetration)
    {
        float fixedArmour = MapToTable(Armour, _minArmour, _maxArmour);
        float fixedPenetration = MapToTable(Penetration, _minPenetration, _maxPenetration);
        int intArmour = Mathf.FloorToInt(fixedArmour);
        int intPenetration = Mathf.FloorToInt(fixedPenetration);
        int armourIdxBelow = ApproxBinarySearch(_armourKeys, intArmour);
        int penetrationIdxBelow = ApproxBinarySearch(_penetrationKeys, intPenetration);
        int armourIdxAbove = armourIdxBelow + 1, penetrationIdxAbove = penetrationIdxBelow + 1;

        float val00 = _penetrationTable[armourIdxBelow, penetrationIdxBelow],
              val01 = _penetrationTable[armourIdxBelow, penetrationIdxAbove],
              val10 = _penetrationTable[armourIdxAbove, penetrationIdxBelow],
              val11 = _penetrationTable[armourIdxAbove, penetrationIdxAbove];
        float armourFactor0 = _armourKeys[armourIdxAbove] - fixedArmour,
              armourFactor1 = fixedArmour - _armourKeys[armourIdxBelow],
              penetrationFactor0 = _penetrationKeys[penetrationIdxAbove] - fixedPenetration,
              penetrationFactor1 = fixedPenetration - _penetrationKeys[penetrationIdxBelow],
              normaliziation = 1f/((_armourKeys[armourIdxAbove] - _armourKeys[armourIdxBelow]) *(_penetrationKeys[penetrationIdxAbove] - _penetrationKeys[penetrationIdxBelow]));

        float res = (val00 * armourFactor0 * penetrationFactor0 +
                     val01 * armourFactor0 * penetrationFactor1 +
                     val10 * armourFactor1 * penetrationFactor0 +
                     val11 * armourFactor1 * penetrationFactor1) * normaliziation;
        return res;
    }

    private static float MapToTable(float val, float min, float max)
    {
        //float mid = (max + min) / 2f;
        float intSize = max - min;
        //float slope = 1f / (intSize * _slopeFactor);
        //return min + intSize / (1 + Mathf.Exp(-(val - mid) * slope));

        // Map to [0,1]:
        float newVal = (val - min) / intSize;
        //float smoothVal = SmoothStep(newVal);
        return Mathf.Lerp(min, max, newVal);
    }

    private static float SmoothStep(float t)
    {
        // Implements SmoothestStep, orginally by Kyle McDonald
        float t2 = Mathf.Clamp01(t);
        return -20f*Mathf.Pow(t2, 7) + 70f*Mathf.Pow(t2, 6) - 84f*Mathf.Pow(t2, 5) + 35f*Mathf.Pow(t2, 4);
    }

    private static int ApproxBinarySearch(int[] arr, int val)
    {
        int low = 0, high = arr.Length - 1, idx;
        while (true)
        {
            idx = (high + low) / 2;
            if (arr[idx] > val)
            {
                high = idx;
            }
            else if (arr[idx] < val)
            {
                low = idx;
            }
            else
            {
                return idx;
            }
            if (low + 1 == high || low == high)
            {
                return low;
            }
        }
    }

    private readonly int[] _armourKeys;
    private readonly int[] _penetrationKeys;
    private readonly float _minArmour, _maxArmour, _minPenetration, _maxPenetration;
    private readonly float[,] _penetrationTable;
    private static readonly float _slopeFactor = 0.1f;
}