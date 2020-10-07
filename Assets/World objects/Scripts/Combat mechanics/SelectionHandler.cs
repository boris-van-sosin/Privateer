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

    public void RegisterShip(Ship s)
    {
        if (_fleet.Any(x => x.SelectedShip == s))
        {
            Debug.LogWarningFormat("Attempted to add ship {0} to selection handler more than once.", s);
            return;
        }
        SelectedShipCard card = ObjectFactory.AcquireShipCard(s);
        if (card.StrikeCraftPanel == null)
        {
            card.transform.SetParent(SelectedShipPanel, false);
            card.StrikeCraftPanel = SelectedStrikeCraftPanel;
            card.ShipSelectionHandler = this;
        }
        ShipSelectionInfo shipItem = new ShipSelectionInfo()
        {
            SelectedShip = s,
            ShipCard = card,
            ShipAI = s.GetComponent<ShipAIHandle>(),
            Selected = false
        };
        _fleet.Add(shipItem);
    }

    public void ClickSelect(Collider colliderHit)
    {
        if (colliderHit != null)
        {
            ShipBase s2 = ShipBase.FromCollider(colliderHit);
            if (s2 != null && s2 is Ship)
            {
                ClearSelection();
                for (int j = 0; j < _fleet.Count; ++j)
                {
                    if (_fleet[j].SelectedShip == s2)
                    {
                        _fleet[j] = SelectDeSelectShip(_fleet[j], true);
                        break;
                    }
                }
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
        int numHits = Physics.OverlapBoxNonAlloc(boxCenter, boxExt, _collidersCache, Quaternion.identity, ObjectFactory.NavBoxesLayerMask);
        for (int i = 0; i < numHits; ++i)
        {
            ShipBase s2 = ShipBase.FromCollider(_collidersCache[i]);
            if (s2 != null && s2 is Ship)
            {
                for (int j = 0; j < _fleet.Count; ++j)
                {
                    if (_fleet[j].SelectedShip == s2)
                    {
                        _fleet[j] = SelectDeSelectShip(_fleet[j], true);
                        break;
                    }
                }
            }
        }
    }

    public void SelectDeSelectFromPanel(Ship s, bool select)
    {
        SelectDeSelectFromPanel(s, select, false);
    }

    public void SelectDeSelectFromPanel(Ship s, bool select, bool thisCardOnly)
    {
        for (int j = 0; j < _fleet.Count; ++j)
        {
            if (!thisCardOnly && _fleet[j].SelectedShip == s)
            {
                _fleet[j] = SelectDeSelectShip(_fleet[j], select, false);
                break;
            }
            else if (select && thisCardOnly)
            {
                _fleet[j] = SelectDeSelectShip(_fleet[j], _fleet[j].SelectedShip == s, false);
                if (_fleet[j].SelectedShip != s)
                {
                    _fleet[j].ShipCard.DeSelectCard();
                }
            }
        }
    }

    public void ClickOrder(Vector3 target)
    {
        foreach (ValueTuple<ShipBase, ShipAIHandle> s2 in ControllableShips())
        {
            if (s2.Item2.GetControlType() == ShipControlType.Manual)
            {
                s2.Item2.SetControlType(ShipControlType.SemiAutonomous);
            }
            s2.Item2.UserNavigateTo(target);
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
                if (s2.Item2.GetControlType() == ShipControlType.Manual)
                {
                    s2.Item2.SetControlType(ShipControlType.SemiAutonomous);
                }
                if (prevShip == null)
                {
                    s2.Item2.Follow(targetShip);
                }
                else
                {
                    s2.Item2.Follow(prevShip);
                }
                prevShip = s2.Item1;
            }
        }
    }

    private void ClearSelection()
    {
        for (int i = 0; i < _fleet.Count; ++i)
        {
            _fleet[i] = SelectDeSelectShip(_fleet[i], false);
        }
    }

    private static ShipSelectionInfo SelectDeSelectShip(ShipSelectionInfo item, bool select)
    {
        return SelectDeSelectShip(item, select, true);
    }

    private static ShipSelectionInfo SelectDeSelectShip(ShipSelectionInfo item, bool select, bool needToUpdatePanel)
    {
        ShipSelectionInfo res = new ShipSelectionInfo()
        {
            SelectedShip = item.SelectedShip,
            ShipAI = item.ShipAI,
            ShipCard = item.ShipCard,
            Selected = select
        };
        item.SelectedShip.SetCircleSelectStatus(select);
        if (select && needToUpdatePanel)
        {
            item.ShipCard.SelectCard();
        }
        else if (!select && needToUpdatePanel)
        {
            item.ShipCard.DeSelectCard();
        }
        return res;
    }

    private IEnumerable<ValueTuple<ShipBase, ShipAIHandle>> ControllableShips()
    {
        foreach (ShipSelectionInfo item in _fleet)
        {
            ShipAIHandle controller = item.ShipAI;
            if (item.Selected && controller != null && !item.SelectedShip.ShipDisabled && !item.SelectedShip.ShipSurrendered)
            {
                yield return new ValueTuple<ShipBase, ShipAIHandle>(item.SelectedShip, controller);
            }
        }
    }

    private List<ShipSelectionInfo> _fleet = new List<ShipSelectionInfo>();

    public RectTransform SelectedShipPanel { get; set; }
    public RectTransform SelectedStrikeCraftPanel { get; set; }

    // Ugly optimization:
    private Collider[] _collidersCache = new Collider[1024];

    private struct ShipSelectionInfo
    {
        public Ship SelectedShip;
        public SelectedShipCard ShipCard;
        public ShipAIHandle ShipAI;
        public bool Selected;
    }
}
