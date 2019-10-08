using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SelectionHandler
{
    public void ClickSelect(Collider colliderHit)
    {
        if (colliderHit != null)
        {
            ShipBase s2 = ShipBase.FromCollider(colliderHit);
            if (s2 != null && s2 is Ship)
            {
                ClearSelection();
                _fleetSelectedShips.Add(s2);
                s2.SetCircleSelectStatus(true);
            }
        }
        else
        {
            ClearSelection();
        }
    }

    public void BoxSelect(Vector3 Corner1, Vector3 Corner2)
    {
        ClearSelection();
        Vector3 boxCenter = (Corner2 + Corner1) / 2f;
        Vector3 boxExt =
            new Vector3(Mathf.Abs(Corner2.x - Corner1.x) / 2,
                        1,
                        Mathf.Abs(Corner2.z - Corner1.z) / 2);
        Collider[] boxedColliders = Physics.OverlapBox(boxCenter, boxExt, Quaternion.identity, ObjectFactory.NavBoxesLayerMask);
        foreach (Collider c in boxedColliders)
        {
            ShipBase s2 = ShipBase.FromCollider(c);
            if (s2 != null && s2 is Ship)
            {
                _fleetSelectedShips.Add(s2);
                s2.SetCircleSelectStatus(true);
            }
        }
    }

    public void ClickOrder(Vector3 target)
    {
        foreach (Ship s2 in _fleetSelectedShips)
        {
            ShipAIController controller = s2.GetComponent<ShipAIController>();
            if (controller != null && !s2.ShipDisabled && !s2.ShipSurrendered)
            {
                controller.NavigateTo(target);
            }
        }
    }

    private void ClearSelection()
    {
        foreach (ShipBase sb in _fleetSelectedShips)
        {
            sb.SetCircleSelectStatus(false);
        }
        _fleetSelectedShips.Clear();
    }

    private List<ShipBase> _fleetSelectedShips = new List<ShipBase>();
}
