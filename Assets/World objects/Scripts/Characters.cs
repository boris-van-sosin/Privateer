using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipCharacter
{
    public int CombatStrength { get; set; }
    public CharacterSpecies Species { get; set; }
    public CharacterStaus Status { get; set; }
    public CharacterProfession Role { get; set; }
    public float CombatPriority { get; set; }

    public enum CharacterSpecies { Terran }
    public enum CharacterProfession { Crew, Captain, Combat }
    public enum CharacterStaus { Active, Incapacitated, Dead }

    public static ShipCharacter GenerateTerranShipCrew()
    {
        return new ShipCharacter()
        {
            Species = CharacterSpecies.Terran,
            CombatStrength = 20,
            Role = CharacterProfession.Crew,
            Status = CharacterStaus.Active,
            CombatPriority = 2.0f + Random.value
        };
    }

    public static ShipCharacter GenerateTerranCombatCrew(int combatStrength)
    {
        return new ShipCharacter()
        {
            Species = CharacterSpecies.Terran,
            CombatStrength = combatStrength,
            Role = CharacterProfession.Combat,
            Status = CharacterStaus.Active,
            CombatPriority = 3.0f + Random.value
        };
    }
}

public class SpecialCharacter : ShipCharacter
{
    public void EffectOnShip(Ship s)
    {

    }

    public string Name { get; set; }
    public string Title { get; set; }
}
