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
    }

    // Update is called once per frame
    void Update()
    {
        float distanceToTravel = Time.deltaTime * Speed;
        transform.position += distanceToTravel * transform.up.normalized;
        _distanceTraveled += distanceToTravel;
        if (_distanceTraveled >= Range)
        {
            Destroy(gameObject);
        }
	}

    public float Speed;
    public float Range;
    private float _distanceTraveled = 0.0f;
    private Vector3 Origin;
    public Ship OriginShip;
}
