using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using System;

public class StatusTopLevel : MonoBehaviour
{
    public void AttachShip(Ship s)
    {
        if (_attachedShip != null)
        {
            DetachShip();
        }
        _attachedShip = s;
        if (_attachedShip != null)
        {
            // Take a snapshot of the ship
            //UnityEngine.UI.Image ImgObj = ShipPhoto;
            int ImgH = 512, ImgW = 512;
            float scaleFactor = 1.2f;
            float requiredSize = scaleFactor * Mathf.Max(_attachedShip.ShipLength, _attachedShip.ShipWidth);
            Vector3 shipAxis = _attachedShip.transform.up;
            Vector3 downDir = Vector3.down;
            Camera cam = ObjectFactory.GetShipStatusPanelCamera();
            cam.transform.position = _attachedShip.transform.position + (_attachedShip.transform.up * ShipLenCenter(_attachedShip));
            float height = requiredSize * 0.5f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            cam.transform.position += new Vector3(0, height, 0);
            cam.transform.rotation = Quaternion.LookRotation(downDir, shipAxis);

            Texture2D shipImg;
            shipImg = new Texture2D(ImgW, ImgH);
            RenderTexture rt = RenderTexture.GetTemporary(ImgW, ImgH);
            cam.enabled = true;
            cam.targetTexture = rt;
            RenderTexture orig = RenderTexture.active;
            RenderTexture.active = rt;
            cam.Render();
            shipImg.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            shipImg.Apply();

            Sprite sp = Sprite.Create(shipImg, new Rect(0, 0, ImgW, ImgH), new Vector2(0, 0));
            ShipPhoto.sprite = sp;

            RectTransform panelRect = ShipPhoto.GetComponent<RectTransform>();
            foreach (TurretHardpoint hp in _attachedShip.WeaponHardpoints)
            {
                Vector3 hpPos = cam.WorldToViewportPoint(hp.transform.position);
                string hardointName = hp.name;
                TurretBase currTurret = hp.GetComponentInChildren<TurretBase>();
                if (currTurret != null)
                {
                    StatusSubsystem compStatus = ObjectFactory.CreateStatusSubsytem(currTurret);
                    compStatus.transform.SetParent(panelRect);
                    compStatus.transform.localPosition = new Vector2(hpPos.x * panelRect.rect.width + panelRect.rect.xMin , hpPos.y * panelRect.rect.height + panelRect.rect.yMin);
                    StatusProgressBar compProgressRing = ObjectFactory.CreateSubsytemProgressRing(currTurret);
                    compProgressRing.transform.SetParent(panelRect);
                    compProgressRing.transform.localPosition = new Vector2(hpPos.x * panelRect.rect.width + panelRect.rect.xMin, hpPos.y * panelRect.rect.height + panelRect.rect.yMin);
                    switch (currTurret.TurretSize)
                    {
                        case ObjectFactory.WeaponSize.Light:
                            break;
                        case ObjectFactory.WeaponSize.Medium:
                            compStatus.transform.localScale = compStatus.transform.localScale * 1.25f;
                            break;
                        case ObjectFactory.WeaponSize.Heavy:
                            compStatus.transform.localScale = compStatus.transform.localScale * 1.5f;
                            break;
                        case ObjectFactory.WeaponSize.TorpedoTube:
                            compStatus.transform.localScale = compStatus.transform.localScale * 1.5f;
                            break;
                        case ObjectFactory.WeaponSize.StrikeCraft:
                            break;
                        default:
                            break;
                    }
                    compProgressRing.transform.localScale = compStatus.transform.localScale;
                    _turretProgressBars.Add(currTurret, compProgressRing);
                }
            }

            RenderTexture.active = orig;
            RenderTexture.ReleaseTemporary(rt);
            cam.enabled = false;


            foreach (IShipActiveComponent comp in _attachedShip.AllComponents.Where(x => x is IShipActiveComponent && !(x is TurretBase) && x.AllowedSlotTypes.All(y => y != ComponentSlotType.Hidden)).Select(z => z as IShipActiveComponent))
            {
                StatusSubsystem compStatus = ObjectFactory.CreateStatusSubsytem(comp);
                RectTransform compRT = compStatus.GetComponent<RectTransform>();
                compRT.SetParent(CompsPanel);
            }

            HealthBar.MaxValue = _attachedShip.MaxHullHitPoints;
            ShieldBar.MaxValue = _attachedShip.ShipTotalMaxShields;
            EnergyBar.MaxValue = _attachedShip.MaxEnergy;
            HeatBar.MaxValue = _attachedShip.MaxHeat;

            SetStrikeCraftStatus();
        }
        else // _attachedShip == null
        {
            _attachedCarrier = null;
        }
    }

