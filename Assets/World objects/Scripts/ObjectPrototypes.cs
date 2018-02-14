using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPrototypes : MonoBehaviour
{
    void Start()
    {
        ObjectFactory.SetPrototypes(this);
    }

    public Projectile CreateProjectile(Vector3 firingVector, float velocity, float range, Ship origShip)
    {
        Projectile res = Instantiate(ProjectileTemplate);
        Quaternion q = Quaternion.FromToRotation(res.transform.up, firingVector);
        res.transform.rotation = q;
        res.Speed = velocity;
        res.Range = range;
        res.OriginShip = origShip;
        return res;
    }

    public ParticleSystem CreateExplosion(Vector3 position)
    {
        ParticleSystem res = Instantiate(SmallExplosion);
        res.transform.position = position;
        ParticleSystem.MainModule m = res.main;
        m.playOnAwake = true;
        m.loop = false;
        
        return res;
    }


    public Projectile ProjectileTemplate;
    public ParticleSystem SmallExplosion; 
}
