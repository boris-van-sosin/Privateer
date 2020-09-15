using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class ShipEditor : MonoBehaviour, IDropHandler
{
    // Start is called before the first frame update
    void Start()
    {
        PopulateHulls();
        PopulateWeapons();
        PopulateShipComps();
        PopulateAmmo();
        GetTurretDefs();
        FilterWeapons(null);
        FilterComponents(null);
        FilterAmmo(null);
        InitShipSections();
    }

    private void PopulateHulls()
    {
        StackingLayout stackingBehavior = ShipHullsScrollViewContent.GetComponent<StackingLayout>();
        stackingBehavior.AutoRefresh = false;

        ShipHullDefinition[] hulls = ObjectFactory.GetAllShipHulls().ToArray();
        float offset = 0.0f;
        for (int i = 0; i < hulls.Length; ++i)
        {
            RectTransform t = Instantiate(ButtonPrototype);
            t.gameObject.AddComponent<StackableUIComponent>();

            t.SetParent(ShipHullsScrollViewContent, false);
            float height = t.rect.height;
            float pivotOffset = t.pivot.x * t.rect.width;
            t.anchoredPosition = new Vector2(pivotOffset, 0);
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
                shipDummy.HullDef = hulls[i];
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
        stackingBehavior.AutoRefresh = true;
    }

    private void FitScrollContent(RectTransform contentRect, float offset)
    {
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, -offset);
        return;
        ScrollRect scrollRect = contentRect.GetComponentInParent<ScrollRect>();
        if (scrollRect == null)
        {
            return;
        }
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
        bool needsReFilter = false;
        if (null != _currShip)
        {
            for (int i = 0; i < _currShip.Value.Hardpoints.Count; ++i)
            {
                _currShip.Value.Hardpoints[i].Item3.enabled = false;
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
                needsReFilter = true;
            }
        }
        else if (null == _currShip)
        {
            needsReFilter = true;
        }

        ShipDummyInEditor s;
        if (_shipsCache.TryGetValue(key, out s))
        {
            s.ShipModel.gameObject.SetActive(true);
            for (int i = 0; i < s.Hardpoints.Count; ++i)
            {
                s.Hardpoints[i].Item3.gameObject.SetActive(false);
            }
            ShipPhotoUtil.PositionCameraToObject(ShipViewCam, s.ShipModel, 1.05f);
            for (int i = 0; i < s.Hardpoints.Count; ++i)
            {
                s.Hardpoints[i].Item3.gameObject.SetActive(true);
            }
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
        SetShipSections(s.HullDef);

        _currShip = s;

        if (needsReFilter)
        {
            FilterWeapons(WeaponFilter);
            FilterComponents(ComponentsFilter);
        }
    }

    private (ShipDummyInEditor, Sprite) CreateShipDummy(string key)
    {
        Debug.LogFormat("Createing hull {0}", key);
        Transform s = ObjectFactory.CreateShipDummy(key);
        Sprite shipPhoto = ObjectFactory.GetObjectPhoto(s, true, ImageCam);
        s.position = Vector3.zero;
        TurretHardpoint[] hardpoints = s.GetComponentsInChildren<TurretHardpoint>();
        List<(TurretHardpoint, Collider, MeshRenderer)> editorHardpoints = new List<(TurretHardpoint, Collider, MeshRenderer)>(hardpoints.Length);
        for (int i = 0; i < hardpoints.Length; ++i)
        {
            GameObject hardpointObj = hardpoints[i].gameObject;
            Transform marker = Instantiate(HardpointMarkerPrototype);
            marker.transform.parent = hardpointObj.transform;
            marker.transform.localPosition = Vector3.zero;
            Collider coll = marker.GetComponent<Collider>();
            MeshRenderer mr = marker.GetComponent<MeshRenderer>();
            editorHardpoints.Add((hardpoints[i], coll, mr));
            mr.enabled = false;
        }
        return (new ShipDummyInEditor() { Key = key, ShipModel = s, Hardpoints = editorHardpoints }, shipPhoto);
    }

    private void PopulateWeapons()
    {
        StackingLayout stackingBehavior = WeaponsScrollViewContent.GetComponent<StackingLayout>();
        stackingBehavior.AutoRefresh = false;

        IReadOnlyList<(string, string)> weapons = ObjectFactory.GetAllWeaponTypesAndSizes();
        _allWeapons = new List<ShipEditorDraggable>(weapons.Count);
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
            t.gameObject.AddComponent<StackableUIComponent>();

            t.SetParent(WeaponsScrollViewContent, false);
            float height = t.rect.height;
            float pivotOffset = t.pivot.x * t.rect.width;
            t.anchoredPosition = new Vector2(pivotOffset, 0);
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
            draggable.CurrentLocation = EditorItemLocation.Production;
            _allWeapons.Add(draggable);
        }

        _allWeaponsComatibleFlags = new bool[_allWeapons.Count];

        FitScrollContent(WeaponsScrollViewContent.GetComponent<RectTransform>(), offset);
        stackingBehavior.AutoRefresh = true;
    }

    private void FilterWeapons(Func<ShipDummyInEditor, string, string, bool> filter)
    {
        float offset = 0.0f;
        List<ShipEditorDraggable> compItems = _allWeapons;
        bool[] compatible = _allWeaponsComatibleFlags;

        ShipComponentFilteringTag ft = ShipComponentFilteringTag.All;
        for (int i = 0; i < WeaponsDisplayToggleGroup.Length; i++)
        {
            if (WeaponsDisplayToggleGroup[i].isOn)
            {
                FilterTag ftComp = WeaponsDisplayToggleGroup[i].GetComponent<FilterTag>();
                ft = ftComp.FilteringTag;
                break;
            }
        }

        FilterAndSortInner(WeaponsScrollViewContent, x => (_currShip.HasValue && filter(_currShip.Value, x.WeaponKey, x.WeaponSize)), compItems, compatible, ft, out offset);

        FitScrollContent(WeaponsScrollViewContent.GetComponent<RectTransform>(), offset);
        WeaponsScrollViewContent.GetComponent<StackingLayout>().ForceRefresh();
    }

    private void PopulateShipComps()
    {
        StackingLayout stackingBehavior = ShipCompsScrollViewContent.GetComponent<StackingLayout>();
        stackingBehavior.AutoRefresh = false;

        IReadOnlyCollection<ShipComponentTemplateDefinition> allComps = ObjectFactory.GetAllShipComponents();
        _allShipComponents = new List<ShipEditorDraggable>(allComps.Count);
        float offset = 0.0f;
        int i = 0;
        int offsetModulus = 1;
        StackingLayout2D stacking2D;
        if (ShipCompsScrollViewContent.TryGetComponent<StackingLayout2D>(out stacking2D) && stacking2D.MaxFirstDirection > 0)
        {
            offsetModulus = stacking2D.MaxFirstDirection;
        }
        float areaWidth = ShipCompsScrollViewContent.rect.width;
        foreach (ShipComponentTemplateDefinition comp in allComps)
        {
            string componentType = comp.ComponentType;
            Sprite compSprite = ObjectFactory.GetShipComponentImage(comp.ComponentType);
            string componentKey = comp.ComponentName;
            if (compSprite == null)
            {
                continue;
            }

            RectTransform t = Instantiate(ButtonPrototype);
            //t.sizeDelta = new Vector2((0f / areaWidth) - t.pivot.x / 3, t.anchorMin.y);
            //t.anchorMax = new Vector2((1f / 3f) - t.pivot.x / 3, t.anchorMax.y);
            t.gameObject.AddComponent<StackableUIComponent>();

            t.SetParent(ShipCompsScrollViewContent, false);
            //float pivotOffset = (1.0f - t.pivot.y) * height;
            //t.anchoredPosition = new Vector2(t.anchoredPosition.x, offset + pivotOffset);
            if (i % offsetModulus == 0)
            {
                float height = t.rect.height;
                offset -= height;
            }

            TextMeshProUGUI textElem = t.GetComponentInChildren<TextMeshProUGUI>();
            textElem.text = comp.ComponentName.Replace(" - ", "\n");
            textElem.fontSize = 14;

            Image img = t.Find("Image").GetComponent<Image>();
            img.sprite = compSprite;

            ShipEditorDraggable draggable = t.gameObject.AddComponent<ShipEditorDraggable>();
            draggable.ContainingEditor = this;
            draggable.Item = EditorItemType.ShipComponent;
            draggable.ShipComponentDef = comp;
            draggable.CurrentLocation = EditorItemLocation.Production;
            _allShipComponents.Add(draggable);
            ++i;
        }

        _allShipComponentsComatibleFlags = new bool[_allShipComponents.Count];

        FitScrollContent(ShipCompsScrollViewContent.GetComponent<RectTransform>(), offset);
        stackingBehavior.AutoRefresh = true;
    }

    private void FilterComponents(Func<ShipDummyInEditor, ShipComponentTemplateDefinition, bool> filter)
    {
        float offset ;
        List<ShipEditorDraggable> compItems = _allShipComponents;
        bool[] compatible = _allShipComponentsComatibleFlags;

        ShipComponentFilteringTag ft = ShipComponentFilteringTag.All;
        for (int i = 0; i < ComponentDisplayToggleGroup.Length; i++)
        {
            if (ComponentDisplayToggleGroup[i].isOn)
            {
                FilterTag ftComp = ComponentDisplayToggleGroup[i].GetComponent<FilterTag>();
                ft = ftComp.FilteringTag;
                break;
            }
        }

        FilterAndSortInner(ShipCompsScrollViewContent, x => _currShip.HasValue && x.CurrentLocation != EditorItemLocation.Ship && filter(_currShip.Value, x.ShipComponentDef), compItems, compatible, ft, out offset);

        FitScrollContent(ShipCompsScrollViewContent.GetComponent<RectTransform>(), offset);
        ShipCompsScrollViewContent.GetComponent<StackingLayout>().ForceRefresh();
    }

    private void PopulateAmmo()
    {
        StackingLayout stackingBehavior = AmmoScrollViewContent.GetComponent<StackingLayout>();
        stackingBehavior.AutoRefresh = false;

        float offset = 0.0f;

        IReadOnlyList<string> gunAmmo = ObjectFactory.GetAllAmmoTypes(true, false);
        _allAmmoTypes = new List<ShipEditorDraggable>(gunAmmo.Count);
        for (int i = 0; i < gunAmmo.Count; ++i)
        {
            Sprite ammoSprite = ObjectFactory.GetAmmonImage(gunAmmo[i]);
            if (ammoSprite == null)
            {
                continue;
            }

            RectTransform t = Instantiate(ButtonPrototype);
            t.gameObject.AddComponent<StackableUIComponent>();

            t.SetParent(AmmoScrollViewContent, false);
            float height = t.rect.height;
            float pivotOffset = t.pivot.x * t.rect.width;
            t.anchoredPosition = new Vector2(pivotOffset, 0);
            offset -= height;

            TextMeshProUGUI textElem = t.GetComponentInChildren<TextMeshProUGUI>();
            string ammoKey = gunAmmo[i];
            textElem.text = gunAmmo[i];

            Image img = t.Find("Image").GetComponent<Image>();
            img.sprite = ammoSprite;

            ShipEditorDraggable draggable = t.gameObject.AddComponent<ShipEditorDraggable>();
            draggable.ContainingEditor = this;
            draggable.Item = EditorItemType.Ammo;
            draggable.AmmoTypeKey = ammoKey;
            _allAmmoTypes.Add(draggable);
        }

        IReadOnlyList<string> torpedoAmmo = ObjectFactory.GetAllAmmoTypes(false, true);
        for (int i = 0; i < gunAmmo.Count; ++i)
        {
            Sprite ammoSprite = ObjectFactory.GetTorpedoTypeImage(torpedoAmmo[i]);
            if (ammoSprite == null)
            {
                continue;
            }

            RectTransform t = Instantiate(ButtonPrototype);
            t.gameObject.AddComponent<StackableUIComponent>();

            t.SetParent(AmmoScrollViewContent, false);
            float height = t.rect.height;
            float pivotOffset = t.pivot.x * t.rect.width;
            t.anchoredPosition = new Vector2(pivotOffset, 0);
            offset -= height;

            TextMeshProUGUI textElem = t.GetComponentInChildren<TextMeshProUGUI>();
            string ammoKey = torpedoAmmo[i];
            textElem.text = torpedoAmmo[i];

            Image img = t.Find("Image").GetComponent<Image>();
            img.sprite = ammoSprite;

            ShipEditorDraggable draggable = t.gameObject.AddComponent<ShipEditorDraggable>();
            draggable.ContainingEditor = this;
            draggable.Item = EditorItemType.Ammo;
            draggable.AmmoTypeKey = ammoKey;
            _allAmmoTypes.Add(draggable);
        }

        _allAmmoTypesComatibleFlags = new bool[_allAmmoTypes.Count];

        FitScrollContent(AmmoScrollViewContent.GetComponent<RectTransform>(), offset);
        stackingBehavior.AutoRefresh = true;
    }

    private void FilterAmmo(Func<TurretDefinition, string, bool> filter)
    {
        float offset;
        List<ShipEditorDraggable> compItems = _allAmmoTypes;
        bool[] compatible = _allAmmoTypesComatibleFlags;

        ShipComponentFilteringTag ft = ShipComponentFilteringTag.All;
        for (int i = 0; i < WeaponsDisplayToggleGroup.Length; i++)
        {
            if (WeaponsDisplayToggleGroup[i].isOn)
            {
                FilterTag ftComp = WeaponsDisplayToggleGroup[i].GetComponent<FilterTag>();
                ft = ftComp.FilteringTag;
                break;
            }
        }

        FilterAndSortInner(AmmoScrollViewContent, x => (_currWeapon != null && filter(_currWeapon, x.AmmoTypeKey)), compItems, compatible, ft, out offset);

        FitScrollContent(AmmoScrollViewContent.GetComponent<RectTransform>(), offset);
        AmmoScrollViewContent.GetComponent<StackingLayout>().ForceRefresh();
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

    private void FilterAndSortInner(RectTransform scrollViewContentBox, Func<ShipEditorDraggable, bool> filter, List<ShipEditorDraggable> items, bool[] compatible, ShipComponentFilteringTag ft, out float totalHeight)
    {
        bool compatibleOnly = ft == ShipComponentFilteringTag.CompatibleOnly;

        totalHeight = 0;

        int offsetModulus = 1;
        int j = 0;
        StackingLayout2D stacking2D;
        if (scrollViewContentBox.TryGetComponent<StackingLayout2D>(out stacking2D) && stacking2D.MaxFirstDirection > 0)
        {
            offsetModulus = stacking2D.MaxFirstDirection;
        }
        float areaWidth = scrollViewContentBox.rect.width;

        // Filter:
        for (int i = 0; i < items.Count; ++i)
        {
            compatible[i] = filter(items[i]);

            items[i].GetComponent<Button>().interactable = compatible[i];
            if (ft == ShipComponentFilteringTag.CompatibleOnly)
            {
                items[i].gameObject.SetActive(compatible[i]);
            }
            else
            {
                items[i].gameObject.SetActive(true);
            }

            if (!compatibleOnly || compatible[i])
            {
                if (j % offsetModulus == 0)
                {
                    RectTransform rt = items[i].GetComponent<RectTransform>();
                    float height = rt.rect.height;
                    totalHeight -= height;
                }
                ++j;
            }
        }

        // Sort:
        if (ft == ShipComponentFilteringTag.CompatibleFirst)
        {
            int sortingIdx = 0;
            for (int i = 0; i < items.Count; ++i)
            {
                if (compatible[i])
                {
                    items[i].transform.SetSiblingIndex(sortingIdx++);
                }
            }
            for (int i = 0; i < items.Count; ++i)
            {
                if (!compatible[i])
                {
                    items[i].transform.SetSiblingIndex(sortingIdx++);
                }
            }
        }
        else
        {
            for (int i = 0; i < items.Count; ++i)
            {
                items[i].transform.SetSiblingIndex(i);
            }
        }
    }


    public void ForceReFilterComponent()
    {
        FilterComponents(ComponentsFilter);
    }

    public void ForceReFilterWeapons()
    {
        FilterWeapons(WeaponFilter);
        FilterAmmo(AmmoFilter);
    }

    private void GetTurretDefs()
    {
        _allTurretDefs = new Dictionary<(string, string), List<TurretDefinition>>();
        foreach (TurretDefinition t in ObjectFactory.GetAllTurretTypes())
        {
            List<TurretDefinition> defsWithKey;
            if(!_allTurretDefs.TryGetValue((t.WeaponType, t.WeaponSize), out defsWithKey))
            {
                defsWithKey = new List<TurretDefinition>();
                _allTurretDefs.Add((t.WeaponType, t.WeaponSize), defsWithKey);
            }
            defsWithKey.Add(t);
        }
    }

    private bool WeaponFilter(ShipDummyInEditor shipDef, string weaponType, string weaponSize)
    {
        for (int i = 0; i < shipDef.Hardpoints.Count; ++i)
        {
            string[] allowedTurrets = shipDef.Hardpoints[i].Item1.AllowedWeaponTypes;
            if (TryMatchTurretDef(allowedTurrets, weaponType, weaponSize) != null)
            {
                return true;
            }
        }
        return false;
    }

    private bool ComponentsFilter(ShipDummyInEditor shipDef, ShipComponentTemplateDefinition comp)
    {
        ObjectFactory.ShipSize shipSz, compMinSz, compMaxSz;
        if (!Enum.TryParse(shipDef.HullDef.ShipSize, out shipSz) ||
            !Enum.TryParse(comp.MinShipSize, out compMinSz) ||
            !Enum.TryParse(comp.MaxShipSize, out compMaxSz))
        {
            return false;
        }

        if (shipSz < compMinSz || shipSz > compMaxSz)
        {
            return false;
        }

        for (int i = 0; i < shipDef.HullDef.ComponentSlots.ForeComponentSlots.Length; ++i)
        {
            if (comp.AllowedSlotTypes.Contains(shipDef.HullDef.ComponentSlots.ForeComponentSlots[i]))
            {
                return true;
            }
        }
        for (int i = 0; i < shipDef.HullDef.ComponentSlots.CenterComponentSlots.Length; ++i)
        {
            if (comp.AllowedSlotTypes.Contains(shipDef.HullDef.ComponentSlots.CenterComponentSlots[i]))
            {
                return true;
            }
        }
        for (int i = 0; i < shipDef.HullDef.ComponentSlots.LeftComponentSlots.Length; ++i)
        {
            if (comp.AllowedSlotTypes.Contains(shipDef.HullDef.ComponentSlots.LeftComponentSlots[i]))
            {
                return true;
            }
        }
        for (int i = 0; i < shipDef.HullDef.ComponentSlots.RightComponentSlots.Length; ++i)
        {
            if (comp.AllowedSlotTypes.Contains(shipDef.HullDef.ComponentSlots.RightComponentSlots[i]))
            {
                return true;
            }
        }
        for (int i = 0; i < shipDef.HullDef.ComponentSlots.AftComponentSlots.Length; ++i)
        {
            if (comp.AllowedSlotTypes.Contains(shipDef.HullDef.ComponentSlots.AftComponentSlots[i]))
            {
                return true;
            }
        }
        return false;
    }

    private bool AmmoFilter(TurretDefinition turretDef, string ammo)
    {
        if (turretDef.BehaviorType == ObjectFactory.WeaponBehaviorType.Gun)
        {
            Warhead w;
            return (ObjectFactory.TryCreateWarhead(turretDef.WeaponType, turretDef.WeaponSize, ammo, out w));
        }
        else if (turretDef.BehaviorType == ObjectFactory.WeaponBehaviorType.Torpedo)
        {
            Warhead w;
            return (ObjectFactory.TryCreateWarhead(ammo, out w));
        }
        else
        {
            return false;
        }
    }

    private TurretDefinition TryMatchTurretDef(string[] hardpointAllowedSlots, string weaponType, string weaponSize)
    {
        return TryMatchTurretDef(hardpointAllowedSlots, weaponType, weaponSize, 0);
    }

    private TurretDefinition TryMatchTurretDef(string[] hardpointAllowedSlots, string weaponType, string weaponSize, int weaponNumAbove)
    {
        TurretDefinition res = null;
        int minWeaponNum = -1;
        List<TurretDefinition> compatibleTurrets;
        if (_allTurretDefs.TryGetValue((weaponType, weaponSize), out compatibleTurrets))
        {
            foreach (TurretDefinition currTurret in compatibleTurrets)
            {
                if (!hardpointAllowedSlots.Contains(string.Format("{0}{1}{2}", currTurret.TurretType, currTurret.WeaponNum, currTurret.WeaponSize)))
                {
                    continue;
                }

                // Try to find the turret with the minimum number of weapons:
                int currWeaponNum;
                if (int.TryParse(currTurret.WeaponNum, out currWeaponNum))
                {
                    if (currWeaponNum <= weaponNumAbove)
                    {
                        continue;
                    }
                    if (minWeaponNum < 0 || currWeaponNum < minWeaponNum)
                    {
                        minWeaponNum = currWeaponNum;
                        res = currTurret;
                    }
                }
                else
                {
                    return currTurret;
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
                    StartDragWeapon(item);
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

    private void StartDragWeapon(ShipEditorDraggable item)
    {
        if (_currShip.HasValue)
        {
            bool drawnPenChard = false;
            for (int i = 0; i < _currShip.Value.Hardpoints.Count; ++i)
            {
                string[] allowedTurrets = _currShip.Value.Hardpoints[i].Item1.AllowedWeaponTypes;
                TurretDefinition turretDef = TryMatchTurretDef(allowedTurrets, item.WeaponKey, item.WeaponSize);

                Material mtl = _currShip.Value.Hardpoints[i].Item3.transform.GetComponent<MeshRenderer>().material;
                if (null == turretDef)
                {
                    mtl.color = SlotIncompatibleColor;
                }
                else if (_currHardpoints[i].Item2 == null)
                {
                    mtl.color = SlotFreeColor;
                }
                else if (_currHardpoints[i].Item2.WeaponType == item.WeaponKey && _currHardpoints[i].Item2.WeaponSize == item.WeaponSize)
                {
                    int prevWeaponNum;
                    if (int.TryParse(_currHardpoints[i].Item2.WeaponNum, out prevWeaponNum) &&
                        TryMatchTurretDef(allowedTurrets, item.WeaponKey, item.WeaponSize, prevWeaponNum) != null)
                    {
                        mtl.color = SlotToAddColor;
                    }
                    else
                    {
                        mtl.color = SlotOccupiedColor;
                    }
                }
                else
                {
                    mtl.color = SlotOccupiedColor;
                }
                _currShip.Value.Hardpoints[i].Item3.enabled = true;

                if (turretDef != null)
                {
                    _currWeapon = turretDef;
                    FilterAmmo(AmmoFilter);
                }
                if (!drawnPenChard && turretDef != null && _currAmmoType != null)
                {
                    drawnPenChard = DrawPenChart(turretDef, _currAmmoType);
                }
            }
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
                            _currShip.Value.Hardpoints[i].Item3.enabled = false;
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

    public void DropItem(ShipEditorDraggable item, ShipEditorDropTarget dropTarget, PointerEventData eventData)
    {
        switch (item.Item)
        {
            case EditorItemType.ShipComponent:
                {
                    PlaceComponent(item, dropTarget);
                }
                break;
            case EditorItemType.Weapon:
                break;
            case EditorItemType.Ammo:
                break;
            case EditorItemType.TurretMod:
                break;
            default:
                break;
        }
    }

    public void ClickItem(ShipEditorDraggable item)
    {
        switch (item.Item)
        {
            case EditorItemType.ShipComponent:
                break;
            case EditorItemType.Weapon:
                {
                    StartDragWeapon(item);
                    Debug.LogFormat("Clicked on weapon {0} {1}", item.WeaponKey, item.WeaponSize);
                }
                break;
            case EditorItemType.Ammo:
                _currAmmoType = item.AmmoTypeKey;
                if (_currWeapon != null)
                {
                    DrawPenChart(_currWeapon, _currAmmoType);
                }
                break;
            case EditorItemType.TurretMod:
                break;
            default:
                break;
        }
    }

    private bool DrawPenChart(TurretDefinition turretDef, string ammoType)
    {
        switch (turretDef.BehaviorType)
        {
            case ObjectFactory.WeaponBehaviorType.Gun:
                SetArmourPenetartionChartGun(turretDef.WeaponType, turretDef.WeaponSize, ammoType);
                return true;
            case ObjectFactory.WeaponBehaviorType.Torpedo:
            case ObjectFactory.WeaponBehaviorType.BomberTorpedo:
                SetArmourPenetartionChartTorpedo(ammoType);
                return true;
            case ObjectFactory.WeaponBehaviorType.Beam:
            case ObjectFactory.WeaponBehaviorType.ContinuousBeam:
            case ObjectFactory.WeaponBehaviorType.Special:
                SetArmourPenetartionChartOther(turretDef.WeaponType, turretDef.WeaponSize);
                return true;
            default:
                return false;
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
                if (_currShip.Value.Hardpoints[j].Item2 == _raycastBuf[i].collider)
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

    private bool SetArmourPenetartionChartGun(string weaponType, string weaponSize, string ammoType)
    {
        Warhead w;
        if (ObjectFactory.TryCreateWarhead(weaponType, weaponSize, ammoType, out w))
        {
            SetArmourPenetartionChartInner(w);
            return true;
        }
        return false;
    }

    private bool SetArmourPenetartionChartTorpedo(string torpedoType)
    {
        Warhead w;
        if (ObjectFactory.TryCreateWarhead(torpedoType, out w))
        {
            SetArmourPenetartionChartInner(w);
            return true;
        }
        return false;
    }

    private bool SetArmourPenetartionChartOther(string weaponType, string weaponSize)
    {
        Warhead w;
        if (ObjectFactory.TryCreateWarhead(weaponType, weaponSize, out w))
        {
            SetArmourPenetartionChartInner(w);
            return true;
        }
        return false;
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

    private void PlaceComponent(ShipEditorDraggable comp, ShipEditorDropTarget target)
    {
        if (comp.Item == EditorItemType.ShipComponent)
        {
            Ship.ShipSection oldSec;
            int oldIdx;
            (oldSec, oldIdx) = FindOldComLocation(comp);

            foreach (KeyValuePair<Ship.ShipSection, ShipEditorDropTarget> shipSecPanel in _shipSectionPanels)
            {
                if (target == shipSecPanel.Value)
                {
                    int placedIdx = PlaceComponentInSection(shipSecPanel.Key, comp.ShipComponentDef);
                    Debug.LogFormat("Dropped {0} into ship {1} section. Placed = {2}", comp.ShipComponentDef.ComponentName, shipSecPanel.Key, placedIdx >= 0);
                    if (placedIdx >= 0)
                    {
                        _currShipCompPlaceholders[shipSecPanel.Key][placedIdx].gameObject.SetActive(false);
                        if (oldIdx < 0)
                        {
                            RectTransform t = Instantiate(PlacedItemPrototype);
                            t.gameObject.AddComponent<StackableUIComponent>();
                            t.SetParent(shipSecPanel.Value.transform);

                            ShipEditorDraggable placedComp = t.gameObject.AddComponent<ShipEditorDraggable>();
                            placedComp.ContainingEditor = this;
                            placedComp.ShipComponentDef = comp.ShipComponentDef;
                            placedComp.CurrentLocation = EditorItemLocation.Ship;

                            Image img = t.Find("Image").GetComponent<Image>();
                            img.sprite = ObjectFactory.GetShipComponentImage(comp.ShipComponentDef.ComponentType);
                        }
                        else
                        {
                            Debug.LogFormat("Removed component {0} from {1}", comp.ShipComponentDef.ComponentName, oldSec);
                            _currShipComps[oldSec][oldIdx] = null;
                            _currShipCompPlaceholders[oldSec][oldIdx].gameObject.SetActive(true);
                            comp.transform.SetParent(shipSecPanel.Value.transform, false);
                        }

                        // Make sure the placeholders are last:
                        for (int i = 0; i < _currShipCompPlaceholders[shipSecPanel.Key].Count; ++i)
                        {
                            _currShipCompPlaceholders[shipSecPanel.Key][i].SetAsLastSibling();
                        }
                    }
                }
            }
        }
    }

    private (Ship.ShipSection, int) FindOldComLocation(ShipEditorDraggable comp)
    {
        if (comp.CurrentLocation == EditorItemLocation.Ship)
        {
            foreach (KeyValuePair<Ship.ShipSection, List<ShipComponentTemplateDefinition>> existingComps in _currShipComps)
            {
                for (int i = 0; i < existingComps.Value.Count; i++)
                {
                    if (existingComps.Value[i] == comp.ShipComponentDef)
                    {
                        return (existingComps.Key, i);
                    }
                }
            }
        }
        return (Ship.ShipSection.Hidden, -1);
    }

    private int PlaceComponentInSection(Ship.ShipSection sec, ShipComponentTemplateDefinition comp)
    {
        string[] slotTypes = _currShipComponentSlots[sec];
        List<ShipComponentTemplateDefinition> occupiedSlots = _currShipComps[sec];
        for (int i = 0; i < slotTypes.Length; ++i)
        {
            if (null == occupiedSlots[i] && comp.AllowedSlotTypes.Contains(slotTypes[i]))
            {
                occupiedSlots[i] = comp;
                return i;
            }
        }
        return -1;
    }

    private void RemoveComponent(ShipEditorDraggable comp)
    {
        if (comp.Item == EditorItemType.ShipComponent)
        {
            Ship.ShipSection oldSec;
            int oldIdx;
            (oldSec, oldIdx) = FindOldComLocation(comp);
            if (oldIdx >= 0)
            {
                _currShipComps[oldSec][oldIdx] = null;
                _currShipCompPlaceholders[oldSec][oldIdx].gameObject.SetActive(true);
                Destroy(comp.gameObject);
            }
        }
    }

    private void InitShipSections()
    {
        _currShipComps.Add(Ship.ShipSection.Center, new List<ShipComponentTemplateDefinition>());
        _currShipComps.Add(Ship.ShipSection.Fore, new List<ShipComponentTemplateDefinition>());
        _currShipComps.Add(Ship.ShipSection.Aft, new List<ShipComponentTemplateDefinition>());
        _currShipComps.Add(Ship.ShipSection.Left, new List<ShipComponentTemplateDefinition>());
        _currShipComps.Add(Ship.ShipSection.Right, new List<ShipComponentTemplateDefinition>());
        _currShipComps.Add(Ship.ShipSection.Hidden, new List<ShipComponentTemplateDefinition>());

        _shipSectionPanels.Add(Ship.ShipSection.Center, ShipSectionsPanel.Center);
        _shipSectionPanels.Add(Ship.ShipSection.Fore, ShipSectionsPanel.Fore);
        _shipSectionPanels.Add(Ship.ShipSection.Aft, ShipSectionsPanel.Aft);
        _shipSectionPanels.Add(Ship.ShipSection.Left, ShipSectionsPanel.Left);
        _shipSectionPanels.Add(Ship.ShipSection.Right, ShipSectionsPanel.Right);

        _currShipCompPlaceholders.Add(Ship.ShipSection.Center, new List<RectTransform>());
        _currShipCompPlaceholders.Add(Ship.ShipSection.Fore, new List<RectTransform>());
        _currShipCompPlaceholders.Add(Ship.ShipSection.Aft, new List<RectTransform>());
        _currShipCompPlaceholders.Add(Ship.ShipSection.Left, new List<RectTransform>());
        _currShipCompPlaceholders.Add(Ship.ShipSection.Right, new List<RectTransform>());
    }

    private void SetShipSections(ShipHullDefinition hullDef)
    {
        foreach (KeyValuePair<Ship.ShipSection, List<ShipComponentTemplateDefinition>> oldCompSlots in _currShipComps)
        {
            ShipEditorDropTarget compsPanel;
            if (_shipSectionPanels.TryGetValue(oldCompSlots.Key, out compsPanel))
            {
                ShipEditorDraggable[] componentItems = compsPanel.GetComponentsInChildren<ShipEditorDraggable>();
                foreach (ShipEditorDraggable comp in componentItems)
                {
                    RemoveComponent(comp);
                }
            }
            List<RectTransform> placeholders;
            if (_currShipCompPlaceholders.TryGetValue(oldCompSlots.Key, out placeholders))
            {
                foreach (RectTransform rt in placeholders)
                {
                    Destroy(rt.gameObject);
                }
                placeholders.Clear();
            }
            oldCompSlots.Value.Clear();
        }
        _currShipComponentSlots = hullDef.ComponentSlots.ToDictionary();
        foreach (KeyValuePair<Ship.ShipSection, string[]> sec in _currShipComponentSlots)
        {
            for (int i = 0; i < sec.Value.Length; ++i)
            {
                _currShipComps[sec.Key].Add(null);
                ShipEditorDropTarget compsPanel;
                if (_shipSectionPanels.TryGetValue(sec.Key, out compsPanel))
                {
                    RectTransform t = Instantiate(PlacedItemPrototype);
                    t.gameObject.AddComponent<StackableUIComponent>();
                    t.SetParent(compsPanel.transform);
                    Image img = t.GetComponent<Image>();
                    if (sec.Value[i] == "ShipSystemCenter")
                    {
                        img.color = new Color(0, 1, 0);
                    }
                    else if (sec.Value[i] == "ShipSystem")
                    {
                        img.color = new Color(0, 0, 1);
                    }
                    else if (sec.Value[i] == "Engine")
                    {
                        img.color = new Color(1, 0, 1);
                    }
                    _currShipCompPlaceholders[sec.Key].Add(t);
                }
            }
        }
        _currShipComps[Ship.ShipSection.Hidden].Add(null);
    }

    public void OnDrop(PointerEventData eventData)
    {
        ShipEditorDraggable droppedItem;
        if (null != eventData.selectedObject && null != (droppedItem = eventData.selectedObject.GetComponent<ShipEditorDraggable>()))
        {
            if (droppedItem.ShipComponentDef != null)
            {
                Debug.LogFormat("Dropped {0} in an empty space", droppedItem.ShipComponentDef.ComponentName);
                RemoveComponent(droppedItem);
            }
        }
    }

    public RectTransform ShipClassesScrollViewContent;
    public RectTransform ShipHullsScrollViewContent;
    public RectTransform WeaponsScrollViewContent;
    public RectTransform AmmoScrollViewContent;
    public RectTransform ShipCompsScrollViewContent;

    public AreaGraphRenderer PenetrationGraph;
    public RectTransform ButtonPrototype;
    public RectTransform PlacedItemPrototype;
    public Transform HardpointMarkerPrototype;
    public Camera ImageCam;
    public Camera ShipViewCam;
    public RawImage ShipViewBox;

    public ShipEditorShipSectionsPanel ShipSectionsPanel;

    public Toggle[] ComponentDisplayToggleGroup;
    public Toggle[] WeaponsDisplayToggleGroup;

    public Color SlotFreeColor;
    public Color SlotIncompatibleColor;
    public Color SlotOccupiedColor;
    public Color SlotToAddColor;

    [NonSerialized]
    private ShipDummyInEditor? _currShip = null;
    [NonSerialized]
    private List<(TurretHardpoint, TurretDefinition, Transform)> _currHardpoints = new List<(TurretHardpoint, TurretDefinition, Transform)>();
    [NonSerialized]
    private Dictionary<Ship.ShipSection, List<ShipComponentTemplateDefinition>> _currShipComps = new Dictionary<Ship.ShipSection, List<ShipComponentTemplateDefinition>>();
    [NonSerialized]
    private Dictionary<Ship.ShipSection, string[]> _currShipComponentSlots = new Dictionary<Ship.ShipSection, string[]>();
    [NonSerialized]
    private Dictionary<Ship.ShipSection, List<RectTransform>> _currShipCompPlaceholders = new Dictionary<Ship.ShipSection, List<RectTransform>>();
    [NonSerialized]
    private Dictionary<Ship.ShipSection, ShipEditorDropTarget> _shipSectionPanels = new Dictionary<Ship.ShipSection, ShipEditorDropTarget>();
    [NonSerialized]
    private Dictionary<string, ShipDummyInEditor> _shipsCache = new Dictionary<string, ShipDummyInEditor>();
    [NonSerialized]
    private Dictionary<(string, string), List<TurretDefinition>> _allTurretDefs;
    [NonSerialized]
    private RaycastHit[] _raycastBuf = new RaycastHit[100];
    [NonSerialized]
    private bool _doDrawDebugRaycast = false;
    [NonSerialized]
    private (Vector3, Vector3) _debugRaycast;
    [NonSerialized]
    private List<ShipEditorDraggable> _allShipComponents;
    [NonSerialized]
    private bool[] _allShipComponentsComatibleFlags;
    [NonSerialized]
    private List<ShipEditorDraggable> _allWeapons;
    [NonSerialized]
    private bool[] _allWeaponsComatibleFlags;
    [NonSerialized]
    private List<ShipEditorDraggable> _allAmmoTypes;
    [NonSerialized]
    private bool[] _allAmmoTypesComatibleFlags;

    [NonSerialized]
    private string _currAmmoType = null;
    [NonSerialized]
    private TurretDefinition _currWeapon = null;

    private struct ShipDummyInEditor
    {
        public string Key;
        public Transform ShipModel;
        public ShipHullDefinition HullDef;
        public List<(TurretHardpoint, Collider, MeshRenderer)> Hardpoints;
    }

    public enum EditorItemType { ShipComponent, Weapon, Ammo, TurretMod }

    public enum EditorItemLocation { Production, Inventory, Ship }

    [Serializable]
    public struct ShipEditorShipSectionsPanel
    {
        public ShipEditorDropTarget Center;
        public ShipEditorDropTarget Fore;
        public ShipEditorDropTarget Aft;
        public ShipEditorDropTarget Left;
        public ShipEditorDropTarget Right;
    }
}