    private void SetStrikeCraftStatus()
    {
        _attachedCarrier = _attachedShip.GetComponent<CarrierBehavior>();
        if (_attachedCarrier == null)
        {
            HangerPanel.gameObject.SetActive(false);
            return;
        }
        HangerPanel.gameObject.SetActive(true);
        if (_attachedCarrier.ActiveFormations != null)
        {
            NumActiveStrikeCraftBox.text = string.Format("{0}/{1}", _attachedCarrier.ActiveFormations.Count(), _attachedCarrier.MaxFormations);
        }
        else
        {
            NumActiveStrikeCraftBox.text = string.Format("{0}/{1}", 0, _attachedCarrier.MaxFormations);
        }
        _attachedCarrier.OnLaunchStart += CarrierEventWrapper;
        _attachedCarrier.OnLaunchFinish += CarrierEventWrapper;
        _attachedCarrier.OnRecoveryStart += CarrierEventWrapper;
        _attachedCarrier.OnRecoveryFinish += CarrierEventWrapper;
        _attachedCarrier.OnFormationRemoved += CarrierFormationEventWrapper;
    }

    public void SetName(ShipDisplayName dn)
    {
        ShortNameBox.text = dn.ShortName;
        FullNameBox.text = dn.FullName;
        FluffBox.text = dn.Fluff;
    }

    private static float ShipLenCenter(ShipBase sb)
    {
        Mesh m = sb.HullObject.GetComponent<MeshFilter>().mesh;
        float localCenter = (m.bounds.max.y + m.bounds.min.y) / 2.0f;
        return localCenter * sb.HullObject.transform.lossyScale.y;
    }

    public void DetachShip()
    {
        _turretProgressBars.Clear();
        if (_attachedCarrier != null)
        {
            _attachedCarrier.OnLaunchStart -= CarrierEventWrapper;
            _attachedCarrier.OnLaunchFinish -= CarrierEventWrapper;
            _attachedCarrier.OnRecoveryStart -= CarrierEventWrapper;
            _attachedCarrier.OnRecoveryFinish -= CarrierEventWrapper;
            _attachedCarrier.OnFormationRemoved -= CarrierFormationEventWrapper;
            _attachedCarrier = null;
        }
    }

    public void ForceUpdateTurretModes()
    {
        if (_attachedShip == null)
        {
            return;
        }

        foreach (KeyValuePair<TurretBase, StatusProgressBar> t in _turretProgressBars)
        {
            switch (t.Key.Mode)
            {
                case TurretBase.TurretMode.Off:
                    t.Value.SetColor(_offTurretColor);
                    break;
                case TurretBase.TurretMode.Manual:
                    t.Value.SetColor(_manualTurretColor);
                    break;
                case TurretBase.TurretMode.Auto:
                    t.Value.SetColor(_autoTurretColor);
                    break;
                default:
                    break;
            }
        }
    }

    private void ForceUpdateHangerStatus()
    {
        NumActiveStrikeCraftBox.text = string.Format("{0}/{1}", _attachedCarrier.ActiveFormations.Count(), _attachedCarrier.MaxFormations);
    }

    private void CarrierEventWrapper(CarrierBehavior c)
    {
        if (c == _attachedCarrier)
        {
            ForceUpdateHangerStatus();
        }
        else
        {
            Debug.LogWarning("Got update from carrier other than the one attached. This is probably incorrect.");
        }
    }
    private void CarrierFormationEventWrapper(CarrierBehavior c, StrikeCraftFormation f)
    {
        CarrierEventWrapper(c);
    }

    void Update()
    {
        if (_attachedShip != null)
        {
            HealthBar.Value = _attachedShip.HullHitPoints;
            ShieldBar.Value = _attachedShip.ShipTotalShields;
            EnergyBar.Value = _attachedShip.Energy;
            HeatBar.Value = _attachedShip.Heat;
        }
    }

    public string ShipProductionKey;
    public Ship AttachedShip { get { return _attachedShip; } }

    private Ship _attachedShip = null;
    private CarrierBehavior _attachedCarrier = null;
    private Dictionary<TurretBase, StatusProgressBar> _turretProgressBars = new Dictionary<TurretBase, StatusProgressBar>();
    public UnityEngine.UI.Image ShipPhoto;
    public GradientBar HealthBar;
    public GradientBar ShieldBar;
    public GradientBar EnergyBar;
    public GradientBar HeatBar;
    public RectTransform CompsPanel;
    public TextMeshProUGUI ShortNameBox;
    public TextMeshProUGUI FullNameBox;
    public TextMeshProUGUI FluffBox;
    public TextMeshProUGUI NumActiveStrikeCraftBox;
    public TextMeshProUGUI NumFightersBox;
    public TextMeshProUGUI NumBombersBox;
    public Transform HangerPanel;
    private static readonly Color _autoTurretColor = new Color(83f / 255f, 198f / 255f, 255f / 255f);
    private static readonly Color _manualTurretColor = new Color(0f / 255f, 0f / 255f, 120f / 255f);
    private static readonly Color _offTurretColor = Color.black;
}
