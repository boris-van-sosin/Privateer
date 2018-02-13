using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserInput : MonoBehaviour
{

	// Use this for initialization
	void Start()
    {

	}

    void Awake()
    {
        _backgroundLayerMask = LayerMask.GetMask("Background");
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 2000, _backgroundLayerMask))
        {
            ControlledShip.ManualTarget(hit.point);
            if (Input.GetMouseButtonDown(0))
            {
                ControlledShip.FireManual(hit.point);
            }
            if (Input.GetMouseButtonDown(1))
            {
                ControlledShip.SetRequiredHeading(hit.point);
            }
        }


        if (Input.GetKey(KeyCode.W))
        {
            ControlledShip.ApplyThrust();
        }
        else if (Input.GetKey(KeyCode.S))
        {
            ControlledShip.ApplyBraking();
        }
        if (Input.GetKey(KeyCode.A))
        {
            ControlledShip.ApplyTurning(true);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            ControlledShip.ApplyTurning(false);
        }
    }

    public Ship ControlledShip; // temporary
    private int _backgroundLayerMask = 0;
}
