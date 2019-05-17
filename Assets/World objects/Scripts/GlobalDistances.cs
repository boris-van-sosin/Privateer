using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GlobalDistances
{
    public static readonly float TorpedoBomberColdLaunchDist = 0.25f;

    public static readonly Vector3 BoardingPanelOffset = new Vector3(0, 0.05f, 0.5f);

    public static readonly Vector3 ShipExplosionSizeSloop = new Vector3(0.25f, 0.25f, 0.25f);
    public static readonly Vector3 ShipExplosionSizeSFrigate = new Vector3(0.35f, 0.35f, 0.35f);
    public static readonly Vector3 ShipExplosionSizeDestroyer = new Vector3(0.55f, 0.55f, 0.55f);
    public static readonly Vector3 ShipExplosionSizeCruiser = new Vector3(0.85f, 0.85f, 0.85f);
    public static readonly Vector3 ShipExplosionSizeCapitalShip = new Vector3(1f, 1f, 1f);
    public static readonly Vector3 ShipExplosionSizeStrikeCraft = new Vector3(0.05f, 0.05f, 0.05f);

    public static readonly float StrikeCraftAIDistEps = 0.005f;
    public static readonly float StrikeCraftAIAttackDist = 1.0f;
    public static readonly float StrikeCraftAIAheadOfFormationNavDist = 1.0f;
    public static readonly float StrikeCraftAIBehindFormationNavDist = 0.2f;
    public static readonly float StrikeCraftAIFormationRecoveryTargetDist = 1.25f;
    public static readonly float StrikeCraftAIRecoveryDist = 2.5f;
    public static readonly float StrikeCraftAIRecoveryPathFixSize = 1.25f;
    public static readonly float StrikeCraftAIFormationPositionTolerance = 0.1f;

    public static readonly float ShipAIDistEps = 0.005f;

    public static readonly float TorpedoAltEpsilon = 1e-3f;

    public static readonly float HarpaxCableWinchSpeed = 2f;
}
