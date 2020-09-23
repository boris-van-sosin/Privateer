using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class ShipEditor : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    // Start is called before the first frame update
    void Start()
    {
        PopulateHulls();
        PopulateWeapons();
        PopulateShipComps();
        PopulateAmmo();
        PopulateTurretMods();
        GetTurretDefs();
        FilterWeapons(null);
        FilterComponents(null);
        FilterTurretMods(null);
        FilterAmmo(null);
        InitShipSections();
        PenetrationGraphs = PenetrationGraphBoxes.Select(a => (a.GetComponentInChildren<AreaGraphRenderer>(), a.Find("Image").GetComponent<Image>())).ToArray();
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
                    if (null != _currHardpoints[i].TurretModel)
                    {
                        Destroy(_currHardpoints[i].TurretModel.gameObject);
                    }
                }
                _currHardpoints.Clear();
                needsReFilter = true;
                WeaponCfgPanel.gameObject.SetActive(false);
                _weaponCfgPanelDirty = true;
            }
        }
        else if (null == _currShip)
        {
            needsReFilter = true;
            _weaponCfgPanelDirty = true;
        }

        ShipDummyInEditor s;
        if (_shipsCache.TryGetValue(key, out s))
        {
            s.ShipModel.gameObject.SetActive(true);
            for (int i = 0; i < s.Hardpoints.Count; ++i)
            {
                s.Hardpoints[i].Item3.gameObject.SetActive(false);
            }
            ShipPhotoUtil.PositionCameraToObject(ShipViewCam, s.ShipModel, 1.2f);
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
            _currHardpoints.Add(TurretInEditor.FromEmptyHardpoint(s.Hardpoints[i].Item1));
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
        MeshRenderer[] renderers = s.GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < renderers.Length; ++i)
        {
            //renderers[i].sharedMaterial = ShipMtlInDisplay;
        }
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
        if (ShipCompsScrollViewContent.TryGetComponent(out stacking2D) && stacking2D.MaxFirstDirection > 0)
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

            GroupedOnOffButton groupedBtn = t.gameObject.AddComponent<GroupedOnOffButton>();
            Button innerBtn = t.GetComponent<Button>();
            groupedBtn.TargetGraphic = t.GetComponent<Graphic>();
            groupedBtn.OnColor = AmmoSelectedColor;
            groupedBtn.OffColor = innerBtn.colors.normalColor;
            groupedBtn.Group = AmmoToggleGroup;

            ShipEditorDraggable draggable = t.gameObject.AddComponent<ShipEditorDraggable>();
            draggable.ContainingEditor = this;
            draggable.Item = EditorItemType.Ammo;
            draggable.AmmoTypeKey = ammoKey;
            _allAmmoTypes.Add(draggable);
        }

        IReadOnlyList<string> torpedoAmmo = ObjectFactory.GetAllAmmoTypes(false, true);
        for (int i = 0; i < torpedoAmmo.Count; ++i)
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

            GroupedOnOffButton groupedBtn = t.gameObject.AddComponent<GroupedOnOffButton>();
            Button innerBtn = t.GetComponent<Button>();
            groupedBtn.TargetGraphic = t.GetComponent<Graphic>();
            groupedBtn.OnColor = AmmoSelectedColor;
            groupedBtn.OffColor = innerBtn.colors.normalColor;
            groupedBtn.Group = AmmoToggleGroup;

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

        if (_currWeapon == null)
        {
            AmmoToggleGroup.MinOn = AmmoToggleGroup.MaxOn = 0;
            for (int i = 0; i < _allAmmoTypes.Count; ++i)
            {
                if (_allAmmoTypes != null)
                {
                    GroupedOnOffButton btn = _allAmmoTypes[i].GetComponent<GroupedOnOffButton>();
                    btn.OffColor = Color.white;
                    btn.Value = false;
                }
            }
        }
        else
        {
            AmmoToggleGroup.MinOn = 0;
            if (_lastClickedAmmoType != null && AmmoFilter(_currWeapon, _lastClickedAmmoType))
            {
                AmmoToggleGroup.MaxOn = 2;
                SetNumAllowedAmmo();
                int numOn = 0;
                int currIdx = -1;
                for (int j = 0; j < _allAmmoTypes.Count; ++j)
                {
                    GroupedOnOffButton btn2 = _allAmmoTypes[j].GetComponent<GroupedOnOffButton>();
                    if (numOn < AmmoToggleGroup.MaxOn && _allAmmoTypes[j].AmmoTypeKey == _lastClickedAmmoType)
                    {
                        btn2.Value = true;
                        currIdx = j;
                        ++numOn;
                    }
                    Button baseBtn = _allAmmoTypes[j].GetComponent<Button>();
                    btn2.OnColor = AmmoSelectedColor;
                    btn2.OffColor = compatible[j] ? baseBtn.colors.normalColor : Color.white;
                }
                for (int k = 0; k < _allAmmoTypes.Count; ++k)
                {
                    GroupedOnOffButton btn2 = _allAmmoTypes[k].GetComponent<GroupedOnOffButton>();
                    if (numOn < AmmoToggleGroup.MaxOn && k != currIdx && btn2.Value)
                    {
                        ++numOn;
                    }
                    else if (numOn >= AmmoToggleGroup.MaxOn && k != currIdx)
                    {
                        btn2.Value = false;
                    }
                }
                AmmoToggleGroup.MinOn = 1;
            }
            else
            {
                for (int i = 0; i < _allAmmoTypes.Count; ++i)
                {
                    if (compatible[i])
                    {
                        AmmoToggleGroup.MaxOn = 2;
                        SetNumAllowedAmmo();
                        GroupedOnOffButton btn = _allAmmoTypes[i].GetComponent<GroupedOnOffButton>();
                        btn.Value = true;
                        for (int j = 0; j < _allAmmoTypes.Count; ++j)
                        {
                            GroupedOnOffButton btn2 = _allAmmoTypes[j].GetComponent<GroupedOnOffButton>();
                            if (j != i)
                            {
                                btn2.Value = false;
                            }
                            Button baseBtn = _allAmmoTypes[j].GetComponent<Button>();
                            btn2.OnColor = AmmoSelectedColor;
                            btn2.OffColor = compatible[j] ? baseBtn.colors.normalColor : Color.white;
                        }
                        AmmoToggleGroup.MinOn = 1;
                        break;
                    }
                }
            }
        }

        FitScrollContent(AmmoScrollViewContent.GetComponent<RectTransform>(), offset);
        AmmoScrollViewContent.GetComponent<StackingLayout>().ForceRefresh();
    }

    private void PopulateTurretMods()
    {
        StackingLayout stackingBehavior = TurretModsScrollViewContent.GetComponent<StackingLayout>();
        stackingBehavior.AutoRefresh = false;

        float offset = 0.0f;

        TurretMod[] turretMods = (TurretMod[]) Enum.GetValues(typeof(TurretMod));
        _allTurretMods = new List<ShipEditorDraggable>(turretMods.Length);
        for (int i = 0; i < turretMods.Length; ++i)
        {
            string turretModKey = turretMods[i].ToString();
            Sprite turretModSprite = ObjectFactory.GetSTurretModImage(turretModKey);
            if (turretModSprite == null)
            {
                continue;
            }

            RectTransform t = Instantiate(ButtonPrototype);
            t.gameObject.AddComponent<StackableUIComponent>();

            t.SetParent(TurretModsScrollViewContent, false);
            float height = t.rect.height;
            float pivotOffset = t.pivot.x * t.rect.width;
            t.anchoredPosition = new Vector2(pivotOffset, 0);
            offset -= height;

            TextMeshProUGUI textElem = t.GetComponentInChildren<TextMeshProUGUI>();
            textElem.text = turretMods[i].ToString();

            Image img = t.Find("Image").GetComponent<Image>();
            img.sprite = turretModSprite;

            GroupedOnOffButton groupedBtn = t.gameObject.AddComponent<GroupedOnOffButton>();
            Button innerBtn = t.GetComponent<Button>();
            groupedBtn.TargetGraphic = t.GetComponent<Graphic>();
            groupedBtn.OnColor = AmmoSelectedColor;
            groupedBtn.OffColor = innerBtn.colors.normalColor;
            groupedBtn.Group = TurretModToggleGroup;

            ShipEditorDraggable draggable = t.gameObject.AddComponent<ShipEditorDraggable>();
            draggable.ContainingEditor = this;
            draggable.Item = EditorItemType.TurretMod;
            draggable.TurretModKey = turretModKey;
            _allTurretMods.Add(draggable);
        }

        _allTurretModsComatibleFlags = new bool[_allTurretMods.Count];
    }

    private void FilterTurretMods(Func<TurretDefinition, string, bool> filter)
    {
        float offset;
        List<ShipEditorDraggable> compItems = _allTurretMods;
        bool[] compatible = _allTurretModsComatibleFlags;

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

        FilterAndSortInner(TurretModsScrollViewContent, x => (_currWeapon != null && filter(_currWeapon, x.TurretModKey)), compItems, compatible, ft, out offset);

        if (_currWeapon == null)
        {
            TurretModToggleGroup.MinOn = TurretModToggleGroup.MaxOn = 0;
            for (int i = 0; i < _allTurretMods.Count; ++i)
            {
                if (_allTurretMods != null)
                {
                    GroupedOnOffButton btn = _allTurretMods[i].GetComponent<GroupedOnOffButton>();
                    btn.OffColor = Color.white;
                    btn.Value = false;
                }
            }
        }
        else
        {
            if (_lastClickedTurretMod != null && TurretModFilter(_currWeapon, _lastClickedTurretMod))
            {
                TurretModToggleGroup.MaxOn = 1;
                for (int j = 0; j < _allTurretMods.Count; ++j)
                {
                    GroupedOnOffButton btn2 = _allTurretMods[j].GetComponent<GroupedOnOffButton>();
                    btn2.Value = _allTurretMods[j].TurretModKey == _lastClickedTurretMod;
                    Button baseBtn = _allTurretMods[j].GetComponent<Button>();
                    btn2.OnColor = AmmoSelectedColor;
                    btn2.OffColor = compatible[j] ? baseBtn.colors.normalColor : Color.white;
                }
            }
            else
            {
                bool anyCompatible = false;
                for (int i = 0; i < _allTurretMods.Count; ++i)
                {
                    if (compatible[i])
                    {
                        TurretModToggleGroup.MaxOn = 1;
                        GroupedOnOffButton btn = _allTurretMods[i].GetComponent<GroupedOnOffButton>();
                        btn.Value = true;
                        for (int j = 0; j < _allTurretMods.Count; ++j)
                        {
                            GroupedOnOffButton btn2 = _allTurretMods[j].GetComponent<GroupedOnOffButton>();
                            if (j != i)
                            {
                                btn2.Value = false;
                            }
                            Button baseBtn = _allTurretMods[j].GetComponent<Button>();
                            btn2.OnColor = AmmoSelectedColor;
                            btn2.OffColor = compatible[j] ? baseBtn.colors.normalColor : Color.white;
                        }
                        _lastClickedTurretMod = _allTurretMods[i].TurretModKey;
                        anyCompatible = true;
                        break;
                    }
                }

                if (!anyCompatible)
                {
                    for (int j = 0; j < _allTurretMods.Count; ++j)
                    {
                        GroupedOnOffButton btn2 = _allTurretMods[j].GetComponent<GroupedOnOffButton>();
                        btn2.Value = false;
                        Button baseBtn = _allTurretMods[j].GetComponent<Button>();
                        btn2.OnColor = AmmoSelectedColor;
                        btn2.OffColor = compatible[j] ? baseBtn.colors.normalColor : Color.white;
                    }
                    TurretModToggleGroup.MinOn = TurretModToggleGroup.MaxOn = 0;
                    _lastClickedTurretMod = null;
                }
            }
        }

        FitScrollContent(TurretModsScrollViewContent.GetComponent<RectTransform>(), offset);
        TurretModsScrollViewContent.GetComponent<StackingLayout>().ForceRefresh();
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
        FilterTurretMods(TurretModFilter);
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

    private bool TurretModFilter(TurretDefinition turretDef, string turretMod)
    {
        if (turretMod == null || turretMod == TurretModEmptyString)
        {
            return true;
        }
        else
        {
            TurretMod mod;
            if (Enum.TryParse<TurretMod>(turretMod, out mod))
            {
                return TurretDefinition.IsTurretModCompatible(turretDef, mod);
            }
            else
            {
                return false;
            }
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
                    if (_currAmmoTypes == null && _currTurretMods == null)
                    {
                        (_currTurretMods, _currAmmoTypes) = SelectedTurretModsAndAmmoTypes();
                    }
                    DrawPenCharts();
                }
                break;
            case EditorItemType.Ammo:
                _lastClickedAmmoType = item.AmmoTypeKey;
                if (_currWeapon != null)
                {
                    GroupedOnOffButton btn;
                    if (item.TryGetComponent(out btn))
                    {
                        btn.Value = true;
                    }
                    (_currTurretMods, _currAmmoTypes) = SelectedTurretModsAndAmmoTypes();
                    DrawPenCharts();
                }
                break;
            case EditorItemType.TurretMod:
                _lastClickedTurretMod = item.TurretModKey;
                StartDragTurretMod(item);
                (_currTurretMods, _currAmmoTypes) = SelectedTurretModsAndAmmoTypes();
                DrawPenCharts();
                break;
            default:
                break;
        }
        _clickedHardpoint = -1;
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
                else if (_currHardpoints[i].IsEmpty)
                {
                    mtl.color = SlotFreeColor;
                }
                else if (_currHardpoints[i].TurretDef.WeaponType == item.WeaponKey && _currHardpoints[i].TurretDef.WeaponSize == item.WeaponSize)
                {
                    int prevWeaponNum;
                    if (int.TryParse(_currHardpoints[i].TurretDef.WeaponNum, out prevWeaponNum) &&
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
                    FilterTurretMods(TurretModFilter);
                    FilterAmmo(AmmoFilter);
                }
                if (!drawnPenChard && turretDef != null && _lastClickedAmmoType != null)
                {
                    drawnPenChard = DrawPenCharts();
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
                PlaceAmmoType(item, eventData);
                break;
            case EditorItemType.TurretMod:
                PlaceTurretMod(item, eventData);
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
                    if (_currAmmoTypes == null && _currTurretMods == null)
                    {
                        (_currTurretMods, _currAmmoTypes) = SelectedTurretModsAndAmmoTypes();
                    }
                    DrawPenCharts();
                }
                break;
            case EditorItemType.Ammo:
                _lastClickedAmmoType = item.AmmoTypeKey;
                if (_currWeapon != null)
                {
                    (_currTurretMods, _currAmmoTypes) = SelectedTurretModsAndAmmoTypes();
                    DrawPenCharts();
                }
                break;
            case EditorItemType.TurretMod:
                _lastClickedTurretMod = item.TurretModKey;
                StartDragTurretMod(item);
                (_currTurretMods, _currAmmoTypes) = SelectedTurretModsAndAmmoTypes();
                DrawPenCharts();
                break;
            default:
                break;
        }
        _clickedHardpoint = -1;
    }

    private int SetPenCharts(TurretDefinition turretDef, string[] selectedAmmoTypes)
    {
        if (turretDef == null)
        {
            PenetrationGraphBoxes[0].gameObject.SetActive(false);
            PenetrationGraphBoxes[1].gameObject.SetActive(false);
            SwapAmmoButton.gameObject.SetActive(false);
            return 0;
        }
        else
        {
            int maxPenCharts;
            bool turretNeedsAmmoType = _allAmmoTypes.Any(a => AmmoFilter(turretDef, a.AmmoTypeKey));

            if (turretNeedsAmmoType && (selectedAmmoTypes == null || selectedAmmoTypes.Length == 0))
            {
                maxPenCharts = 0;
            }
            else if (!turretNeedsAmmoType)
            {
                maxPenCharts = 1;
            }
            else
            {
                maxPenCharts = selectedAmmoTypes.Count(a => a != null);
            }

            for (int i = 0; i < PenetrationGraphBoxes.Length; ++i)
            {
                PenetrationGraphBoxes[i].gameObject.SetActive(i < maxPenCharts);
                if (i < maxPenCharts && turretNeedsAmmoType)
                {
                    PenetrationGraphs[i].Item2.gameObject.SetActive(true);
                    PenetrationGraphs[i].Item2.sprite = ObjectFactory.GetAmmonImage(selectedAmmoTypes[i]);
                }
                else if (!turretNeedsAmmoType)
                {
                    PenetrationGraphs[i].Item2.gameObject.SetActive(false);
                }
            }
            SwapAmmoButton.gameObject.SetActive(maxPenCharts > 1);

            return maxPenCharts;
        }
    }

    private bool DrawPenCharts()
    {
        return DrawPenCharts(_currWeapon, _currAmmoTypes);
    }

    private bool DrawPenCharts(TurretDefinition turretDef, string[] ammoTypes)
    {
        int numPenChartsToDraw = SetPenCharts(turretDef, ammoTypes);
        bool drawnAnyPenChart = false;
        for (int i = 0; i < numPenChartsToDraw; ++i)
        {
            string currAmmoInArr = null;
            if (ammoTypes != null && ammoTypes.Length > i)
            {
                currAmmoInArr = ammoTypes[i];
            }
            if (DrawPenChart(turretDef, currAmmoInArr, i))
            {
                drawnAnyPenChart = true;
            }
        }
        return drawnAnyPenChart;
    }

    private bool DrawPenChart(TurretDefinition turretDef, string ammoType, int graphIdx)
    {
        switch (turretDef.BehaviorType)
        {
            case ObjectFactory.WeaponBehaviorType.Gun:
                SetArmourPenetartionChartGun(turretDef.WeaponType, turretDef.WeaponSize, ammoType, graphIdx);
                return true;
            case ObjectFactory.WeaponBehaviorType.Torpedo:
            case ObjectFactory.WeaponBehaviorType.BomberTorpedo:
                SetArmourPenetartionChartTorpedo(ammoType, graphIdx);
                return true;
            case ObjectFactory.WeaponBehaviorType.Beam:
            case ObjectFactory.WeaponBehaviorType.ContinuousBeam:
            case ObjectFactory.WeaponBehaviorType.Special:
                SetArmourPenetartionChartOther(turretDef.WeaponType, turretDef.WeaponSize, graphIdx);
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
            if (null != _currHardpoints[hardpointIdx].TurretModel &&
                _currHardpoints[hardpointIdx].TurretDef.WeaponType == item.WeaponKey && _currHardpoints[hardpointIdx].TurretDef.WeaponSize == item.WeaponSize &&
                int.TryParse(_currHardpoints[hardpointIdx].TurretDef.WeaponNum, out prevWeaponNum))
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
                if (null != _currHardpoints[hardpointIdx].TurretModel)
                {
                    Destroy(_currHardpoints[hardpointIdx].TurretModel.gameObject);
                }

                // Place the new weapon
                Transform dummyTurret = ObjectFactory.CreateTurretDummy(turretDef.TurretType, turretDef.WeaponNum, item.WeaponSize, item.WeaponKey);
                dummyTurret.parent = hardpoint.transform;
                dummyTurret.localScale = Vector3.one;
                dummyTurret.localPosition = Vector3.zero;
                Quaternion q = Quaternion.LookRotation(-hardpoint.transform.up, hardpoint.transform.forward);
                dummyTurret.transform.rotation = q;

                _currHardpoints[hardpointIdx] = new TurretInEditor()
                { Hardpoint = hardpoint, TurretDef = turretDef, TurretModel = dummyTurret, AmmoTypes = _currAmmoTypes, TurretMods = _currTurretMods };

                MeshRenderer[] renderers = dummyTurret.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < renderers.Length; ++i)
                {
                    //renderers[i].sharedMaterial = ShipMtlInDisplay;
                }

                GetShipQualityStats();
            }
        }
    }

    private (string[], string[]) SelectedTurretModsAndAmmoTypes()
    {
        string[] selectedTurretMods;
        if (TurretModToggleGroup.NumButtonsOn == 0)
        {
            selectedTurretMods = null;
        }
        else
        {
            selectedTurretMods = new string[] { _lastClickedTurretMod };
        }

        string[] selectedAmmo;
        if (AmmoToggleGroup.NumButtonsOn == 0)
        {
            selectedAmmo = null;
        }
        else
        {
            if (selectedTurretMods != null && selectedTurretMods.Contains(TurretModDuamAmmoString))
            {
                List<string> selectedAmmoList = AmmoToggleGroup.ButtonsOn.Select(a => a.GetComponent<ShipEditorDraggable>().AmmoTypeKey).ToList();
                while (selectedAmmoList.Count < 2)
                {
                    selectedAmmoList.Add(null);
                }
                while (selectedAmmoList.Count > 2)
                {
                    selectedAmmoList.RemoveAt(selectedAmmoList.Count - 1);
                }
                selectedAmmo = selectedAmmoList.ToArray();
            }
            else
            {
                selectedAmmo = AmmoToggleGroup.ButtonsOn.Select(a => a.GetComponent<ShipEditorDraggable>().AmmoTypeKey).Take(1).ToArray();
            }
        }
        return (selectedTurretMods, selectedAmmo);
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

    private bool SetArmourPenetartionChartGun(string weaponType, string weaponSize, string ammoType, int graphIdx)
    {
        Warhead w;
        if (ObjectFactory.TryCreateWarhead(weaponType, weaponSize, ammoType, out w))
        {
            SetArmourPenetartionChartInner(w, graphIdx);
            return true;
        }
        return false;
    }

    private bool SetArmourPenetartionChartTorpedo(string torpedoType, int graphIdx)
    {
        Warhead w;
        if (ObjectFactory.TryCreateWarhead(torpedoType, out w))
        {
            SetArmourPenetartionChartInner(w, graphIdx);
            return true;
        }
        return false;
    }

    private bool SetArmourPenetartionChartOther(string weaponType, string weaponSize, int graphIdx)
    {
        Warhead w;
        if (ObjectFactory.TryCreateWarhead(weaponType, weaponSize, out w))
        {
            SetArmourPenetartionChartInner(w, graphIdx);
            return true;
        }
        return false;
    }

    private void SetArmourPenetartionChartInner(Warhead w, int graphIdx)
    {
        ArmourPenetrationTable penetrationTable = ObjectFactory.GetArmourPenetrationTable();
        int minArmor = 0, maxArmor = 1000, samplePoints = 50;
        PenetrationGraphs[graphIdx].Item1.DataPoints = new Vector2[samplePoints + 1];

        float fSamplePoints = samplePoints;
        for (int i = 0; i <= samplePoints; ++i)
        {
            float currStep = i / fSamplePoints;
            float penetrationProb = penetrationTable.PenetrationProbability(Mathf.RoundToInt(Mathf.Lerp(minArmor, maxArmor, currStep)), w.ArmourPenetration);
            PenetrationGraphs[graphIdx].Item1.DataPoints[i] = new Vector2(currStep, penetrationProb);
        }
        PenetrationGraphs[graphIdx].Item1.RequireUpdate();
    }

    private void StartDragTurretMod(ShipEditorDraggable item)
    {
        if (_currWeapon != null)
        {
            GroupedOnOffButton btn;
            if (item.TryGetComponent(out btn))
            {
                btn.Value = true;
            }
        }
        SetNumAllowedAmmo();
        FilterAmmo(AmmoFilter);
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
                            placedComp.CurrentShipSection = shipSecPanel.Key;

                            Image img = t.Find("Image").GetComponent<Image>();
                            img.sprite = ObjectFactory.GetShipComponentImage(comp.ShipComponentDef.ComponentType);
                        }
                        else
                        {
                            Debug.LogFormat("Removed component {0} from {1}", comp.ShipComponentDef.ComponentName, oldSec);
                            _currShipComps[oldSec][oldIdx] = null;
                            _currShipCompPlaceholders[oldSec][oldIdx].gameObject.SetActive(true);
                            comp.transform.SetParent(shipSecPanel.Value.transform, false);
                            comp.CurrentShipSection = shipSecPanel.Key;
                        }

                        // Make sure the placeholders are last:
                        for (int i = 0; i < _currShipCompPlaceholders[shipSecPanel.Key].Count; ++i)
                        {
                            _currShipCompPlaceholders[shipSecPanel.Key][i].SetAsLastSibling();
                        }

                        GetShipQualityStats();

                        break;
                    }
                }
            }
        }
    }

    private void PlaceAmmoType(ShipEditorDraggable item, PointerEventData eventData)
    {
        if (item.AmmoTypeKey == null)
        {
            return;
        }

        int hardpointIdx = RaycastDropWeapon(eventData.position);
        if (hardpointIdx >= 0)
        {
            if (!_currHardpoints[hardpointIdx].IsEmpty && AmmoFilter(_currHardpoints[hardpointIdx].TurretDef, item.AmmoTypeKey))
            {
                _currHardpoints[hardpointIdx] = _currHardpoints[hardpointIdx].ReplaceAmmo(new string[] { item.AmmoTypeKey });
            }
        }
    }

    private void PlaceTurretMod(ShipEditorDraggable item, PointerEventData eventData)
    {
        int hardpointIdx = RaycastDropWeapon(eventData.position);
        if (hardpointIdx >= 0)
        {
            if (!_currHardpoints[hardpointIdx].IsEmpty && TurretModFilter(_currHardpoints[hardpointIdx].TurretDef, item.TurretModKey))
            {
                string turretModToAdd = item.TurretModKey != null ? item.TurretModKey : TurretModEmptyString;
                string[] prevTurretMods = _currHardpoints[hardpointIdx].TurretMods;
                string[] newTurretMods = CycleStringArr(prevTurretMods, turretModToAdd);
                bool prevDual = prevTurretMods.Contains(TurretModDuamAmmoString), newDual = newTurretMods.Contains(TurretModDuamAmmoString),
                    incAmmoNum = !prevDual && newDual, decAmmoNum = prevDual && !newDual;

                _currHardpoints[hardpointIdx] = _currHardpoints[hardpointIdx].ReplaceTurretMods(newTurretMods);

                // Set the ammo array to the correct length:
                if (incAmmoNum)
                {
                    _currHardpoints[hardpointIdx] = _currHardpoints[hardpointIdx].ReplaceAmmo(_currHardpoints[hardpointIdx].AmmoTypes.Concat(new string[] { null }).ToArray());
                }
                else if (decAmmoNum)
                {
                    _currHardpoints[hardpointIdx] = _currHardpoints[hardpointIdx].ReplaceAmmo(_currHardpoints[hardpointIdx].AmmoTypes.Take(1).ToArray());
                }
            }
        }
    }

    private void SetNumAllowedAmmo()
    {
        if (AmmoToggleGroup.MaxOn != 0)
        {
            AmmoToggleGroup.MaxOn = _lastClickedTurretMod == TurretModDuamAmmoString ? 2 : 1;
        }
    }

    private void GetShipQualityStats()
    {
        int powerCapacity = 0, heatCapacity = 0, powerGeneration = 0, cooling = 0, powerConsumption = 0, heatGeneration = 0, powerHigh = 0, heatHigh = 0, powerPerSalvoMain = 0, powerPerSalvoAll = 0, heatPerSalvoMain = 0, heatPerSalvoAll = 0;
        float powerForSustainedFireMain = 0, powerForSustainedFireAll = 0, heatForSustainedFireMain = 0, heatForSustainedFireAll = 0;
        foreach (KeyValuePair<Ship.ShipSection, List<ShipComponentTemplateDefinition>> existingComps in _currShipComps)
        {
            foreach (ShipComponentTemplateDefinition currComp in existingComps.Value)
            {
                if (currComp == null)
                {
                    continue;
                }

                if (currComp.CapacitorBankDefinition != null)
                {
                    powerCapacity += currComp.CapacitorBankDefinition.PowerCapacity;
                }
                else if (currComp.DamageControlDefinition != null)
                {
                    powerHigh += currComp.DamageControlDefinition.PowerUsage;
                    powerHigh += currComp.DamageControlDefinition.HeatGeneration;
                }
                else if (currComp.ElectromagneticClampsDefinition != null)
                {
                    powerHigh += currComp.ElectromagneticClampsDefinition.PowerUsage;
                    heatHigh += currComp.ElectromagneticClampsDefinition.HeatGeneration;
                }
                else if (currComp.FireControlGeneralDefinition != null)
                {
                    powerConsumption += currComp.FireControlGeneralDefinition.PowerUsage;
                    heatGeneration += currComp.FireControlGeneralDefinition.HeatGeneration;
                }
                else if (currComp.HeatExchangeDefinition != null)
                {
                    cooling += currComp.HeatExchangeDefinition.CoolignRate;
                }
                else if (currComp.HeatSinkDefinition != null)
                {
                    heatCapacity += currComp.HeatSinkDefinition.HeatCapacity;
                }
                else if (currComp.PowerPlantDefinition != null)
                {
                    powerGeneration += currComp.PowerPlantDefinition.PowerOutput;
                    heatGeneration += currComp.PowerPlantDefinition.HeatOutput;
                }
                else if (currComp.ShieldGeneratorDefinition != null)
                {
                    powerConsumption += currComp.ShieldGeneratorDefinition.PowerUsage + currComp.ShieldGeneratorDefinition.PowerPerShieldRegeneration;
                    heatGeneration += currComp.ShieldGeneratorDefinition.HeatGeneration = currComp.ShieldGeneratorDefinition.HeatPerShieldRegeneration;
                }
                else if (currComp.ShipEngineDefinition != null)
                {
                    powerConsumption += currComp.ShipEngineDefinition.PowerUsage;
                    heatGeneration += currComp.ShipEngineDefinition.HeatGeneration;
                }
            }
        }
        for (int i = 0; i < _currHardpoints.Count; ++i)
        {
            TurretInEditor hardpoint = _currHardpoints[i];
            if (hardpoint.IsEmpty)
            {
                continue;
            }

            int powerToFire, heatToFire;
            float firingInterval;
            (powerToFire, heatToFire, firingInterval) = ObjectFactory.GetWeaponPowerConsumption(hardpoint.TurretDef.WeaponType, hardpoint.TurretDef.WeaponSize);
            if (powerToFire >= 0)
            {
                int numBarrels;
                if (!int.TryParse(hardpoint.TurretDef.WeaponNum, out numBarrels))
                {
                    numBarrels = 1;
                }
                powerToFire *= numBarrels;
                heatToFire *= numBarrels;
                float powerSustained = powerToFire / firingInterval, heatSustained = heatToFire / firingInterval;

                powerPerSalvoAll += powerToFire;
                heatPerSalvoAll += heatToFire;
                powerForSustainedFireAll += powerSustained;
                heatForSustainedFireAll += heatSustained;

                if (hardpoint.Hardpoint.WeaponAIHint == TurretAIHint.Main)
                {
                    int oppHardpoint = FindPairedTurret(_currHardpoints, i);
                    if (oppHardpoint >= 0)
                    {
                        // Paired turret:
                        int oppPowerToFire, oppHeatToFire;
                        float oppFiringInterval;
                        (oppPowerToFire, oppHeatToFire, oppFiringInterval) =
                            ObjectFactory.GetWeaponPowerConsumption(_currHardpoints[oppHardpoint].TurretDef.WeaponType, _currHardpoints[oppHardpoint].TurretDef.WeaponSize);
                        if (oppPowerToFire >= 0)
                        {
                            int oppNumBarrels;
                            if (!int.TryParse(_currHardpoints[oppHardpoint].TurretDef.WeaponNum, out oppNumBarrels))
                            {
                                oppNumBarrels = 1;
                            }
                            oppPowerToFire *= oppNumBarrels;
                            oppHeatToFire *= oppNumBarrels;
                            if (i < oppHardpoint)
                            {
                                powerPerSalvoMain += Mathf.Max(powerToFire, oppPowerToFire);
                                heatPerSalvoMain += Mathf.Max(heatToFire, oppHeatToFire);
                            }
                        }
                        else
                        {
                            // There is something invalid in the opposite turret. Add the regular power & heat.
                            powerPerSalvoMain += powerToFire;
                            heatPerSalvoMain += heatToFire;
                        }
                    }
                    else
                    {
                        // Unpaired turret:
                        powerPerSalvoMain += powerToFire;
                        heatPerSalvoMain += heatToFire;
                    }
                    powerForSustainedFireMain += powerSustained;
                    heatForSustainedFireMain += heatSustained;
                }
            }
        }
        Debug.LogFormat("Ship stats: Power capacity: {0} Heat capacity: {1} Power generation: {2}/sec, Power consumption (normal): {3}/sec Power consumption (high): {4}/sec Cooling: {5}/sec Heat generation(normal): {6}/sec Heat generation(high): {7}/sec Power per salvo: main: {8} all: {9} Heat per salvo: main: {10} all: {11} Power for sustained fire: main: {12} all: {13} heat for sustained fire: main: {14} all: {15}",
                        powerCapacity, heatCapacity, powerGeneration * 4, (powerConsumption) * 4, (powerConsumption + powerHigh) * 4, cooling * 4, (heatGeneration) * 4, (heatGeneration + heatHigh) * 4,
                        powerPerSalvoMain, powerPerSalvoAll, heatPerSalvoMain, heatPerSalvoAll, powerForSustainedFireMain, powerForSustainedFireAll, heatForSustainedFireMain, heatForSustainedFireAll);

        SystemsInfoPanel.PowerCap.text = string.Format("{0}", powerCapacity);
        SystemsInfoPanel.PowerGen.text = string.Format("{0}/sec", powerGeneration * 4);
        SystemsInfoPanel.PowerConsumptionInCombat.text = string.Format("{0}/sec", powerConsumption * 4);
        SystemsInfoPanel.PowerConsumptionMax.text = string.Format("{0}/sec", (powerConsumption + powerHigh) * 4);

        SystemsInfoPanel.HeatCap.text = string.Format("{0}", heatCapacity);
        SystemsInfoPanel.Cooling.text = string.Format("{0}/sec", cooling * 4);
        SystemsInfoPanel.HeatGenInCombat.text = string.Format("{0}/sec", heatGeneration * 4);
        SystemsInfoPanel.HeatGenMax.text = string.Format("{0}/sec", (heatGeneration + heatHigh) * 4);

        SystemsInfoPanel.MainBatteryPower.text = string.Format("{0}", powerPerSalvoMain);
        SystemsInfoPanel.MainBatteryPowerSustained.text = string.Format("{0:0.#}/sec", powerForSustainedFireMain);
        SystemsInfoPanel.AllWeaponsPower.text = string.Format("{0}", powerPerSalvoAll);
        SystemsInfoPanel.AllWeaponsPowerSustained.text = string.Format("{0:0.#}/sec", powerForSustainedFireAll);

        SystemsInfoPanel.MainBatteryHeat.text = string.Format("{0}", heatPerSalvoMain);
        SystemsInfoPanel.MainBatteryHeatSustained.text = string.Format("{0:0.#}/sec", heatForSustainedFireMain);
        SystemsInfoPanel.AllWeaponsHeat.text = string.Format("{0}", heatPerSalvoAll);
        SystemsInfoPanel.AllWeaponsHeatSustained.text = string.Format("{0:0.#}/sec", heatForSustainedFireAll);
    }

    private int FindPairedTurret(List<TurretInEditor> hardpoints, int idx)
    {
        if (hardpoints[idx].IsEmpty)
        {
            return -1;
        }
        for (int i = 0; i < hardpoints.Count; ++i)
        {
            if (i == idx || hardpoints[i].IsEmpty)
                continue;

            if (Mathf.Approximately(hardpoints[i].Hardpoint.transform.localPosition.x, -hardpoints[idx].Hardpoint.transform.localPosition.x) &&
                Mathf.Approximately(hardpoints[i].Hardpoint.transform.localPosition.y, hardpoints[idx].Hardpoint.transform.localPosition.y) &
                Mathf.Approximately(hardpoints[i].Hardpoint.transform.localPosition.z, hardpoints[idx].Hardpoint.transform.localPosition.z))
                return i;
        }
        return -1;
    }

    private (Ship.ShipSection, int) FindOldComLocation(ShipEditorDraggable comp)
    {
        if (comp.CurrentLocation == EditorItemLocation.Ship)
        {
            Ship.ShipSection sec = comp.CurrentShipSection;
            if (sec == Ship.ShipSection.Hidden)
            {
                return (Ship.ShipSection.Hidden, -1);
            }
            List<ShipComponentTemplateDefinition> secComps = _currShipComps[sec];
            for (int i = 0; i < secComps.Count; i++)
            {
                if (secComps[i] == comp.ShipComponentDef)
                {
                    return (sec, i);
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

                GetShipQualityStats();
            }
        }
    }

    private static string[] CycleStringArr(string[] arr, string newStr)
    {
        if (arr == null)
        {
            return null;
        }
        else if (arr.Length == 0)
        {
            return new string[0];
        }
        else if (arr.Length == 1)
        {
            return new string[] { newStr };
        }
        else
        {
            string[] newArr = new string[arr.Length];
            for (int i = 1; i < arr.Length - 1; ++i)
            {
                newArr[i] = arr[i - 1];
            }
            newArr[0] = newStr;
            return newArr;
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
            ShipEditorDropTarget compsPanel;
            if (!_shipSectionPanels.TryGetValue(sec.Key, out compsPanel))
            {
                compsPanel = null;
            }
            for (int i = 0; i < sec.Value.Length; ++i)
            {
                _currShipComps[sec.Key].Add(null);
                if (compsPanel != null)
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

    public void ForceRefreshShipSections()
    {
        foreach (ShipEditorDropTarget compsPanel in _shipSectionPanels.Values)
        {
            StackingLayout2D stacking;
            if (compsPanel.TryGetComponent(out stacking))
            {
                stacking.ForceRefresh();
            }
        }
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

    public void ToggleWeaponControlCfgPanel()
    {
        bool prev = WeaponCfgPanel.gameObject.activeSelf;
        if (_currShip.HasValue)
        {
            WeaponCfgPanel.gameObject.SetActive(!prev);
            if (!prev && _weaponCfgPanelDirty)
            {
                WeaponCfgPanel.SetShipTemplate(_currShip.Value.HullDef);
                _weaponCfgPanelDirty = false;
            }
        }
        else
        {
            WeaponCfgPanel.gameObject.SetActive(false);
        }
    }

    public void SwapAmmoTypes()
    {
        if (_clickedHardpoint >= 0)
        {

        }
        else
        {
            if (_currAmmoTypes != null && _currAmmoTypes.Length > 1)
            {
                Array.Reverse(_currAmmoTypes);
                DrawPenCharts();
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        int hardpointIdx = RaycastDropWeapon(eventData.position);
        if (hardpointIdx >= 0)
        {
            _clickedHardpoint = hardpointIdx;
            DrawPenCharts(_currHardpoints[hardpointIdx].TurretDef, _currHardpoints[hardpointIdx].AmmoTypes);
        }
    }

    public void RandomShipClassName()
    {
        if (!_currShip.HasValue)
        {
            return;
        }

        ObjectFactory.ShipSize sz = (ObjectFactory.ShipSize) Enum.Parse(typeof(ObjectFactory.ShipSize), _currShip.Value.HullDef.ShipSize);
        string[] subcultures = new string[] { "British", "German", "Russian", "Pirate" };
        ShipDisplayName name =
            NamingSystem.GenShipName(ObjectFactory.GetCultureNames("Terran"),
                                     subcultures[UnityEngine.Random.Range(0, subcultures.Length)],
                                     ObjectFactory.InternalShipTypeToNameType(sz));
        ShipClassTextbox.text = name.ShortNameKey;
    }

    public ShipTemplate CompileShip()
    {
        if (!_currShip.HasValue)
        {
            return null;
        }

        ShipTemplate res = new ShipTemplate()
        {
            ShipHullProductionKey = _currShip.Value.Key,
            ShipComponents = new ShipTemplate.ShipComponentList[_currShipComps.Count],
        };

        int idx = 0;
        foreach (KeyValuePair<Ship.ShipSection, List<ShipComponentTemplateDefinition>> shipSecComponents in _currShipComps)
        {
            res.ShipComponents[idx++] = new ShipTemplate.ShipComponentList() { Section = shipSecComponents.Key, Components = shipSecComponents.Value.ToArray() };
        }

        int numWeapons = 0;
        for (int i = 0; i < _currHardpoints.Count; ++i)
        {
            if (!_currHardpoints[i].IsEmpty)
            {
                ++numWeapons;
            }
        }
        res.Turrets = new ShipTemplate.TemplateTurretPlacement[numWeapons];
        int weaponIdx = 0;
        for (int i = 0; i < _currHardpoints.Count; ++i)
        {
            if (!_currHardpoints[i].IsEmpty)
            {
                res.Turrets[i].HardpointKey = _currHardpoints[i].Hardpoint.name;
                res.Turrets[i].TurretType = _currHardpoints[i].TurretDef.TurretType;
                res.Turrets[i].WeaponType = _currHardpoints[i].TurretDef.WeaponType;
                res.Turrets[i].WeaponSize = _currHardpoints[i].TurretDef.WeaponSize;
                res.Turrets[i].WeaponNum = _currHardpoints[i].TurretDef.WeaponNum;
                res.Turrets[i].AlternatingFire = false; //TODO: implement
                res.Turrets[i].AmmoTypes = new string[_currHardpoints[i].AmmoTypes.Length];
                _currHardpoints[i].AmmoTypes.CopyTo(res.Turrets[i].AmmoTypes, 0);
                if (_currHardpoints[i].TurretMods == null || _currHardpoints[i].TurretMods.Length == 0)
                {
                    res.Turrets[i].InstalledMods = new TurretMod[] { TurretMod.None };
                }
                else
                {
                    res.Turrets[i].InstalledMods = _currHardpoints[i].TurretMods.Select(s => s != null ? (TurretMod) Enum.Parse(typeof(TurretMod), s) : TurretMod.None).ToArray();
                }
                ++weaponIdx;
            }
        }

        res.WeaponConfig = _weaponCfgPanelDirty ? WeaponControlGroupCfgPanel.DefaultForShip(_currShip.Value.HullDef) : WeaponCfgPanel.Compile();
        res.ShipClassName = ShipClassTextbox.text;

        return res;
    }

    public void CompileShip2()
    {
        CompileShip();
    }

    public void ExportShipDesign()
    {
        ShipTemplate template = CompileShip();
        string yamlShip = HierarchySerializer.SerializeObject(template);
        if (template.ShipClassName != string.Empty)
        {
            string savePath = System.IO.Path.Combine(Application.persistentDataPath, "ShipTemplates", template.ShipClassName + ".yml");
            System.IO.File.WriteAllText(savePath, yamlShip, System.Text.Encoding.UTF8);
        }
    }

    public RectTransform ShipClassesScrollViewContent;
    public RectTransform ShipHullsScrollViewContent;
    public RectTransform WeaponsScrollViewContent;
    public RectTransform AmmoScrollViewContent;
    public RectTransform TurretModsScrollViewContent;
    public RectTransform ShipCompsScrollViewContent;

    public RectTransform[] PenetrationGraphBoxes;
    public RectTransform ButtonPrototype;
    public RectTransform PlacedItemPrototype;
    public Transform HardpointMarkerPrototype;
    public Camera ImageCam;
    public Camera ShipViewCam;
    public RawImage ShipViewBox;
    public MultiToggleGroup AmmoToggleGroup;
    public MultiToggleGroup TurretModToggleGroup;
    public Button SwapAmmoButton;

    public ShipEditorShipSectionsPanel ShipSectionsPanel;
    public WeaponControlGroupCfgPanel WeaponCfgPanel;

    public Toggle[] ComponentDisplayToggleGroup;
    public Toggle[] WeaponsDisplayToggleGroup;

    public Color SlotFreeColor;
    public Color SlotIncompatibleColor;
    public Color SlotOccupiedColor;
    public Color SlotToAddColor;
    public Color AmmoSelectedColor;

    public ShipInfoPanel SystemsInfoPanel;

    public TMP_InputField ShipClassTextbox;

    [NonSerialized]
    private ShipDummyInEditor? _currShip = null;
    [NonSerialized]
    private List<TurretInEditor> _currHardpoints = new List<TurretInEditor>();
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
    private List<ShipEditorDraggable> _allTurretMods;
    [NonSerialized]
    private bool[] _allTurretModsComatibleFlags;

    private (AreaGraphRenderer, Image)[] PenetrationGraphs;

    [NonSerialized]
    private string _lastClickedAmmoType = null;
    [NonSerialized]
    private string[] _currAmmoTypes = null;
    [NonSerialized]
    private TurretDefinition _currWeapon = null;
    [NonSerialized]
    private string _lastClickedTurretMod = null;
    [NonSerialized]
    private string[] _currTurretMods = null;
    [NonSerialized]
    private int _clickedHardpoint = -1;

    private bool _weaponCfgPanelDirty = true;

    private static readonly string TurretModEmptyString = TurretMod.None.ToString();
    private static readonly string TurretModDuamAmmoString = TurretMod.DualAmmoFeed.ToString();

    private struct ShipDummyInEditor
    {
        public string Key;
        public Transform ShipModel;
        public ShipHullDefinition HullDef;
        public List<(TurretHardpoint, Collider, MeshRenderer)> Hardpoints;
    }

    private struct TurretInEditor
    {
        public TurretHardpoint Hardpoint;
        public TurretDefinition TurretDef;
        public Transform TurretModel;
        public string[] AmmoTypes;
        public string[] TurretMods;

        public bool IsEmpty => TurretDef == null;

        public static TurretInEditor FromEmptyHardpoint(TurretHardpoint hp)
        {
            return new TurretInEditor()
            {
                Hardpoint = hp,
                TurretDef = null,
                TurretModel = null,
                AmmoTypes = null,
                TurretMods = null
            };
        }

        public TurretInEditor ReplaceAmmo(string [] newAmmo)
        {
            return new TurretInEditor()
            {
                Hardpoint = this.Hardpoint,
                TurretDef = this.TurretDef,
                TurretModel = this.TurretModel,
                AmmoTypes = newAmmo,
                TurretMods = this.TurretMods
            };
        }

        public TurretInEditor ReplaceTurretMods(string[] newTurretMods)
        {
            return new TurretInEditor()
            {
                Hardpoint = this.Hardpoint,
                TurretDef = this.TurretDef,
                TurretModel = this.TurretModel,
                AmmoTypes = this.AmmoTypes,
                TurretMods = newTurretMods
            };
        }
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

    [Serializable]
    public struct ShipInfoPanel
    {
        public TextMeshProUGUI PowerGen;
        public TextMeshProUGUI PowerCap;
        public TextMeshProUGUI PowerConsumptionInCombat;
        public TextMeshProUGUI PowerConsumptionMax;

        public TextMeshProUGUI Cooling;
        public TextMeshProUGUI HeatCap;
        public TextMeshProUGUI HeatGenInCombat;
        public TextMeshProUGUI HeatGenMax;

        public TextMeshProUGUI MainBatteryPower;
        public TextMeshProUGUI MainBatteryPowerSustained;
        public TextMeshProUGUI AllWeaponsPower;
        public TextMeshProUGUI AllWeaponsPowerSustained;

        public TextMeshProUGUI MainBatteryHeat;
        public TextMeshProUGUI MainBatteryHeatSustained;
        public TextMeshProUGUI AllWeaponsHeat;
        public TextMeshProUGUI AllWeaponsHeatSustained;
    }
}