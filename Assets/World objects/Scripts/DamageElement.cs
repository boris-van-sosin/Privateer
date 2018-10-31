using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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

    public static bool ArmourPenetration(float armourRating, float armourPenetration)
    {
        float armourDifference = armourPenetration - armourRating;
        float penetrateChance = _minArmourPenetration + (_maxArmourPenetration - _minArmourPenetration) / (1 + Mathf.Exp(-_armourPenetrationSteepness * armourDifference));
        float penetrateRoll = Random.value;
        Debug.Log(string.Format("Hit on armour. Armour rating: {0}. Armour penetration: {1}. Chance to penetrate: {2}. Roll: {3}.", armourRating, armourPenetration, penetrateChance, penetrateRoll));
        return penetrateRoll < penetrateChance;
    }

    public static IEnumerator BoardingCombat(Ship attacker, Ship defender)
    {
        Tuple<Canvas, BoardingProgressPanel> panel = ObjectFactory.CreateBoardingProgressPanel();
        panel.Item1.transform.position = (defender.transform.position + attacker.transform.position) / 2 + new Vector3(0, 0.1f, 1.0f);
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
                if (Random.value < _surrenderChance)
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
                if (Random.value < _surrenderChance)
                {
                    // attacking ship surrenders
                    break;
                }
            }
            yield return new WaitForSeconds(1.0f);
        }
        attacker.ResolveBoardingAction(defender, side1Q.Count == 0);
        defender.ResolveBoardingAction(attacker, side2Q.Count == 0);
        Object.Destroy(panel.Item1.gameObject);
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
                if (Random.value < _combatDeathChance)
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
                if (Random.value < _combatDeathChance)
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
        int combatResult = Random.Range(0, combinedStrength);
        return combatResult < side1Strength;
    }

    private static readonly float _minArmourPenetration = .05f;
    private static readonly float _maxArmourPenetration = .95f;
    private static readonly float _armourPenetrationSteepness = .2f;

    private static readonly int _soldiersInCombatPulse = 10;
    private static readonly float _combatDeathChance = 0.2f;
    private static readonly float _surrenderChance = 0.2f;
    private static readonly int _attackerSurrenderRatio = 2;
    private static readonly int _defenderSurrenderRatio = 4;
}