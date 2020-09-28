using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using System;
using System.Text;

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
            int ImgH = 512, ImgW = 512;
            ValueTuple<Sprite, IEnumerable<ValueTuple<TurretHardpoint, Vector3>>>
                spriteAndTurrets = ShipPhotoUtil.TakePhotoWithTurretPos(_attachedShip, ImgH, ImgW);

            ShipPhoto.sprite = spriteAndTurrets.Item1;

            RectTransform panelRect = ShipPhoto.GetComponent<RectTransform>();
            foreach (ValueTuple<TurretHardpoint, Vector3> t in spriteAndTurrets.Item2)
            {
                Vector3 hpPos = t.Item2;
                string hardointName = t.Item1.name;
                TurretBase currTurret = t.Item1.GetComponentInChildren<TurretBase>();
                if (currTurret != null)
                {
                    StatusSubsystem compStatus = ObjectFactory.CreateStatusSubsytem(currTurret);
                    compStatus.transform.SetParent(panelRect);
                    compStatus.transform.localPosition = new Vector2(hpPos.x * panelRect.rect.width + panelRect.rect.xMin , hpPos.y * panelRect.rect.height + panelRect.rect.yMin);
                    StatusProgressBar compProgressRing = ObjectFactory.CreateSubsytemProgressRing(currTurret);
                    compProgressRing.transform.SetParent(panelRect);
                    compProgressRing.transform.localPosition = new Vector2(hpPos.x * panelRect.rect.width + panelRect.rect.xMin, hpPos.y * panelRect.rect.height + panelRect.rect.yMin);
                    switch (currTurret.TurretWeaponSize)
                    {
                        case "Light":
                            break;
                        case "Medium":
                            compStatus.transform.localScale = compStatus.transform.localScale * 1.25f;
                            break;
                        case "Heavy":
                            compStatus.transform.localScale = compStatus.transform.localScale * 1.5f;
                            break;
                        case "TorpedoTube":
                            compStatus.transform.localScale = compStatus.transform.localScale * 1.5f;
                            break;
                        default:
                            break;
                    }
                    compProgressRing.transform.localScale = compStatus.transform.localScale;
                    _turretProgressBars.Add(currTurret, compProgressRing);
                }
            }

            foreach (IShipActiveComponent comp in _attachedShip.AllComponents.Where(x => x is IShipActiveComponent && !(x is TurretBase) && x.AllowedSlotTypes.All(y => y != "Hidden")).Select(z => z as IShipActiveComponent))
            {
                StatusSubsystem compStatus = ObjectFactory.CreateStatusSubsytem(comp);
                RectTransform compRT = compStatus.GetComponent<RectTransform>();
                compRT.SetParent(CompsPanel);
            }

            HealthBar.MaxValue = _attachedShip.MaxHullHitPoints;
            ShieldBar.MaxValue = _attachedShip.ShipTotalMaxShields;
            EnergyBar.MaxValue = _attachedShip.MaxEnergy;
            HeatBar.MaxValue = _attachedShip.MaxHeat;

            SetWeaponsText();
        }
    }

    public void SetName(ShipDisplayName dn)
    {
        ShortNameBox.text = dn.ShortName;
        FullNameBox.text = dn.FullName;
        FluffBox.text = dn.Fluff;
    }

    public void DetachShip()
    {
        _turretProgressBars.Clear();
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
        SetWeaponsText();
    }

    private void SetWeaponsText()
    {
        _weaponsTextBuf.Clear();
        foreach (TurretBase t in _turretProgressBars.Keys)
        {
            if (t.Mode == TurretBase.TurretMode.Manual)
            {
                if (t is GunTurret gt)
                {
                    _weaponsTextBuf.AppendFormat("{0}x {1} {2}, {3}", gt.NumBarrels, gt.TurretWeaponSize, gt.TurretWeaponType, gt.SelectedAmmoType).AppendLine();
                }
                else if (t is TorpedoTurret tt)
                {
                    _weaponsTextBuf.AppendFormat("{0}, {1}", tt.TurretWeaponType, tt.LoadedTorpedoType).AppendLine();
                }
                else
                {
                    _weaponsTextBuf.AppendFormat("{0}x {1} {2}", t.NumBarrels, t.TurretWeaponSize, t.TurretWeaponType).AppendLine();
                }
            }
        }
        WeaponsBox.text = _weaponsTextBuf.ToString();
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
    private Dictionary<TurretBase, StatusProgressBar> _turretProgressBars = new Dictionary<TurretBase, StatusProgressBar>();
    private StringBuilder _weaponsTextBuf = new StringBuilder();
    public UnityEngine.UI.Image ShipPhoto;
    public GradientBar HealthBar;
    public GradientBar ShieldBar;
    public GradientBar EnergyBar;
    public GradientBar HeatBar;
    public RectTransform CompsPanel;
    public TextMeshProUGUI ShortNameBox;
    public TextMeshProUGUI FullNameBox;
    public TextMeshProUGUI FluffBox;
    public TextMeshProUGUI WeaponsBox;
    private static readonly Color _autoTurretColor = new Color(83f / 255f, 198f / 255f, 255f / 255f);
    private static readonly Color _manualTurretColor = new Color(0f / 255f, 0f / 255f, 120f / 255f);
    private static readonly Color _offTurretColor = Color.black;
}
