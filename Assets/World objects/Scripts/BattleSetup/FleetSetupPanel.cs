using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;

public class FleetSetupPanel : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(Populate());
        PopulateCultures();
    }

    private IEnumerator Populate()
    {
        StackingLayout stackingBehavior = AvailableShipsScrollContent.GetComponent<StackingLayout>();
        stackingBehavior.AutoRefresh = false;

        string[] shipClasses = ObjectFactory.GetAllShipClassTemplates().ToArray();
        float offset = 0.0f;
        _availableShipClasses = new Dictionary<string, AvailableShipItem>();
        for (int i = 0; i < shipClasses.Length; ++i)
        {
            ShipTemplate template = ObjectFactory.GetShipClassTemplate(shipClasses[i]);

            RectTransform t = Instantiate(AvailableShipItemTemplate);

            t.SetParent(AvailableShipsScrollContent, false);
            float height = t.rect.height;
            float pivotOffset = t.pivot.x * t.rect.width;
            t.anchoredPosition = new Vector2(pivotOffset, 0);
            offset -= height;

            TextMeshProUGUI textElem = t.GetComponentInChildren<TextMeshProUGUI>();
            string classKey = shipClasses[i];
            string shipType = string.Format("{0} class - {1}", classKey, ObjectFactory.GetShipHullDefinition(template.ShipHullProductionKey).ShipType);
            textElem.text = shipType;

            Button buttonElem = t.GetComponent<Button>();
            buttonElem.onClick.AddListener(() => SelectShipClass(classKey));

            Image img = buttonElem.GetComponentInChildrenOneLevel<Image>();

            AvailableShipItem newItem = new AvailableShipItem()
            {
                ProducationKey = shipClasses[i],
                ShipButtonInScrollView = t,
                Template = template,
                SpriteKey = shipClasses[i] + "class"
            };
            img.sprite = GetShipSprite(newItem);
            _availableShipClasses.Add(shipClasses[i], newItem);
            yield return _frameWait;
        }

        AvailableShipsScrollContent.sizeDelta = new Vector2(AvailableShipsScrollContent.sizeDelta.x, -offset);
        stackingBehavior.ForceRefresh();
        stackingBehavior.AutoRefresh = true;
        yield return null;
    }

    private Sprite GetShipSprite(AvailableShipItem item)
    {
        Sprite shipPhoto;

        string photoKey = item.SpriteKey;
        if (ObjectFactory.GetGenericPhoto(photoKey, out shipPhoto))
        {
            return shipPhoto;
        }

        Transform s = ObjectFactory.CreateShipDummy(item.Template.ShipHullProductionKey);
        TurretHardpoint[] hardpoints = s.GetComponentsInChildren<TurretHardpoint>();
        for (int i = 0; i < item.Template.Turrets.Length; ++i)
        {
            ShipTemplate.TemplateTurretPlacement currTurret = item.Template.Turrets[i];
            Transform dummyTurret = ObjectFactory.CreateTurretDummy(currTurret.TurretType, currTurret.WeaponNum, currTurret.WeaponSize, currTurret.WeaponType);
            foreach (TurretHardpoint hp in hardpoints)
            {
                if (hp.name == currTurret.HardpointKey)
                {
                    dummyTurret.SetParent(hp.transform, false);
                    dummyTurret.localScale = Vector3.one;
                    dummyTurret.localPosition = Vector3.zero;
                    Quaternion q = Quaternion.LookRotation(-hp.transform.up, hp.transform.forward);
                    dummyTurret.transform.rotation = q;
                    break;
                }
            }
        }
        shipPhoto = ObjectFactory.GetObjectPhoto(s, true, ImageCam);
        ObjectFactory.RegisterGenericPhoto(photoKey, shipPhoto);
        Destroy(s.gameObject);
        return shipPhoto;
    }

    private void SelectShipClass(string className)
    {
        AvailableShipItem selectedShip = _availableShipClasses[className];
        Sprite shipSprite = null;
        ObjectFactory.GetGenericPhoto(selectedShip.SpriteKey, out shipSprite);
        CurrSelectedShipImage.sprite = shipSprite;
        bool changed = _selectedShipKey != className;
        _selectedShipKey = className;
        if (changed)
        {
            RandomShipName();
        }
    }

    public void RandomShipName()
    {
        if (_selectedShipKey == null)
        {
            return;
        }
        AvailableShipItem selectedShip = _availableShipClasses[_selectedShipKey];
        ShipHullDefinition hullDef = ObjectFactory.GetShipHullDefinition(selectedShip.Template.ShipHullProductionKey);
        ObjectFactory.ShipSize sz = (ObjectFactory.ShipSize) Enum.Parse(typeof(ObjectFactory.ShipSize), hullDef.ShipSize);
        string[] subcultures = new string[] { "British", "German", "Russian", "Pirate" };
        _selectedShipName =
            NamingSystem.GenShipName(ObjectFactory.GetCultureNames("Terran"),
                                     CultureDropdown.options[CultureDropdown.value].text,
                                     ObjectFactory.InternalShipTypeToNameType(sz),
                                     AllUsedShipNames);
        _shipNameEditLock = true;
        ShipShortNameField.text = _selectedShipName.ShortName;
        ShipLongNameField.text = _selectedShipName.FullName;
        _shipNameEditLock = false;
    }

    public void EditShipName()
    {
        if (!_shipNameEditLock)
        {
            _shipNameEditLock = true;
            _selectedShipName.ShortName = ShipShortNameField.text;
            if (ShipLongNameModeToggle.isOn)
            {
                _selectedShipName.FullName = ShipLongNameField.text;
            }
            else
            {
                _selectedShipName.FullName = ShipLongNameField.text = _selectedShipName.ShortName;
            }
            _selectedShipName.ShortNameKey = _selectedShipName.ShortName;
            _selectedShipName.FullNameKey = _selectedShipName.FullName;
            _selectedShipName.Fluff = string.Empty;
            _shipNameEditLock = false;
        }
    }

    public void ChangedLongNameMode(bool trash)
    {
        ShipLongNameField.interactable = ShipLongNameModeToggle.isOn;
    }

    private void PopulateCultures()
    {
        List<string> subcultures = new List<string> { "British", "German", "Russian", "Pirate" };
        CultureDropdown.AddOptions(subcultures);
    }

    public void AddShipToFleet()
    {
        if (_selectedShipKey != null)
        {
            StackingLayout stackingBehavior = SelectedShipsScrollContent.GetComponent<StackingLayout>();
            stackingBehavior.AutoRefresh = false;

            AvailableShipItem selectedShip = _availableShipClasses[_selectedShipKey];
            ShipShadow shadow = selectedShip.Template.ToNewShip();

            ShipHullDefinition hullDef = ObjectFactory.GetShipHullDefinition(selectedShip.Template.ShipHullProductionKey);

            int numCrew = (hullDef.OperationalCrew + hullDef.MaxCrew) / 2;//userShip ? (s.OperationalCrew + s.MaxCrew) / 2 : (s.OperationalCrew + s.SkeletonCrew) / 2;
            shadow.Crew = new ShipCharacter[numCrew];
            for (int i = 0; i < numCrew; ++i)
            {
                ShipCharacter currCrew = ShipCharacter.GenerateTerranShipCrew();
                currCrew.Level = ShipCharacter.CharacterLevel.Trained;
                shadow.Crew[i] = currCrew;
            }

            shadow.DisplayName = _selectedShipName;

            RectTransform t = Instantiate(SelectedShipItemTemplate);

            t.SetParent(SelectedShipsScrollContent, false);
            float height = t.rect.height;
            float pivotOffset = t.pivot.x * t.rect.width;
            t.anchoredPosition = new Vector2(pivotOffset, 0);

            TextMeshProUGUI textElem = t.GetComponentInChildren<TextMeshProUGUI>();
            textElem.text = _selectedShipName.ShortName;

            Button buttonElem = t.GetComponentInChildren<Button>();
            buttonElem.onClick.AddListener(() => RemoveShip(shadow));

            Image img = t.Find("Image").GetComponent<Image>();
            Sprite shipSprite = null;
            ObjectFactory.GetGenericPhoto(selectedShip.SpriteKey, out shipSprite);
            img.sprite = shipSprite;

            ShipInFleetItem newItem = new ShipInFleetItem()
            {
                Shadow = shadow,
                ShipButtonInScrollView = t
            };

            _selectedFleet.Add(newItem);

            UpdateSelectedFleetContentBoxSize();
            stackingBehavior.ForceRefresh();
            stackingBehavior.AutoRefresh = true;
        }
    }

    private void RemoveShip(ShipShadow shadow)
    {
        int idx = -1;
        for (int i = 0; i < _selectedFleet.Count; ++i)
        {
            if (_selectedFleet[i].Shadow == shadow)
            {
                idx = i;
                break;
            }
        }
        if (idx >= 0)
        {
            Destroy(_selectedFleet[idx].ShipButtonInScrollView.gameObject);
            _selectedFleet.RemoveAt(idx);
            UpdateSelectedFleetContentBoxSize();
            //StackingLayout stackingBehavior = SelectedShipsScrollContent.GetComponent<StackingLayout>();
            //stackingBehavior.ForceRefresh();
        }
    }

    private void UpdateSelectedFleetContentBoxSize()
    {
        float totalHeight = 0.0f;
        for (int i = 0; i < _selectedFleet.Count; ++i)
        {
            totalHeight += _selectedFleet[i].ShipButtonInScrollView.rect.height;
        }
        SelectedShipsScrollContent.sizeDelta = new Vector2(SelectedShipsScrollContent.sizeDelta.x, totalHeight);
    }

    private IEnumerable<string> AllUsedShipNames => _selectedFleet.Select(s => s.Shadow.DisplayName.FullNameKey);

    public IEnumerable<ShipShadow> SelectedFleet => _selectedFleet.Select(s => s.Shadow);

    public TMP_Dropdown CultureDropdown;
    public RectTransform AvailableShipsScrollContent;
    public RectTransform SelectedShipsScrollContent;
    public Image CurrSelectedShipImage;
    public RectTransform AvailableShipItemTemplate;
    public RectTransform SelectedShipItemTemplate;
    public TMP_InputField ShipShortNameField;
    public TMP_InputField ShipLongNameField;
    public Toggle ShipLongNameModeToggle;
    public Camera ImageCam;

    private Dictionary<string, AvailableShipItem> _availableShipClasses;
    private List<ShipInFleetItem> _selectedFleet = new List<ShipInFleetItem>();
    private string _selectedShipKey = null;
    private ShipDisplayName _selectedShipName = new ShipDisplayName();

    private static readonly WaitForEndOfFrame _frameWait = new WaitForEndOfFrame();

    private bool _shipNameEditLock = false; // Ugly hack

    private struct AvailableShipItem
    {
        public RectTransform ShipButtonInScrollView;
        public string ProducationKey;
        public string SpriteKey;
        public ShipTemplate Template;
    }

    private struct ShipInFleetItem
    {
        public RectTransform ShipButtonInScrollView;
        public ShipShadow Shadow;
    }
}
