using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    protected virtual void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
    }

    protected virtual void Start()
    {
        Origin = transform.position;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        float distanceToTravel = Time.deltaTime * Speed;
        Vector3 posFlat = new Vector3(transform.position.x, -0.01f, transform.position.z);
        Vector3 dirFlat = new Vector3(transform.up.x, 0, transform.up.z);
        if (!ProximityProjectile && CheckForDirectHit(posFlat, dirFlat, distanceToTravel))
        {
            return;
        }
        else if (ProximityProjectile && CheckForProximityHit(posFlat, BlastRadius))
        {
            return;
        }
        transform.position += distanceToTravel * transform.up.normalized;
        _distanceTraveled += distanceToTravel;
        if (_distanceTraveled >= Range)
        {
            Destroy(gameObject);
        }
	}

    protected virtual void DoHit(RaycastHit hit, ShipBase shipHit)
    {
        shipHit.TakeHit(ProjectileWarhead, hit.point);
        ParticleSystem ps = ObjectFactory.AcquireParticleSystem(WeaponEffectKey.Item1, WeaponEffectKey.Item2, hit.point);
        if (ps != null)
        {
            ps.transform.localScale = new Vector3(ProjectileWarhead.WeaponEffectScale, ProjectileWarhead.WeaponEffectScale, ProjectileWarhead.WeaponEffectScale);
            ps.Play();
            ObjectFactory.ReleaseParticleSystem(WeaponEffectKey.Item1, WeaponEffectKey.Item2, ps, 2.0f);
        }
        Destroy(gameObject);
    }

    protected virtual void DoHit(RaycastHit hit, Torpedo torp)
    {
        ParticleSystem ps = ObjectFactory.AcquireParticleSystem(WeaponEffectKey.Item1, WeaponEffectKey.Item2, hit.point);
        if (ps != null)
        {
            ps.transform.localScale = new Vector3(ProjectileWarhead.WeaponEffectScale, ProjectileWarhead.WeaponEffectScale, ProjectileWarhead.WeaponEffectScale);
            ps.Play();
            ObjectFactory.ReleaseParticleSystem(WeaponEffectKey.Item1, WeaponEffectKey.Item2, ps, 2.0f);
        }
        Destroy(gameObject);
        Destroy(torp.gameObject); //TODO: replace with damage calculation
    }

    public void SetScale(float sc)
    {
        transform.localScale = new Vector3(sc, 2 * sc, sc);
        if (_trail != null)
        {
            _trail.widthMultiplier = (sc * _trailWidthFactor);
        }
    }

    private bool CheckForDirectHit(Vector3 pos, Vector3 dir, float distanceToTravel)
    {
        Ray r = new Ray(pos, dir);
        RaycastHit hit;
        if (Physics.Raycast(r, out hit, distanceToTravel, ObjectFactory.AllTargetableLayerMask))
        {
            GameObject hitobj = hit.collider.gameObject;
            ShipBase shipHit;
            if (hitobj.GetComponent<Projectile>() == null)
            {
                shipHit = ShipBase.FromCollider(hit.collider);
                if (shipHit != null && shipHit != OriginShip && IsHit(shipHit))
                {
                    DoHit(hit, shipHit);
                    return true;
                }
                else
                {
                    Torpedo torpHit = hitobj.GetComponent<Torpedo>();
                    if (torpHit != null && IsHit(torpHit))
                    {
                        DoHit(hit, torpHit);
                        return true;
                    }
                }
                return false;
            }
        }
        return false;
    }

    private bool CheckForProximityHit(Vector3 pos, float blastRadius)
    {
        Collider[] objsHits = Physics.OverlapSphere(pos, blastRadius, ObjectFactory.AllTargetableLayerMask);
        bool validHit = false;
        foreach (Collider c in objsHits)
        {
            ShipBase s;
            Torpedo t;
            if ((s = ShipBase.FromCollider(c)) != null)
            {
                //Debug.LogFormat("Proximity hit triggered on {0}. Shell location: {1} blast radius {2}", s, pos, blastRadius);
                if (s != OriginShip)
                {
                    if (IsHit(s))
                    {
                        Vector3 hitLocation = c.ClosestPoint(pos);
                        s.TakeHit(ProjectileWarhead, hitLocation);
                    }
                    validHit = true;
                }
            }
            else if ((t = c.GetComponent<Torpedo>())!=null)
            {
                //Debug.LogFormat("Proximity hit triggered on {0}. Shell location: {1} blast radius {2}", t, pos, blastRadius);
                validHit = true;
                if (IsHit(t))
                {
                    Destroy(t.gameObject); //TODO: replace with damage calculation
                }
            }
        }
        if (validHit)
        {
            ParticleSystem ps = ObjectFactory.AcquireParticleSystem(WeaponEffectKey.Item1, WeaponEffectKey.Item2, pos);
            if (ps != null)
            {
                ps.transform.localScale = new Vector3(ProjectileWarhead.WeaponEffectScale, ProjectileWarhead.WeaponEffectScale, ProjectileWarhead.WeaponEffectScale);
                ps.Play();
                ObjectFactory.ReleaseParticleSystem(WeaponEffectKey.Item1, WeaponEffectKey.Item2, ps, 2.0f);
            }
            Destroy(gameObject);
        }
        return validHit;
    }

    private bool IsHit(ShipBase s)
    {
        if (TargetableEntityUtils.IsTargetable(s.TargetableBy, TargetableEntityInfo.Flak))
        {
            float roll = Random.Range(0f, 1f);
            return (roll <= ProjectileWarhead.EffectVsStrikeCraft);
        }
        else
        {
            return true;
        }
    }

    private bool IsHit(Torpedo t)
    {
        float roll = Random.Range(0f, 1f);
        return (roll <= ProjectileWarhead.EffectVsStrikeCraft);
    }

    /*void OnDrawGizmos()
    {
        if (ProximityProjectile)
        {
            Vector3 posFlat = new Vector3(transform.position.x, -0.01f, transform.position.z);
            Gizmos.DrawWireSphere(posFlat, BlastRadius);
        }
    }*/

    /*void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.collider.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger: " + other.gameObject.ToString());
    }*/

    public float Speed;
    public float Range;
    private float _distanceTraveled = 0.0f;
    private Vector3 Origin;
    public ShipBase OriginShip;
    public Warhead ProjectileWarhead { get; set; }
    public (string, string) WeaponEffectKey { get; set; }
    private TrailRenderer _trail;
    private static float _trailWidthFactor = 1.0f; // 1.0f/0.01f;
    public bool ProximityProjectile;
    public float BlastRadius { get { return ProjectileWarhead.BlastRadius; } }
}
