using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GlobalDistances
{
    // Constants:
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
    public static readonly float StrikeCraftAIFormationEscortDist = 6f;
    public static readonly float StrikeCraftAIFormationAggroDist = 30f;
    public static readonly float StrikeCraftAIFormationUnAggroDist = 50f;

    public static readonly float ShipAIDistEps = 0.005f;

    public static readonly float TorpedoAltEpsilon = 1e-3f;
    public static readonly float TorpedoLaunchNoiseMagnitude = 0.001f;

    public static readonly float HarpaxCableWinchSpeed = 2f;

    // Factors:
    public static readonly float ShipAIAntiClumpLengthFactor = 2f;
    public static readonly float ShipAIAntiClumpMoveDistFactor = 0.5f;

    public static readonly float Bug0StoppingDistFactor = 20f;
    public static readonly float Bug0AvoidObstacleOriginPtFactor = 0.25f;
    public static readonly float Bug0AvoidObstacleRangeFactor = 1.1f;
    public static readonly float Bug0ObstacleMarginFactor = 1.1f;
    public static readonly float Bug0WallFollowMinRangeFactor = 0.5f;
    public static readonly float Bug0WallFollowMaxRangeFactor = 1f;
    public static readonly float Bug0WallFollowRangeDiffFactor = 0.2f;
    public static readonly float Bug0ForwardCastDistFactor = 2f;

    public static readonly float StrikeCraftAIAttackPosRangeFactor = 0.75f;
    public static readonly float StrikeCraftAIVsStrikeCrafRangeFactor = 0.01f;
    public static readonly float StrikeCraftAICarrierFollowDistFactor = 0.95f;

    public static readonly float TurretTargetAcquisitionRangeFactor = 1.05f;
}

public static class GlobalTimes
{

}