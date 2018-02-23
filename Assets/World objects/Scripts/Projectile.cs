using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{

	// Use this for initialization
	void Start()
    {
    }

    void Awake()
    {
        Origin = transform.position;
        _shipLayerMask = ~LayerMask.GetMask("Background");
    }

    // Update is called once per frame
    void Update()
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
                shipHit.TakeHit(ProjectileWarhead, hit.point);
                ParticleSystem ps = ObjectFactory.CreateExplosion(hit.point);
                ps.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                Destroy(ps.gameObject, 5.0f);
                Destroy(gameObject);
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
}
