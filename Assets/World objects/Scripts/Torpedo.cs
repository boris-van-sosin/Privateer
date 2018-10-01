﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Torpedo : MonoBehaviour
{
	// Use this for initialization
	void Start ()
    {
        _exhaustPatricleSystem = GetComponentInChildren<ParticleSystem>();
        _collider = GetComponent<Collider>();
        _shipLayerMask = ~LayerMask.GetMask("Background");
        WeaponEffectKey = ObjectFactory.WeaponEffect.SmallExplosion;
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
                    vecToTarget = _lastVecToTarget;
                }
                else
                {
                    _lastVecToTarget = vecToTarget;
                }
            }
            Quaternion rotToTarget = Quaternion.FromToRotation(transform.up, vecToTarget);

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

            Vector3 rotAxis;
            float rotAngle;
            rotToTarget.ToAngleAxis(out rotAngle, out rotAxis);
            Quaternion actualRot = Quaternion.AngleAxis(Mathf.Min(rotAngle, Time.deltaTime * TurnRate), rotAxis);
            //Debug.Log(string.Format("Torpedo heading: {0} . Angle to target: {0}", transform.up, rotToTarget.eulerAngles));
            transform.rotation = actualRot * transform.rotation;

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
            if (Physics.Raycast(r, out hit, distanceToTravel, _shipLayerMask))
            {
                GameObject hitobj = hit.collider.gameObject;
                Ship shipHit;
                if (hitobj.GetComponent<Projectile>() == null && hitobj.GetComponent<Torpedo>() == null)
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
    }

    private void DoHit(RaycastHit hit, Ship shipHit)
    {
        shipHit.TakeHit(ProjectileWarhead, hit.point);
        ParticleSystem ps = ObjectFactory.CreateWeaponEffect(WeaponEffectKey, hit.point);
        if (ps != null)
        {
            ps.transform.localScale = ProjectileWarhead.WeaponEffectScale;
            Destroy(ps.gameObject, 5.0f);
        }
        Destroy(gameObject);
    }

    public float BurnAcceleration;
    public float MaxSpeed;
    public float ColdPhaseSpeed;
    public float TurnRate;
    public float Range;
    public float StepSize;
    public float ColdLaunchDist;
    private readonly float _altEpsilon = 1e-3f;
    private float _distanceTraveled = 0.0f;
    private Vector3 _lastVecToTarget;
    private Vector3 Origin;
    public Ship OriginShip;
    private bool _inBurnPhase = false;
    private int _shipLayerMask;
    public Warhead ProjectileWarhead { get; set; }
    public ObjectFactory.WeaponEffect WeaponEffectKey { get; set; }
    public Vector3 Target;// { get; set; }
    public Vector3 ColdLaunchVec { get; set; }
    public float Speed { get; set; }

    private ParticleSystem _exhaustPatricleSystem;
    private Collider _collider;
}