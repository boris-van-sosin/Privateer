using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipCharacter
{
    public int CombatStrength { get; set; }
    public CharacterSpecies Species { get; set; }
    public CharacterStaus Status { get; set; }

    public enum CharacterSpecies { Terran }
    public enum CharacterProfession { Crew, Captain }
    public enum CharacterStaus { Active, Incapacitated, Dead }
}

public class SpecialCharacter : ShipCharacter
{
    public void EffectOnShip(Ship s)
    {

    }

    public string Name { get; set; }
}
