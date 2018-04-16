using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HarpaxBehavior : Projectile
{
    protected override void Awake()
    {
        base.Awake();
        _cableRenderer = GetComponent<LineRenderer>();
        _cableRenderer.positionCount = 2;
    }

    protected override void Update()
    {
        base.Update();
        _cableRenderer.SetPosition(0, OriginShip.transform.position);
        _cableRenderer.SetPosition(1, transform.position);
    }

    protected override void DoHit(RaycastHit hit, Ship shipHit)
    {
        Destroy(gameObject);
    }

    private LineRenderer _cableRenderer;
}
