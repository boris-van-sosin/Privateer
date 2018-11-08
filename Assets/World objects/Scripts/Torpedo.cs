using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Torpedo : MonoBehaviour, ITargetableEntity
{
	// Use this for initialization
	void Start ()
    {
        _exhaustPatricleSystem = GetComponentInChildren<ParticleSystem>();
        _collider = GetComponent<Collider>();
        WeaponEffectKey = ObjectFactory.WeaponEffect.SmallExplosion;
        _targetReached = false;
        Targetable = true;
        Origin = transform.position;
    }

    void Awake()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (!_inBurnPhase)
        {
            float distanceToTravel = Time.deltaTime * ColdPhaseSpeed;
            transform.position += distanceToTravel * ColdLaunchVec;
            _distanceTraveled += distanceToTravel;
            Vector3 randRotAxis = Random.onUnitSphere;
            float rotAmount = Random.Range(0f, 5f);
            transform.rotation = Quaternion.AngleAxis(rotAmount, randRotAxis) * transform.rotation;
            if (_distanceTraveled >= ColdLaunchDist)
            {
                _inBurnPhase = true;
                _distanceTraveled = 0;
                if (_exhaustPatricleSystem)
                {
                    _exhaustPatricleSystem.Play();
                }
            }
            if (_collider != null && !_collider.enabled && _distanceTraveled >= OriginShip.ShipWidth)
            {
                _collider.enabled = true;
                Vector3 vecToTarget = (Target - Origin);
                vecToTarget.y = 0;
                Target = transform.position + vecToTarget.normalized * Range * 1.1f;
                Target.y = 0;
            }
        }
        else
        {
            Vector3 vecToTarget = (Target - transform.position);
            if (transform.position.y > _altEpsilon || transform.position.y < -_altEpsilon)
            {
                vecToTarget = vecToTarget.normalized * StepSize;
                Debug.DrawLine(transform.position, Target, Color.red, Time.deltaTime);
            }
            else
            {
                if (vecToTarget.sqrMagnitude < (Time.deltaTime * Time.deltaTime * Speed * Speed))
                {
                    _targetReached = true;
                }
            }
            if (!_targetReached)
            {
                Quaternion rotToTarget = Quaternion.FromToRotation(transform.up, vecToTarget);
                Vector3 rotAxis;
                float rotAngle;
                rotToTarget.ToAngleAxis(out rotAngle, out rotAxis);
                Quaternion actualRot = Quaternion.AngleAxis(Mathf.Min(rotAngle, Time.deltaTime * TurnRate), rotAxis);
                //Debug.Log(string.Format("Torpedo heading: {0} . Angle to target: {0}", transform.up, rotToTarget.eulerAngles));
                transform.rotation = actualRot * transform.rotation;
            }

            /*if (transform.position.y > _altEpsilon)
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
            }*/

            // Accelerate:
            if (Speed < MaxSpeed)
            {
                Speed = Mathf.Max(Speed + BurnAcceleration, MaxSpeed);
            }

            // Advance to target:
            float distanceToTravel = Time.deltaTime * Speed;

            Vector3 posFlat = new Vector3(transform.position.x, -0.01f, transform.position.z);
            Vector3 dirFlat = new Vector3(transform.up.x, 0, transform.up.z);
            Ray r = new Ray(posFlat, dirFlat);
            RaycastHit hit;
            if (Physics.Raycast(r, out hit, distanceToTravel, ObjectFactory.AllTargetableLayerMask))
            {
                GameObject hitobj = hit.collider.gameObject;
                Ship shipHit;
                if (hitobj.GetComponent<Projectile>() == null && hitobj.GetComponent<Torpedo>() == null)
                {
                    shipHit = Ship.FromCollider(hit.collider);
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
    }

    private void DoHit(RaycastHit hit, Ship shipHit)
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

    void OnDestroy()
    {
        Targetable = false;
    }

    // TargetableEntity properties:
    public Vector3 EntityLocation { get { return transform.position; } }
    public bool Targetable { get; private set; }

    public float BurnAcceleration;
    public float MaxSpeed;
    public float ColdPhaseSpeed;
    public float TurnRate;
    public float Range;
    public float StepSize;
    public float ColdLaunchDist;
    private readonly float _altEpsilon = 1e-3f;
    private float _distanceTraveled = 0.0f;
    //private Vector3 _lastVecToTarget;
    private bool _targetReached;
    private Vector3 Origin;
    public Ship OriginShip;
    private bool _inBurnPhase = false;
    public Warhead ProjectileWarhead { get; set; }
    public ObjectFactory.WeaponEffect WeaponEffectKey { get; set; }
    public Vector3 Target;// { get; set; }
    public Vector3 ColdLaunchVec { get; set; }
    public float Speed { get; set; }

    private ParticleSystem _exhaustPatricleSystem;
    private Collider _collider;
}
