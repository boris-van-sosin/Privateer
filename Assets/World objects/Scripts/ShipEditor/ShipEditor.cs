using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class ShipEditor : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        PopulateHulls();
        PopulateWeapons();
        GetTurretDefs();
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
            if (_currShip.Value.Key != key)
            {
                for (int i = 0; i < _currHardpoints.Count; ++i)
                {
                    if (null != _currHardpoints[i].Item3)
                    {
                        Destroy(_currHardpoints[i].Item3.gameObject);
                    }
                }
                _currHardpoints.Clear();
            }
        }
        ShipDummyInEditor s;
        if (_shipsCache.TryGetValue(key, out s))
        {
            s.ShipModel.gameObject.SetActive(true);
            ShipPhotoUtil.PositionCameraToObject(ShipViewCam, s.ShipModel, 1.2f);
        }
        else
        {
            s = CreateShipDummy(key).Item1;
            _shipsCache[key] = s;
        }
        for (int i = 0; i < s.Hardpoints.Count; ++i)
        {
            _currHardpoints.Add((s.Hardpoints[i].Item1, null, null));
        }

        _currShip = s;
    }

    private (ShipDummyInEditor, Sprite) CreateShipDummy(string key)
    {
        Debug.LogFormat("Createing hull {0}", key);
        Transform s = ObjectFactory.CreateShipDummy(key);
        Sprite shipPhoto = ObjectFactory.GetObjectPhoto(s, true, ImageCam);
        s.position = Vector3.zero;
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
            marker.gameObject.SetActive(false);
        }
        return (new ShipDummyInEditor() { Key = key, ShipModel = s, Hardpoints = editorHardpoints }, shipPhoto);
    }

    private void PopulateWeapons()
    {
        IReadOnlyList<(string, string)> weapons = ObjectFactory.GetAllWeaponTypesAndSizes();
        float offset = 0.0f;
        for (int i = 0; i < weapons.Count; ++i)
        {
            string weaponSizeKey = weapons[i].Item2;
            Sprite szSprite = ObjectFactory.GetWeaponSizeImage(weaponSizeKey);
            string weaponKey = weapons[i].Item1;
            Sprite s, weaponSprite = ObjectFactory.GetWeaponImage(weaponKey);
            if (weaponSprite == null)
            {
                continue;
            }
            if (szSprite != null)
            {
                s = CombineSpriteTextures(weaponSprite, szSprite);
            }
            else
            {
                s = weaponSprite;
            }

            RectTransform t = Instantiate(ButtonPrototype);

            t.SetParent(WeaponsScrollViewContent, false);
            float height = t.rect.height;
            float pivotOffset = (1.0f - t.pivot.y) * height;
            t.anchoredPosition = new Vector2(t.anchoredPosition.x, offset + pivotOffset);
            offset -= height;

            TextMeshProUGUI textElem = t.GetComponentInChildren<TextMeshProUGUI>();
            textElem.text = string.Format("{0} {1}", weaponSizeKey, weaponKey);

            Image img = t.Find("Image").GetComponent<Image>();
            img.sprite = s;

            ShipEditorDraggable draggable = t.gameObject.AddComponent<ShipEditorDraggable>();
            draggable.ContainingEditor = this;
            draggable.Item = EditorItemType.Weapon;
            draggable.WeaponSize = weaponSizeKey;
            draggable.WeaponKey = weaponKey;
        }

        FitScrollContent(WeaponsScrollViewContent.GetComponent<RectTransform>(), offset);
    }

    private Sprite CombineSpriteTextures(Sprite s1, Sprite s2)
    {
        Texture2D t1 = s1.texture;
        Texture2D t2 = s2.texture;
        Texture2D res = new Texture2D(Mathf.RoundToInt(s1.rect.width), Mathf.RoundToInt(s1.rect.height));

        int endX = Mathf.RoundToInt(s2.rect.width);
        int startY = Mathf.RoundToInt(s1.rect.height) - Mathf.RoundToInt(s2.rect.height);
        int offsetX1 = Mathf.RoundToInt(s1.rect.x), offsetY1 = Mathf.RoundToInt(s1.rect.y),
            offsetX2 = Mathf.RoundToInt(s2.rect.x), offsetY2 = Mathf.RoundToInt(s2.rect.y);

        for (int x = 0; x < res.width; x++)
        {
            for (int y = 0; y < res.height; y++)
            {
                Color resColor;
                if (x <= endX && y >= startY)
                {
                    Color t1Color = t1.GetPixel(offsetX1 + x, offsetY1 + y);
                    Color t2Color = t2.GetPixel(offsetX2 + x, offsetY2 + y);
                    resColor = new Color(Mathf.Lerp(t1Color.r, t2Color.r, t2Color.a),
                                         Mathf.Lerp(t1Color.g, t2Color.g, t2Color.a),
                                         Mathf.Lerp(t1Color.b, t2Color.b, t2Color.a),
                                         Mathf.Max(t1Color.a, t2Color.a));
                }
                else
                {
                    resColor = t1.GetPixel(offsetX1 + x, offsetY1 + y);
                }
                res.SetPixel(x, y, resColor);
            }
        }

        res.Apply();
        return Sprite.Create(res, new Rect(0, 0, res.width, res.height), Vector2.zero);
    }

    private void GetTurretDefs()
    {
        _allTurretDefs = ObjectFactory.GetAllTurretTypes().ToArray();
    }

    private TurretDefinition TryMatchTurretDef(string[] hardpointAllowedSlots, string weaponType, string weaponSize)
    {
        return TryMatchTurretDef(hardpointAllowedSlots, weaponType, weaponSize, 0);
    }

    private TurretDefinition TryMatchTurretDef(string[] hardpointAllowedSlots, string weaponType, string weaponSize, int weaponNumAbove)
    {
        TurretDefinition res = null;
        int minWeaponNum = -1;
        for (int i = 0; i < _allTurretDefs.Length; ++i)
        {
            if (_allTurretDefs[i].WeaponType == weaponType && _allTurretDefs[i].WeaponSize == weaponSize)
            {
                if (!hardpointAllowedSlots.Contains(string.Format("{0}{1}{2}", _allTurretDefs[i].TurretType, _allTurretDefs[i].WeaponNum, _allTurretDefs[i].WeaponSize)))
                {
                    continue;
                }

                // Try to find the turret with the minimum number of weapons:
                int currWeaponNum;
                if (int.TryParse(_allTurretDefs[i].WeaponNum, out currWeaponNum))
                {
                    if (currWeaponNum <= weaponNumAbove)
                    {
                        continue;
                    }
                    if (minWeaponNum < 0 || currWeaponNum < minWeaponNum)
                    {
                        minWeaponNum = currWeaponNum;
                        res = _allTurretDefs[i];
                    }
                }
                else
                {
                    return _allTurretDefs[i];
                }
            }
        }
        return res;
    }

    public void StartDragItem(ShipEditorDraggable item)
    {
        switch (item.Item)
        {
            case EditorItemType.ShipComponent:
                break;
            case EditorItemType.Weapon:
                {
                    if (_currShip.HasValue)
                    {
                        bool drawnPenChard = false;
                        for (int i = 0; i < _currShip.Value.Hardpoints.Count; ++i)
                        {
                            string[] allowedTurrets = _currShip.Value.Hardpoints[i].Item1.AllowedWeaponTypes;
                            TurretDefinition turretDef = TryMatchTurretDef(allowedTurrets, item.WeaponKey, item.WeaponSize);
                            _currShip.Value.Hardpoints[i].Item3.gameObject.SetActive(turretDef != null);
                            if (!drawnPenChard && turretDef != null)
                            {
                                switch (turretDef.BehaviorType)
                                {
                                    case ObjectFactory.WeaponBehaviorType.Gun:
                                        SetArmourPenetartionChartGun(item.WeaponKey, item.WeaponSize, "ShapedCharge");
                                        drawnPenChard = true;
                                        break;
                                    case ObjectFactory.WeaponBehaviorType.Torpedo:
                                    case ObjectFactory.WeaponBehaviorType.BomberTorpedo:
                                        SetArmourPenetartionChartTorpedo("Heavy");
                                        drawnPenChard = true;
                                        break;
                                    case ObjectFactory.WeaponBehaviorType.Beam:
                                    case ObjectFactory.WeaponBehaviorType.ContinuousBeam:
                                    case ObjectFactory.WeaponBehaviorType.Special:
                                        SetArmourPenetartionChartOther(item.WeaponKey, item.WeaponSize);
                                        drawnPenChard = true;
                                        break;
                                    default:
                                        break;
                                }
                            }
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

    public void DropItem(ShipEditorDraggable item, PointerEventData eventData)
    {
        switch (item.Item)
        {
            case EditorItemType.ShipComponent:
                break;
            case EditorItemType.Weapon:
                {
                    if (_currShip.HasValue)
                    {
                        PlaceWeapon(item, eventData);
                        for (int i = 0; i < _currShip.Value.Hardpoints.Count; ++i)
                        {
                            _currShip.Value.Hardpoints[i].Item3.gameObject.SetActive(false);
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

    private void PlaceWeapon(ShipEditorDraggable item, PointerEventData eventData)
    {
        int hardpointIdx = RaycastDropWeapon(eventData.position);
        if (hardpointIdx >= 0)
        {
            TurretHardpoint hardpoint = _currShip.Value.Hardpoints[hardpointIdx].Item1;
            TurretDefinition turretDef;
            int prevWeaponNum;
            if (null != _currHardpoints[hardpointIdx].Item3 &&
                _currHardpoints[hardpointIdx].Item2.WeaponType == item.WeaponKey && _currHardpoints[hardpointIdx].Item2.WeaponSize == item.WeaponSize &&
                int.TryParse(_currHardpoints[hardpointIdx].Item2.WeaponNum, out prevWeaponNum))
            {

                turretDef = TryMatchTurretDef(hardpoint.AllowedWeaponTypes, item.WeaponKey, item.WeaponSize, prevWeaponNum);
            }
            else
            {
                turretDef = TryMatchTurretDef(hardpoint.AllowedWeaponTypes, item.WeaponKey, item.WeaponSize);
            }

            if (turretDef != null)
            {
                // Remove any existing weapon
                if (null != _currHardpoints[hardpointIdx].Item3)
                {
                    Destroy(_currHardpoints[hardpointIdx].Item3.gameObject);
                }

                // Place the new weapon
                Transform dummyTurret = ObjectFactory.CreateTurretDummy(turretDef.TurretType, turretDef.WeaponNum, item.WeaponSize, item.WeaponKey);
                dummyTurret.parent = hardpoint.transform;
                dummyTurret.localScale = Vector3.one;
                dummyTurret.localPosition = Vector3.zero;
                Quaternion q = Quaternion.LookRotation(-hardpoint.transform.up, hardpoint.transform.forward);
                dummyTurret.transform.rotation = q;
                _currHardpoints[hardpointIdx] = (hardpoint, turretDef, dummyTurret);
            }
        }
    }

    private int RaycastDropWeapon(Vector3 clickPosition)
    {
        if (!_currShip.HasValue)
        {
            return -1;
        }
        // Transform the click from world space to the local space of the RenderTexture:
        Vector3 posInImg = ShipViewBox.rectTransform.InverseTransformPoint(clickPosition);

        // Adjust to the different origin and direction of the image:
        Vector3 size = new Vector3(ShipViewCam.targetTexture.width, ShipViewCam.targetTexture.height, 0);
        Vector3 adjustedPos = new Vector3(posInImg.x, size.y + posInImg.y, posInImg.z);
        //Debug.LogFormat("Drop poisition (relative): {0}. Adjusted: {1}", posInImg, adjustedPos);

        //with this knowledge we can creata a ray.
        Ray portaledRay = ShipViewCam.ScreenPointToRay(adjustedPos);
        
        _doDrawDebugRaycast = true;
        _debugRaycast = (portaledRay.origin, portaledRay.origin + portaledRay.direction.normalized * 10f);

        //and cast it.
        int numHits = Physics.RaycastNonAlloc(portaledRay, _raycastBuf);
        for (int i = 0; i < numHits; ++i)
        {
            Debug.LogFormat("Hit object {0}", _raycastBuf[i].collider.gameObject);
            for (int j = 0; j < _currShip.Value.Hardpoints.Count; j++)
            {
                if (_currShip.Value.Hardpoints[j].Item3 == _raycastBuf[i].collider.transform)
                {
                    return j;
                }
            }
        }
        return -1;
    }

    private void OnDrawGizmos()
    {
        if (_doDrawDebugRaycast)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_debugRaycast.Item1, _debugRaycast.Item2);
        }
    }

    private void SetArmourPenetartionChartGun(string weaponType, string weaponSize, string ammoType)
    {
        SetArmourPenetartionChartInner(ObjectFactory.CreateWarhead(weaponType, weaponSize, ammoType));
    }

    private void SetArmourPenetartionChartTorpedo(string torpedoType)
    {
        SetArmourPenetartionChartInner(ObjectFactory.CreateWarhead(torpedoType));
    }

    private void SetArmourPenetartionChartOther(string weaponType, string weaponSize)
    {
        SetArmourPenetartionChartInner(ObjectFactory.CreateWarhead(weaponType, weaponSize));
    }

    private void SetArmourPenetartionChartInner(Warhead w)
    {
        ArmourPenetrationTable penetrationTable = ObjectFactory.GetArmourPenetrationTable();
        int minArmor = 0, maxArmor = 1000, samplePoints = 50;
        PenetrationGraph.DataPoints = new Vector2[samplePoints + 1];

        float fSamplePoints = samplePoints;
        for (int i = 0; i <= samplePoints; ++i)
        {
            float currStep = i / fSamplePoints;
            float penetrationProb = penetrationTable.PenetrationProbability(Mathf.RoundToInt(Mathf.Lerp(minArmor, maxArmor, currStep)), w.ArmourPenetration);
            PenetrationGraph.DataPoints[i] = new Vector2(currStep, penetrationProb);
        }
        PenetrationGraph.RequireUpdate();
    }

    public RectTransform ShipClassesScrollViewContent;
    public RectTransform ShipHullsScrollViewContent;
    public RectTransform WeaponsScrollViewContent;
    public AreaGraphRenderer PenetrationGraph;
    public RectTransform ButtonPrototype;
    public Transform HardpointMarkerPrototype;
    public Camera ImageCam;
    public Camera ShipViewCam;
    public RawImage ShipViewBox;

    private ShipDummyInEditor? _currShip = null;
    List<(TurretHardpoint, TurretDefinition, Transform)> _currHardpoints = new List<(TurretHardpoint, TurretDefinition, Transform)>();
    private Dictionary<string, ShipDummyInEditor> _shipsCache = new Dictionary<string, ShipDummyInEditor>();
    [NonSerialized]
    private TurretDefinition[] _allTurretDefs;
    private RaycastHit[] _raycastBuf = new RaycastHit[100];
    private bool _doDrawDebugRaycast = false;
    private (Vector3, Vector3) _debugRaycast;

    private struct ShipDummyInEditor
    {
        public string Key;
        public Transform ShipModel;
        public List<(TurretHardpoint, Collider, Transform)> Hardpoints;
    }

    public enum EditorItemType { ShipComponent, Weapon, Ammo, TurretMod }
}
