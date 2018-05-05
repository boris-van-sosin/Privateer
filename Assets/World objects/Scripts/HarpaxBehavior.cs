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
        if (shipHit.ShipTotalShields > 0)
        {
            Destroy(gameObject);
            return;
        }
        if (_attached)
        {
            return;
        }
        _attached = true;

        Rigidbody origRB = OriginShip.GetComponent<Rigidbody>();
        Rigidbody targetRB = shipHit.GetComponent<Rigidbody>();
        targetRB.drag = 10; targetRB.angularDrag = 10;
        CableBehavior cable = ObjectFactory.CreateHarpaxTowCable(origRB, targetRB, hit.point);
        shipHit.TowedByHarpax = cable;
        cable.MinRopeLength = 0.01f;
        cable.MaxRopeLength = (OriginShip.transform.position - hit.point).magnitude;

        //StartCoroutine(Bleh(hj));
        Destroy(gameObject, 10f);
    }

    private IEnumerator Bleh(Joint j)
    {
        while (true)
        {
            Debug.Log(string.Format("Joint force: {0}", j.currentForce));
            yield return new WaitForSeconds(0.1f);
        }
    }

    private LineRenderer _cableRenderer;
    private bool _attached = false;
}
