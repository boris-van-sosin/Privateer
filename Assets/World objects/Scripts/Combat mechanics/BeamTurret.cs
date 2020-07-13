﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeamTurret : GeneralBeamTurret
{
    protected override void Start()
    {
        base.Start();
        _beamDurationWait = new WaitForSeconds(BeamDuration);
    }

    IEnumerator HandleBeam()
    {
        yield return _endOfFrameWait;
        _beamRenderer.enabled = true;
        Warhead w = ObjectFactory.CreateWarhead(ObjectFactory.WeaponType.Lance, TurretSize);
        DoBeamHit(w);
        yield return new WaitForSeconds(BeamDuration);
        _beamRenderer.enabled = false;
        yield return null;
    }

    protected override void FireInner(Vector3 firingVector, int barrelIdx)
    {        _firingVector = firingVector;
        _beamOrigin = Muzzles[barrelIdx].position;
        StartCoroutine(HandleBeam());
    }

    public override bool IsTurretModCombatible(TurretMod m)
    {
        switch (m)
        {
            case TurretMod.None:
            case TurretMod.TractorBeam:
            case TurretMod.ImprovedCapacitors:
                return true;
            case TurretMod.FastAutoloader:
            case TurretMod.Harpax:
            case TurretMod.Accelerator:
            case TurretMod.AdvancedTargeting:
            default:
                return false;
        }
    }

    protected override void FireGrapplingToolInner(Vector3 firingVector, int barrelIdx)
    {
        // Not implemented yet
    }

    public float BeamDuration;

    private WaitForSeconds _beamDurationWait;
}