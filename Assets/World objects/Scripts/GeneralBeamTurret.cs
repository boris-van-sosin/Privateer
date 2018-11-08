using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GeneralBeamTurret : DirectionalTurret
{
    protected override void Awake()
    {
        base.Awake();
        _beamRenderer = GetComponentInChildren<LineRenderer>();
        _beamRenderer.positionCount = 2;
        _beamRenderer.useWorldSpace = true;
    }

    protected void DoBeamHit(Warhead w)
    {
        Vector3 boxCenter = _beamOrigin + 0.5f * MaxRange * _firingVector;
        Vector3 boxSize = new Vector3(0.01f, 0.3f, MaxRange);
        //ExtDebug.DrawBoxCastBox(boxCenter, boxSize / 2, _firingVector, Quaternion.LookRotation(_firingVector), MaxRange, Color.magenta, 1.0f);
        RaycastHit[] hits = Physics.BoxCastAll(boxCenter, boxSize / 2, _firingVector, Quaternion.LookRotation(_firingVector), MaxRange, ObjectFactory.AllTargetableLayerMask);
        int closestHit = -1;
        Ship hitShip = null;
        for (int i = 0; i < hits.Length; ++i)
        {
            if (hits[i].collider.gameObject == ContainingShip.gameObject.gameObject || hits[i].collider.gameObject == ContainingShip.ShieldCapsule.gameObject)
            {
                continue;
            }
            Ship currHitShip = Ship.FromCollider(hits[i].collider);
            if (currHitShip != null && (closestHit < 0 || hits[i].distance < hits[closestHit].distance))
            {
                hitShip = currHitShip;
                closestHit = i;
            }
        }
        _beamRenderer.SetPosition(0, _beamOrigin);
        if (hitShip != null)
        {
            Vector3 hitLocation = _beamOrigin + _firingVector.normalized * (hitShip.transform.position - _beamOrigin).magnitude;
            _beamRenderer.SetPosition(1, hitLocation);
            hitShip.TakeHit(w, hitLocation);
        }
        else
        {
            _beamRenderer.SetPosition(1, Muzzles[_nextBarrel].position + _firingVector.normalized * MaxRange);
        }
    }

    protected Vector3 _firingVector, _beamOrigin;
    protected LineRenderer _beamRenderer;
}
