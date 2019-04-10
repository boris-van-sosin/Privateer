using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(ShipBase))]
public class CarrierBehavior : MonoBehaviour
{
    private void Start()
    {
        _ship = GetComponent<ShipBase>();
    }

    public void LaunchDbg()
    {
        StartCoroutine(LaunchSequence("Fed Fighter"));
    }

    private IEnumerator LaunchSequence(string strikeCraftKey)
    {
        if (LaunchTransform.Length == 0)
        {
            yield break;
        }

        StrikeCraftFormation formation = ObjectFactory.CreateStrikeCraftFormation("Fighter Wing");

        formation.Owner = _ship.Owner;
        int i = 0;
        formation.transform.position = LaunchTransform[0].position;
        formation.transform.rotation = _ship.transform.rotation;
        foreach (Transform tr in formation.Positions)
        {
            StrikeCraft s = ObjectFactory.CreateStrikeCraftAndFitOut(strikeCraftKey);
            s.Owner = formation.Owner;
            s.transform.position = LaunchTransform[i].position;
            s.transform.rotation = _ship.transform.rotation;
            s.AddToFormation(formation);
            s.Activate();
            formation.MaxSpeed = s.MaxSpeed * 1.1f;
            formation.TurnRate = s.TurnRate * 0.5f;
            ++i;
            if (i >= LaunchTransform.Length)
            {
                i = 0;
                yield return new WaitForSeconds(2);
            }
        }
        yield return null;
    }

    public Transform[] LaunchTransform;
    public Transform[] RecoveryTransform;

    private ShipBase _ship;
}
