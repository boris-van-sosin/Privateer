using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SelectionHandler
{
    public enum OrderType
    {
        Follow,
        Defend,
        Attack
    }

    public void ClickSelect(Collider colliderHit)
    {
        if (colliderHit != null)
        {
            ShipBase s2 = ShipBase.FromCollider(colliderHit);
            if (s2 != null && s2 is Ship)
            {
                ClearSelection();
                _fleetSelectedShips.Add(s2);
                AddSelectedShipCard((Ship)s2);
                s2.SetCircleSelectStatus(true);
            }
        }
        else
        {
            ClearSelection();
        }
    }

    private void AddSelectedShipCard(Ship s)
    {
        if (SelectedShipPanel != null)
        {
            SelectedShipCard card = ObjectFactory.AcquireShipCard(s);
            card.transform.SetParent(SelectedShipPanel);
            RectTransform cardRT = card.GetComponent<RectTransform>();
            float height = cardRT.rect.height;
            float pivotOffset = (1f - cardRT.pivot.y) * height;
            cardRT.anchoredPosition = new Vector2(cardRT.anchoredPosition.x, -pivotOffset);
            _selectedShipCards.Add(card);
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
        int numHits = Physics.OverlapBoxNonAlloc(boxCenter, boxExt, _collidersCache, Quaternion.identity, ObjectFactory.NavBoxesLayerMask);
        for (int i = 0; i < numHits; ++i)
        {
            ShipBase s2 = ShipBase.FromCollider(_collidersCache[i]);
            if (s2 != null && s2 is Ship)
            {
                _fleetSelectedShips.Add(s2);
                AddSelectedShipCard((Ship)s2);
                s2.SetCircleSelectStatus(true);
            }
        }
    }

    public void ClickOrder(Vector3 target)
    {
        foreach (ValueTuple<ShipBase, ShipAIHandle> s2 in ControllableShips())
        {
            Ship s2AsShip = s2.Item1 as Ship;
            if (s2.Item2.AIHandle.GetControlType(s2AsShip) == ShipAIController.ShipControlType.Manual)
            {
                s2.Item2.AIHandle.SetControlType(s2AsShip, ShipAIController.ShipControlType.SemiAutonomous);
            }
            s2.Item2.AIHandle.UserNavigateTo(s2AsShip, target);
        }
    }
    public void ClickOrder(ShipBase targetShip, OrderType order)
    {
        if (true && order == OrderType.Follow)
        {
            ShipBase prevShip = null;
            foreach (ValueTuple<ShipBase, ShipAIHandle> s2 in ControllableShips())
            {
                Ship s2AsShip = s2.Item1 as Ship;
                if (s2.Item2.AIHandle.GetControlType(s2AsShip) == ShipAIController.ShipControlType.Manual)
                {
                    s2.Item2.AIHandle.SetControlType(s2AsShip, ShipAIController.ShipControlType.SemiAutonomous);
                }
                if (prevShip == null)
                {
                    s2.Item2.AIHandle.Follow(s2AsShip, targetShip);
                }
                else
                {
                    s2.Item2.AIHandle.Follow(s2AsShip, prevShip);
                }
                prevShip = s2.Item1;
            }
        }
    }

    private void ClearSelection()
    {
        foreach (ShipBase sb in _fleetSelectedShips)
        {
            sb.SetCircleSelectStatus(false);
        }
        if (SelectedShipPanel != null)
        {
            foreach (SelectedShipCard card in _selectedShipCards)
            {
                ObjectFactory.ReleaseShipCard(card);
                card.transform.SetParent(null);
            }
            _selectedShipCards.Clear();
        }
        _fleetSelectedShips.Clear();
    }

    private IEnumerable<ValueTuple<ShipBase, ShipAIHandle>> ControllableShips()
    {
        foreach (Ship s2 in _fleetSelectedShips)
        {
            ShipAIHandle controller = s2.GetComponent<ShipAIHandle>();
            if (controller != null && !s2.ShipDisabled && !s2.ShipSurrendered)
            {
                yield return new ValueTuple<ShipBase, ShipAIHandle>(s2, controller);
            }
        }
    }

    private List<ShipBase> _fleetSelectedShips = new List<ShipBase>();
    private List<SelectedShipCard> _selectedShipCards = new List<SelectedShipCard>();

    public RectTransform SelectedShipPanel { get; set; }

    // Ugly optimization:
    private Collider[] _collidersCache = new Collider[1024];
}
