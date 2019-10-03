using System;
using System.Collections.Generic;
using System.Linq;

public class ShipAITactics
{

}

public enum TurretAIHint
{
    Main,
    Secondary,
    CloseIn,
    Torpedo
}

public enum ShipRole
{
    CivilSmall,
    CivilMedium,
    CivilLarge,
    SloopGunboat,
    SloopTorpedoBoat,
    Frigate,
    Destroyer,
    DestroyerSupport,
    Cruiser,
    HeavyCruiser,
    FastCarrier,
    Battleship,
    SpecialBattleship,
    SuperBattleship, // Only Shipbreakers have those
    DemiBattleship,
    Battlecruiser,
    SuperCarrier
}
