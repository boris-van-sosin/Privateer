using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurretInfoPanel : MonoBehaviour
{
    public void OpenWithTurret(ShipEditor.TurretInEditor srcTurret, string[] allAmmoTypes, Action<ShipEditor.TurretInEditor> callback)
    {
        gameObject.SetActive(true);
        float firingInterval;
        (_, _, firingInterval) = ObjectFactory.GetWeaponPowerConsumption(srcTurret.TurretDef.WeaponType, srcTurret.TurretDef.WeaponSize);

        if (string.IsNullOrEmpty(srcTurret.TurretDef.WeaponNum))
        {
            TurretTitleText.text = string.Format("{0}, {1} {2}", srcTurret.TurretDef.TurretType, srcTurret.TurretDef.WeaponSize, srcTurret.TurretDef.WeaponType);
        }
        else
        {
            TurretTitleText.text = string.Format("{0}, {1} x {2} {3}", srcTurret.TurretDef.TurretType, srcTurret.TurretDef.WeaponNum, srcTurret.TurretDef.WeaponSize, srcTurret.TurretDef.WeaponType);
        }

        ShipEditor.DrawPenCharts(srcTurret.TurretDef, srcTurret.AmmoTypes, PenetrationGraphBoxes, SwapAmmoButton, allAmmoTypes);

        int numWeapons = 1;
        if (!string.IsNullOrEmpty(srcTurret.TurretDef.WeaponNum))
        {
            numWeapons = int.Parse(srcTurret.TurretDef.WeaponNum);
        }

        for (int i = 0; i < FirepowerBoxes.Length; ++i)
        {
            if (i < srcTurret.AmmoTypes.Length)
            {
                FirepowerBoxes[i].gameObject.SetActive(true);
                Warhead w;
                switch (srcTurret.TurretDef.BehaviorType)
                {
                    case ObjectFactory.WeaponBehaviorType.Gun:
                        w = ObjectFactory.CreateWarhead(srcTurret.TurretDef.WeaponType, srcTurret.TurretDef.WeaponSize, srcTurret.AmmoTypes[i]);
                        break;
                    case ObjectFactory.WeaponBehaviorType.Torpedo:
                    case ObjectFactory.WeaponBehaviorType.BomberTorpedo:
                        w = ObjectFactory.CreateWarhead(srcTurret.AmmoTypes[i]);
                        break;
                    case ObjectFactory.WeaponBehaviorType.Beam:
                    case ObjectFactory.WeaponBehaviorType.ContinuousBeam:
                    case ObjectFactory.WeaponBehaviorType.Special:
                        w = ObjectFactory.CreateWarhead(srcTurret.TurretDef.WeaponType, srcTurret.TurretDef.WeaponSize);
                        break;
                    default:
                        w = new Warhead();
                        break;
                }
                FirepowerBoxes[i].ArmourDamage.text = string.Format("{0}\n{1:0.#}/s", w.ArmourDamage, w.ArmourDamage * numWeapons / firingInterval);
                FirepowerBoxes[i].ArmourPenetration.text = w.ArmourPenetration.ToString();
                FirepowerBoxes[i].HullDamage.text = string.Format("{0}\n{1:0.#}/s", w.HullDamage, w.HullDamage * numWeapons / firingInterval);
                FirepowerBoxes[i].ShieldDamage.text = string.Format("{0}\n{1:0.#}/s", w.ShieldDamage, w.ShieldDamage * numWeapons / firingInterval);
                FirepowerBoxes[i].CompDamage.text = string.Format("{0}\n{1:0.#}/s", w.SystemDamage, w.SystemDamage * numWeapons / firingInterval);
                FirepowerBoxes[i].CompsHit.text = w.HitMultiplicity.ToString();
                FirepowerBoxes[i].StrikeCraftDamage.text = string.Format("{0:0.##}", w.EffectVsStrikeCraft);
                FirepowerBoxes[i].BlastRadius.text = string.Format("{0:0.##}", w.BlastRadius);
            }
            else
            {
                FirepowerBoxes[i].gameObject.SetActive(false);
            }
        }

        if (numWeapons > 1)
        {
            AlternatignFireToggle.interactable = true;
            AlternatignFireToggle.isOn = srcTurret.AlternatingFire;
        }
        else
        {
            AlternatignFireToggle.interactable = false;
            AlternatignFireToggle.isOn = srcTurret.AlternatingFire;
        }

        _resultingTurret = new ShipEditor.TurretInEditor()
        {
            TurretDef = srcTurret.TurretDef,
            Hardpoint = srcTurret.Hardpoint,
            TurretModel = srcTurret.TurretModel,
            TurretMods = srcTurret.TurretMods,
            AmmoTypes = new string[srcTurret.AmmoTypes.Length]
        };
        srcTurret.AmmoTypes.CopyTo(_resultingTurret.AmmoTypes, 0);

        _allAmmoTypes = allAmmoTypes;
        _callback = callback;
    }

    public void SwapAmmoInPanel()
    {
        Array.Reverse(_resultingTurret.AmmoTypes);

        ShipEditor.DrawPenCharts(_resultingTurret.TurretDef, _resultingTurret.AmmoTypes, PenetrationGraphBoxes, SwapAmmoButton, _allAmmoTypes);

        SwapLabels(FirepowerBoxes[0].ArmourDamage, FirepowerBoxes[1].ArmourDamage);
        SwapLabels(FirepowerBoxes[0].ArmourPenetration, FirepowerBoxes[1].ArmourPenetration);
        SwapLabels(FirepowerBoxes[0].HullDamage, FirepowerBoxes[1].HullDamage);
        SwapLabels(FirepowerBoxes[0].ShieldDamage, FirepowerBoxes[1].ShieldDamage);
        SwapLabels(FirepowerBoxes[0].CompDamage, FirepowerBoxes[1].CompDamage);
        SwapLabels(FirepowerBoxes[0].CompsHit, FirepowerBoxes[1].CompsHit);
        SwapLabels(FirepowerBoxes[0].StrikeCraftDamage, FirepowerBoxes[1].StrikeCraftDamage);
        SwapLabels(FirepowerBoxes[0].BlastRadius, FirepowerBoxes[1].BlastRadius);
    }

    private static void SwapLabels(TextMeshProUGUI text1, TextMeshProUGUI text2)
    {
        string tmp = text1.text;
        text1.text = text2.text;
        text2.text = tmp;
    }

    public void ApplyAndClsoe()
    {
        _resultingTurret.AlternatingFire = AlternatignFireToggle.isOn;
        _callback(_resultingTurret);
        gameObject.SetActive(false);
    }

    public TextMeshProUGUI TurretTitleText;
    public RectTransform[] PenetrationGraphBoxes;
    public Button SwapAmmoButton;
    public FirepowerInfoGrid[] FirepowerBoxes;
    public Toggle AlternatignFireToggle;

    private ShipEditor.TurretInEditor _resultingTurret;
    private Action<ShipEditor.TurretInEditor> _callback;
    private string[] _allAmmoTypes;
}
