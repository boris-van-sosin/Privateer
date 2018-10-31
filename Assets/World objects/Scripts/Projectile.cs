using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    protected virtual void Awake()
    {
        _shipLayerMask = ~LayerMask.GetMask("Background");
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
        Ray r = new Ray(posFlat, dirFlat);
        RaycastHit hit;
        if (Physics.Raycast(r, out hit, distanceToTravel, _shipLayerMask))
        {
            GameObject hitobj = hit.collider.gameObject;
            Ship shipHit;
            if (hitobj.GetComponent<Projectile>() == null)
            {
                shipHit = hitobj.GetComponent<Ship>();
                if (shipHit == null)
                {
                    shipHit = hitobj.GetComponentInParent<Ship>();
                }
                if (shipHit == null || shipHit == OriginShip)
                {
                    return;
                }
                //Debug.Log("Hit " + hit.collider.gameObject.ToString());
                DoHit(hit, shipHit);
                return;
            }
        }
        transform.position += distanceToTravel * transform.up.normalized;
        _distanceTraveled += distanceToTravel;
        if (_distanceTraveled >= Range)
        {
            Destroy(gameObject);
        }
	}

    protected virtual void DoHit(RaycastHit hit, Ship shipHit)
    {
        shipHit.TakeHit(ProjectileWarhead, hit.point);
        ParticleSystem ps = ObjectFactory.CreateWeaponEffect(WeaponEffectKey, hit.point);
        if (ps != null)
        {
            ps.transform.localScale = new Vector3(ProjectileWarhead.WeaponEffectScale, ProjectileWarhead.WeaponEffectScale, ProjectileWarhead.WeaponEffectScale);
            Destroy(ps.gameObject, 5.0f);
        }
        Destroy(gameObject);
    }

    public void SetScale(float sc)
    {
        transform.localScale = new Vector3(sc, 2 * sc, sc);
        if (_trail != null)
        {
            _trail.widthMultiplier = (sc * _trailWidthFactor);
        }
    }

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
    public Ship OriginShip;
    private int _shipLayerMask;
    public Warhead ProjectileWarhead { get; set; }
    public ObjectFactory.WeaponEffect WeaponEffectKey { get; set; }
    private TrailRenderer _trail;
    private static float _trailWidthFactor = 1.0f; // 1.0f/0.01f;
}
