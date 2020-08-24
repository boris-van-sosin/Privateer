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

    public override void ResetObject()
    {
        base.ResetObject();
        _attached = false;
    }

    protected override void RecycleObject()
    {
        gameObject.SetActive(false);
        ObjectFactory.ReleaseHarpaxProjectile(this);
    }

    protected override void DoHit(RaycastHit hit, ShipBase shipHit)
    {
        if (shipHit.ShipTotalShields > 0)
        {
            RecycleObject();
            return;
        }
        if (_attached || OriginShip.TowedByHarpax != null || OriginShip.TowingByHarpax != null || shipHit.TowedByHarpax != null || shipHit.TowingByHarpax != null)
        {
            RecycleObject();
            return;
        }
        _attached = true;
        OriginShip.GrapplingMode = false;

        Rigidbody origRB = OriginShip.GetComponent<Rigidbody>();
        Rigidbody targetRB = shipHit.GetComponent<Rigidbody>();
        targetRB.drag = 10; targetRB.angularDrag = 10;
        origRB.drag = 10; origRB.angularDrag = 10;
        CableBehavior cable = ObjectFactory.AcquireHarpaxTowCable(origRB, targetRB, hit.point);
        OriginShip.TowingByHarpax = cable;
        shipHit.TowedByHarpax = cable;
        cable.MinRopeLength = 0.01f;
        cable.MaxRopeLength = (OriginShip.transform.position - hit.point).magnitude;

        RecycleObject();
    }

    private IEnumerator Bleh(Joint j)
    {
        while (true)
        {
            Debug.LogFormat("Joint force: {0}", j.currentForce);
            yield return new WaitForSeconds(0.1f);
        }
    }

    private LineRenderer _cableRenderer;
    private bool _attached = false;
}
