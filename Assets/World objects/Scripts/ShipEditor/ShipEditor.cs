using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.UI;

public class ShipEditor : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        PopulateHulls();
        PopulateWeapons();
    }

    private void PopulateHulls()
    {
        ShipHullDefinition[] hulls = ObjectFactory.GetAllShipHulls().ToArray();
        float offset = 0.0f;
        for (int i = 0; i < hulls.Length; ++i)
        {
            RectTransform t = Instantiate(ButtonPrototype);

            t.SetParent(ShipHullsScrollViewContent, false);
            float height = t.rect.height;
            float pivotOffset = (1.0f - t.pivot.y) * height;
            t.anchoredPosition = new Vector2(t.anchoredPosition.x, offset + pivotOffset);
            offset -= height;

            TextMeshProUGUI textElem = t.GetComponentInChildren<TextMeshProUGUI>();
            string hullKey = hulls[i].HullName;
            textElem.text = string.Format("{0} - {1}", hulls[i].ShipType, hullKey);

            Button buttonElem = t.GetComponent<Button>();
            buttonElem.onClick.AddListener(() => SelectShipDummy(hullKey));

            ShipDummyInEditor shipDummy;
            Sprite objSprite;
            if (!_shipsCache.TryGetValue(hullKey, out shipDummy))
            {
                Debug.LogFormat("Createing hull {0}", hullKey);
                (shipDummy, objSprite) = CreateShipDummy(hullKey);
                _shipsCache[hullKey] = shipDummy;
            }
            else
            {
                objSprite = null;
            }
            shipDummy.ShipModel.gameObject.SetActive(false);

            Image img = t.Find("Image").GetComponent<Image>();
            img.sprite = objSprite;
        }

        FitScrollContent(ShipHullsScrollViewContent.GetComponent<RectTransform>(), offset);
    }

    private void FitScrollContent(RectTransform contentRect, float offset)
    {
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, -offset);
        ScrollRect scrollRect = ShipHullsScrollViewContent.GetComponentInParent<ScrollRect>();
        RectTransform viewportRect = scrollRect.viewport;
        if (viewportRect.rect.height < contentRect.rect.height)
        {
            RectTransform scrollbarRect = scrollRect.verticalScrollbar.GetComponent<RectTransform>();
            RectTransform tabRect = scrollRect.transform.parent.GetComponent<RectTransform>();
            tabRect.sizeDelta = new Vector2(tabRect.sizeDelta.x + scrollbarRect.rect.width, tabRect.sizeDelta.y);
        }
    }

    private void SelectShipDummy(string key)
    {
        if (null != _currShip)
        {
            for (int i = 0; i < _currShip.Value.Hardpoints.Count; ++i)
            {
                _currShip.Value.Hardpoints[i].Item3.gameObject.SetActive(false);
            }
            _currShip.Value.ShipModel.gameObject.SetActive(false);
        }
        ShipDummyInEditor s;
        if (_shipsCache.TryGetValue(key, out s))
        {
            s.ShipModel.gameObject.SetActive(true);
        }
        else
        {
            s = CreateShipDummy(key).Item1;
            _shipsCache[key] = s;
        }
        
        _currShip = s;
    }

    private (ShipDummyInEditor, Sprite) CreateShipDummy(string key)
    {
        Debug.LogFormat("Createing hull {0}", key);
        Transform s = ObjectFactory.CreateShipDummy(key);
        Sprite shipPhoto = ObjectFactory.GetObjectPhoto(s, true, ImageCam);
        s.position = new Vector3(-2.5f, 0, 0);
        TurretHardpoint[] hardpoints = s.GetComponentsInChildren<TurretHardpoint>();
        List<(TurretHardpoint, Collider, Transform)> editorHardpoints = new List<(TurretHardpoint, Collider, Transform)>(hardpoints.Length);
        for (int i = 0; i < hardpoints.Length; ++i)
        {
            GameObject hardpointObj = hardpoints[i].gameObject;
            Transform marker = Instantiate(HardpointMarkerPrototype);
            marker.transform.parent = hardpointObj.transform;
            marker.transform.localPosition = Vector3.zero;
            Collider coll = marker.GetComponent<Collider>();
            editorHardpoints.Add((hardpoints[i], coll, marker));
        }
        return (new ShipDummyInEditor() { ShipModel = s, Hardpoints = editorHardpoints }, shipPhoto);
    }

    private void PopulateWeapons()
    {
        IReadOnlyList<string> weapons = ObjectFactory.GetAllWeaponTypes();
        float offset = 0.0f;
        for (int i = 0; i < weapons.Count; ++i)
        {
            string weaponKey = weapons[i];
            Sprite s = ObjectFactory.GetWeaponImage(weaponKey);
            if (s == null)
            {
                continue;
            }
            RectTransform t = Instantiate(ButtonPrototype);

            t.SetParent(WeaponsScrollViewContent, false);
            float height = t.rect.height;
            float pivotOffset = (1.0f - t.pivot.y) * height;
            t.anchoredPosition = new Vector2(t.anchoredPosition.x, offset + pivotOffset);
            offset -= height;

            TextMeshProUGUI textElem = t.GetComponentInChildren<TextMeshProUGUI>();
            textElem.text = weaponKey;

            Image img = t.Find("Image").GetComponent<Image>();
            img.sprite = s;

            ShipEditorDraggable draggable = t.gameObject.AddComponent<ShipEditorDraggable>();
            draggable.ContainingEditor = this;
            draggable.Item = EditorItemType.Weapon;
            draggable.WeaponKey = weaponKey;
        }

        FitScrollContent(WeaponsScrollViewContent.GetComponent<RectTransform>(), offset);
    }

    public void StartDragItem(ShipEditorDraggable draggedItem)
    {
        switch (draggedItem.Item)
        {
            case EditorItemType.ShipComponent:
                break;
            case EditorItemType.Weapon:
                {
                    if (_currShip.HasValue)
                    {
                        for (int i = 0; i < _currShip.Value.Hardpoints.Count; ++i)
                        {
                            Debug.LogFormat("Allowed weapon types: {0}", string.Join(",", _currShip.Value.Hardpoints[i].Item1.AllowedWeaponTypes));
                        }
                    }
                }
                break;
            case EditorItemType.Ammo:
                break;
            case EditorItemType.TurretMod:
                break;
            default:
                break;
        }
    }

    public RectTransform ShipClassesScrollViewContent;
    public RectTransform ShipHullsScrollViewContent;
    public RectTransform WeaponsScrollViewContent;
    public RectTransform ButtonPrototype;
    public Transform HardpointMarkerPrototype;
    public Camera ImageCam;

    private ShipDummyInEditor? _currShip = null;
    private Dictionary<string, ShipDummyInEditor> _shipsCache = new Dictionary<string, ShipDummyInEditor>();

    private struct ShipDummyInEditor
    {
        public Transform ShipModel;
        public List<(TurretHardpoint, Collider, Transform)> Hardpoints;
    }

    public enum EditorItemType { ShipComponent, Weapon, Ammo, TurretMod }
}
