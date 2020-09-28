using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class FleetSetupPanel : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(Populate());
    }

    private IEnumerator Populate()
    {
        StackingLayout stackingBehavior = AvailableShipsScrollContent.GetComponent<StackingLayout>();
        stackingBehavior.AutoRefresh = false;

        string[] shipClasses = ObjectFactory.GetAllShipClassTemplates().ToArray();
        float offset = 0.0f;
        _availableShipClasses = new AvailableShipItem[shipClasses.Length];
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
            string classKey = shipClasses[i] + " class";
            string shipType = string.Format("{0} - {1}", classKey, ObjectFactory.GetShipHullDefinition(template.ShipHullProductionKey).ShipType);
            textElem.text = shipType;

            Button buttonElem = t.GetComponent<Button>();
            //buttonElem.onClick.AddListener(() => SelectShipDummy(hullKey));


            Image img = buttonElem.GetComponentInChildrenOneLevel<Image>();
            img.sprite = GetShipSprite(template);

            _availableShipClasses[i] = new AvailableShipItem()
            {
                ProducationKey = shipClasses[i],
                ShipButtonInScrollView = t,
                ShipText = textElem,
                Template = template
            };
            yield return _frameWait;
        }

        AvailableShipsScrollContent.sizeDelta = new Vector2(AvailableShipsScrollContent.sizeDelta.x, -offset);
        stackingBehavior.ForceRefresh();
        stackingBehavior.AutoRefresh = true;
        yield return null;
    }

    private Sprite GetShipSprite(ShipTemplate template)
    {
        Sprite shipPhoto;

        string photoKey = template.ShipClassName + "class";
        if (ObjectFactory.GetGenericPhoto(photoKey, out shipPhoto))
        {
            return shipPhoto;
        }

        Transform s = ObjectFactory.CreateShipDummy(template.ShipHullProductionKey);
        TurretHardpoint[] hardpoints = s.GetComponentsInChildren<TurretHardpoint>();
        for (int i = 0; i < template.Turrets.Length; ++i)
        {
            ShipTemplate.TemplateTurretPlacement currTurret = template.Turrets[i];
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

    public void RandomShipName()
    {

    }

    public RectTransform AvailableShipsScrollContent;
    public RectTransform SelectedShipsScrollContent;
    public Image CurrSelectedShipImage;
    public RectTransform AvailableShipItemTemplate;
    public RectTransform SelectedShipItemTemplate;
    public TMP_InputField ShipNameField;
    public Camera ImageCam;

    private AvailableShipItem[] _availableShipClasses;

    private static readonly WaitForEndOfFrame _frameWait = new WaitForEndOfFrame();

    private struct AvailableShipItem
    {
        public RectTransform ShipButtonInScrollView;
        public TextMeshProUGUI ShipText;
        public string ProducationKey;
        public ShipTemplate Template;
    }
}
