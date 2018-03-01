using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Warhead
{
    public int ShieldDamage { get; set; }
    public int ArmourDamage { get; set; }
    public int ArmourPenetration { get; set; }
    public int SystemDamage { get; set; }
    public int HullDamage { get; set; }
    public int HeatGenerated { get; set; }
    public Vector3 WeaponEffectScale { get; set; }
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

    private static readonly float _minArmourPenetration = .05f;
    private static readonly float _maxArmourPenetration = .95f;
    private static readonly float _armourPenetrationSteepness = .2f;
}