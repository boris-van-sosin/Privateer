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
            Destroy(gameObject, 10f);
            return;
        }
        if (_attached)
        {
            return;
        }
        _attached = true;
        GameObject[] cableSegs = ObjectFactory.CreateHarpaxTowCable(OriginShip.transform.position, hit.point);
        _cableRenderer.positionCount = cableSegs.Length;
        for (int i = 0; i < cableSegs.Length; ++i)
        {
            _cableRenderer.SetPosition(i, cableSegs[i].transform.position);
        }
        Joint hj = OriginShip.gameObject.AddComponent<SpringJoint>();
        //hj.axis = Vector3.forward;
        hj.anchor = Vector3.zero;
        hj.connectedBody = cableSegs[0].GetComponent<Rigidbody>();
        cableSegs[cableSegs.Length - 1].GetComponent<Joint>().connectedBody = shipHit.GetComponent<Rigidbody>();
        Destroy(gameObject, 10f);
    }

    private LineRenderer _cableRenderer;
    private bool _attached = false;
}
