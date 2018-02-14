using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ObjectFactory
{
    public static void SetPrototypes(ObjectPrototypes p)
    {
        if (_prototypes == null)
        {
            _prototypes = p;
        }
    }

    public static Projectile CreateProjectile(Vector3 firingVector, float velocity, float range, Ship origShip)
    {
        if (_prototypes != null)
        {
            return _prototypes.CreateProjectile(firingVector, velocity, range, origShip);
        }
        else
        {
            return null;
        }
    }

    public static ParticleSystem CreateExplosion(Vector3 position)
    {
        if (_prototypes != null)
        {
            return _prototypes.CreateExplosion(position);
        }
        else
        {
            return null;
        }
    }

    private static ObjectPrototypes _prototypes = null;
}
