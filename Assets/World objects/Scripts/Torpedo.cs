using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Torpedo : MonoBehaviour
{
	// Use this for initialization
	void Start ()
    {
		
	}

    void Awake()
    {
        _targetQueue.Push(Target);
    }

    // Update is called once per frame
    void Update ()
    {
        Vector3 vecToTarget = (Target - transform.position);
        if (vecToTarget.sqrMagnitude < Time.deltaTime * Speed)
        {

        }
        else
        {
            Debug.DrawLine(transform.position, Target, Color.red, Time.deltaTime);
        }
        Quaternion rotToTarget = Quaternion.FromToRotation(transform.up, vecToTarget);
        if (transform.position.y > _altEpsilon)
        {
            Quaternion rotDown = Quaternion.AngleAxis(-TurnRate, transform.right);
            float factor = Mathf.Exp(-transform.position.y);
            //rotToTarget = Quaternion.Lerp(rotDown, rotToTarget, factor);
        }
        else if (transform.position.y < - _altEpsilon)
        {
            Quaternion rotDown = Quaternion.AngleAxis(TurnRate, transform.right);
            float factor = Mathf.Exp(transform.position.y);
            //rotToTarget = Quaternion.Lerp(rotDown, rotToTarget, factor);
        }
        Vector3 rotAxis;
        float rotAngle;
        rotToTarget.ToAngleAxis(out rotAngle, out rotAxis);
        Quaternion actualRot = Quaternion.AngleAxis(Mathf.Min(rotAngle, Time.deltaTime * TurnRate), rotAxis);
        //Debug.Log(string.Format("Torpedo heading: {0} . Angle to target: {0}", transform.up, rotToTarget.eulerAngles));
        transform.rotation = actualRot * transform.rotation;
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
                //DoHit(hit, shipHit);
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

    public float Speed;
    public float TurnRate;
    public float Range;
    private readonly float _altEpsilon = 1e-3f;
    private float _distanceTraveled = 0.0f;
    private Vector3 Origin;
    public Ship OriginShip;
    private int _shipLayerMask;
    public Warhead ProjectileWarhead { get; set; }
    public ObjectFactory.WeaponEffect WeaponEffectKey { get; set; }
    public Vector3 Target;// { get; set; }
    private Stack<Vector3> _targetQueue = new Stack<Vector3>();
}
