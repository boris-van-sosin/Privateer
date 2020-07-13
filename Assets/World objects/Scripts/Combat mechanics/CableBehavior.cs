using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableBehavior : MonoBehaviour
{
    //Objects that will interact with the rope
    public Transform WhatTheRopeIsConnectedTo;
    public Transform WhatIsHangingFromTheRope;

    //Line renderer used to display the rope
    private LineRenderer lineRenderer;

    //A list with all rope sections
    private List<Vector3> allRopeSections = new List<Vector3>();

    //Rope data
    private float ropeLength = 1f;
    public float MinRopeLength;
    public float MaxRopeLength;
    //Mass of what the rope is carrying
    private float loadMass = 0f;

    //The joint we use to approximate the rope
    private SpringJoint springJoint;
    private FixedJoint fixedJoint;

    void Awake()
    {
        springJoint = GetComponent<SpringJoint>();
        fixedJoint = GetComponent<FixedJoint>();
        //Init the line renderer we use to display the rope
        lineRenderer = GetComponent<LineRenderer>();

        springJoint.spring = 1e+18f;
        springJoint.damper = 1e+8f;
    }

    public void Connect(Rigidbody whatTheRopeIsConnectedTo, Rigidbody whatIsHangingFromTheRope)
    {
        Connect(whatTheRopeIsConnectedTo, whatIsHangingFromTheRope, whatIsHangingFromTheRope.transform.position);
    }

    public void Connect(Rigidbody whatTheRopeIsConnectedTo, Rigidbody whatIsHangingFromTheRope, Vector3 targetConnectionPoint)
    {

        WhatTheRopeIsConnectedTo = whatTheRopeIsConnectedTo.transform;
        WhatIsHangingFromTheRope = whatIsHangingFromTheRope.transform;

        transform.position = WhatTheRopeIsConnectedTo.position;
        fixedJoint.connectedBody = whatTheRopeIsConnectedTo;
        springJoint.connectedBody = whatIsHangingFromTheRope;
        springJoint.connectedAnchor = WhatIsHangingFromTheRope.InverseTransformPoint(targetConnectionPoint);
        ropeLength = (WhatTheRopeIsConnectedTo.position - WhatIsHangingFromTheRope.position).magnitude;
        //Init the spring we use to approximate the rope from point a to b
        UpdateSpring();

        //Add the weight to what the rope is carrying
        loadMass = WhatIsHangingFromTheRope.GetComponent<Rigidbody>().mass;
    }

    void Update()
    {
        //Add more/less rope
        UpdateWinch();

        //Display the rope with a line renderer
        DisplayRope();
    }

    //Update the spring constant and the length of the spring
    private void UpdateSpring()
    {
        //Add the value to the spring
        //springJoint.spring = 1e+18f;
        //springJoint.damper = 1e+8f;

        //Update length of the rope
        springJoint.maxDistance = ropeLength;
    }

    //Display the rope with a line renderer
    private void DisplayRope()
    {
        //Update the list with rope sections by approximating the rope with a bezier curve
        //A Bezier curve needs 4 control points
        Vector3 A = WhatTheRopeIsConnectedTo.position;
        Vector3 D = WhatIsHangingFromTheRope.TransformPoint(springJoint.connectedAnchor);

        //Upper control point
        //To get a little curve at the top than at the bottom
        //Vector3 B = A + WhatTheRopeIsConnectedTo.up * (-(A - D).magnitude * 0.1f);
        //B = A;

        //Lower control point
        //Vector3 C = D + WhatIsHangingFromTheRope.up * ((A - D).magnitude * 0.5f);

        //Get the positions
        //BezierCurve.GetBezierCurve(A, B, C, D, allRopeSections);
        // tmp:
        allRopeSections.Clear();
        allRopeSections.Add(A);
        allRopeSections.Add(D);

        //An array with all rope section positions
        Vector3[] positions = new Vector3[allRopeSections.Count];

        for (int i = 0; i < allRopeSections.Count; i++)
        {
            positions[i] = allRopeSections[i];
        }

        //Just add a line between the start and end position for testing purposes
        //Vector3[] positions = new Vector3[2];

        //positions[0] = whatTheRopeIsConnectedTo.position;
        //positions[1] = whatIsHangingFromTheRope.position;


        //Add the positions to the line renderer
        lineRenderer.positionCount = positions.Length;

        lineRenderer.SetPositions(positions);
    }

    //Add more/less rope
    private void UpdateWinch()
    {
        bool hasChangedRope = false;

        //More rope
        if (Input.GetKey(KeyCode.O) && ropeLength < MaxRopeLength)
        {
            ropeLength += GlobalDistances.HarpaxCableWinchSpeed * Time.deltaTime;

            hasChangedRope = true;
        }
        else if (Input.GetKey(KeyCode.I) && ropeLength > MinRopeLength)
        {
            ropeLength -= GlobalDistances.HarpaxCableWinchSpeed * Time.deltaTime;

            hasChangedRope = true;
        }


        if (hasChangedRope)
        {
            ropeLength = Mathf.Clamp(ropeLength, MinRopeLength, MaxRopeLength);

            //Need to recalculate the k-value because it depends on the length of the rope
            UpdateSpring();
        }
    }

    public Vector3 TowVector
    {
        get
        {
            return (WhatIsHangingFromTheRope.position - WhatTheRopeIsConnectedTo.position).normalized;
        }
    }

    public void DisconnectAndDestroy()
    {
        Ship ship1 = WhatTheRopeIsConnectedTo.GetComponent<Ship>();
        Ship ship2 = WhatIsHangingFromTheRope.GetComponent<Ship>();
        if (ship1 != null && ship2 != null)
        {
            ship1.TowingByHarpax = null;
            ship2.TowedByHarpax = null;
        }
        Destroy(gameObject);
    }
}
