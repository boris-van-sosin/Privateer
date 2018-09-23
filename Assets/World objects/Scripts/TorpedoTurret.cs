using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorpedoTurret : TurretBase
{
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void SetDefaultAngle()
    {
        _defaultDirection = _containingShip.transform.InverseTransformDirection(transform.up);
    }

    protected override void FireInner(Vector3 firingVector)
    {
        throw new System.NotImplementedException();
    }

    protected override Vector3 GetFiringVector(Vector3 vecToTarget)
    {
        return vecToTarget - (Muzzles[_nextBarrel].right * Vector3.Dot(Muzzles[_nextBarrel].right, vecToTarget));
    }

    protected override void FireGrapplingToolInner(Vector3 firingVector)
    {
        throw new System.NotImplementedException();
    }

    protected override Ship AcquireTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, MaxRange * 1.05f);
        Ship foundTarget = null;
        foreach (Collider c in colliders)
        {
            Ship s = c.GetComponent<Ship>();
            if (s == null)
            {
                continue;
            }
            else if (s.ShipDisabled)
            {
                continue;
            }
            else if (s.ShipTotalShields > 0) // experimential: torpedoes do not affect shields
            {
                continue;
            }
            if (ContainingShip.Owner.IsEnemy(s.Owner))
            {
                foundTarget = s;
            }
        }
        return foundTarget;
    }

    public int TorpedoesInSpread;
}
