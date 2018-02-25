using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeamTurret : TurretBase
{
    protected override void Awake()
    {
        base.Awake();
        _beamRenderer = GetComponentInChildren<LineRenderer>();
        _beamRenderer.positionCount = 2;
        _beamRenderer.useWorldSpace = true;
    }

    IEnumerator HandleBeam()
    {
        yield return new WaitForEndOfFrame();
        _beamRenderer.enabled = true;
        Warhead w = ObjectFactory.CreateWarhead(ObjectFactory.WeaponType.Lance, TurretSize);
        Vector3 boxCenter = _beamOrigin + 0.5f * MaxRange * _firingVector;
        Vector3 boxSize = new Vector3(0.01f, 0.3f, MaxRange);
        //ExtDebug.DrawBoxCastBox(boxCenter, boxSize / 2, _firingVector, Quaternion.LookRotation(_firingVector), MaxRange, Color.magenta, 1.0f);
        RaycastHit[] hits = Physics.BoxCastAll(boxCenter, boxSize / 2, _firingVector, Quaternion.LookRotation(_firingVector), MaxRange);
        int closestHit = -1;
        Ship hitShip = null;
        for (int i = 0; i < hits.Length; ++i)
        {
            if (hits[i].collider.gameObject == ContainingShip.gameObject.gameObject || hits[i].collider.gameObject == ContainingShip.ShieldCapsule.gameObject)
            {
                continue;
            }
            Ship currHitShip = hits[i].collider.GetComponent<Ship>();
            if (currHitShip == null)
            {
                currHitShip = hits[i].collider.GetComponentInParent<Ship>();
            }
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
        yield return new WaitForSeconds(BeamDuration);
        _beamRenderer.enabled = false;
        yield return null;
    }

    protected override void FireInner(Vector3 firingVector)
    {
        base.FireInner(firingVector);
        _firingVector = firingVector;
        _beamOrigin = Muzzles[_nextBarrel].position;
        StartCoroutine(HandleBeam());
    }

    public float BeamDuration;
    private Vector3 _firingVector, _beamOrigin;
    private LineRenderer _beamRenderer;
}
