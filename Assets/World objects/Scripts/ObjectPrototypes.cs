using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ObjectPrototypes : MonoBehaviour
{
    void Awake()
    {
        ObjectFactory.SetPrototypes(this);
    }

    public Projectile CreateProjectile(Vector3 firingVector, float velocity, float range, Ship origShip)
    {
        Projectile res = Instantiate(ProjectileTemplate);
        Quaternion q = Quaternion.FromToRotation(res.transform.up, firingVector);
        res.transform.rotation = q;
        res.Speed = velocity;
        res.Range = range;
        res.OriginShip = origShip;
        return res;
    }

    public Projectile CreatePlasmaProjectile(Vector3 firingVector, float velocity, float range, Ship origShip)
    {
        Projectile res = Instantiate(PlasmaProjectileTemplate);
        Quaternion q = Quaternion.FromToRotation(res.transform.up, firingVector);
        res.transform.rotation = q;
        res.Speed = velocity;
        res.Range = range;
        res.OriginShip = origShip;
        return res;
    }

    public HarpaxBehavior CreateHarpaxProjectile(Vector3 firingVector, float velocity, float range, Ship origShip)
    {
        HarpaxBehavior res = Instantiate(HarpaxTemplate);
        Quaternion q = Quaternion.FromToRotation(res.transform.up, firingVector);
        res.transform.rotation = q;
        res.Speed = velocity;
        res.Range = range;
        res.OriginShip = origShip;
        return res;
    }

    public GameObject CreateHarpaxCableSeg()
    {
        return Instantiate<GameObject>(HarpaxCableSegTemplate);
    }

    public ParticleSystem CreateWeaponEffect(ObjectFactory.WeaponEffect e, Vector3 position)
    {
        if (_weaponEffectsDictionary == null)
        {
            _weaponEffectsDictionary = new Dictionary<ObjectFactory.WeaponEffect, ParticleSystem>()
            {
                { ObjectFactory.WeaponEffect.SmallExplosion, SmallExplosion },
                { ObjectFactory.WeaponEffect.BigExplosion, BigExplosion },
                { ObjectFactory.WeaponEffect.KineticImpactSparks, KineticImpactSparks },
                { ObjectFactory.WeaponEffect.FlakBurst, FlakBurst},
                { ObjectFactory.WeaponEffect.PlasmaExplosion, PlasmsExplosion },
                { ObjectFactory.WeaponEffect.DamageElectricSparks, DamageElectricSparks},
            };
        }
        ParticleSystem res = Instantiate(_weaponEffectsDictionary[e]);
        res.transform.position = position;
        ParticleSystem.MainModule m = res.main;
        m.playOnAwake = true;
        m.loop = false;
        
        return res;
    }

    public Ship CreateShip(string prodKey)
    {
        Ship res;
        if (_shipPrototypeDictionary.TryGetValue(prodKey, out res))
        {
            return Instantiate(res);
        }
        foreach (Ship s in ShipPrototypes)
        {
            _shipPrototypeDictionary[s.ProductionKey] = s;
        }
        if (_shipPrototypeDictionary.TryGetValue(prodKey, out res))
        {
            return Instantiate(res);
        }
        else
        {
            return null;
        }
    }

    public string[] GetAllShipTypes()
    {
        if (_shipPrototypeDictionary.Count == 0)
        {
            foreach (Ship s in ShipPrototypes)
            {
                _shipPrototypeDictionary[s.ProductionKey] = s;
            }
        }
        return _shipPrototypeDictionary.Keys.ToArray();
    }

    public TurretBase CreateTurret(string prodKey)
    {
        TurretBase res;
        if (_turretPrototypeDictionary.TryGetValue(prodKey, out res))
        {
            return Instantiate(res);
        }
        foreach (TurretBase t in TurretPrototypes)
        {
            string turretKey = t.TurretType.ToString() + t.TurretWeaponType.ToString();
            _turretPrototypeDictionary[turretKey] = t;
        }
        if (_turretPrototypeDictionary.TryGetValue(prodKey, out res))
        {
            return Instantiate(res);
        }
        else
        {
            return null;
        }
    }

    public string[] GetAllTurretTypes()
    {
        if (_turretPrototypeDictionary.Count == 0)
        {
            foreach (TurretBase t in TurretPrototypes)
            {
                string turretKey = t.TurretType.ToString() + t.TurretWeaponType.ToString();
                _turretPrototypeDictionary[turretKey] = t;
            }
        }
        return _turretPrototypeDictionary.Keys.ToArray();
    }

    public StatusTopLevel CreateStatusPanel(string prodKey)
    {
        StatusTopLevel res;
        if (_statusPanelsPrototypeDictionary.TryGetValue(prodKey, out res))
        {
            return Instantiate(res);
        }
        foreach (StatusTopLevel s in StatusPanelPrototypes)
        {
            _statusPanelsPrototypeDictionary[s.ShipProductionKey] = s;
        }
        if (_statusPanelsPrototypeDictionary.TryGetValue(prodKey, out res))
        {
            return Instantiate(res);
        }
        else
        {
            return null;
        }
    }

    public StatusSubsystem CreateStatusSprite()
    {
        StatusSubsystem res = Instantiate(SubsystemStatusSprite);
        return res;
    }

    public Sprite GetSprite(string key)
    {
        Sprite res;
        if(!_sprites.TryGetValue(key, out res))
        {
            for (int i = 0; i < SpriteKeys.Length; i++)
            {
                _sprites[SpriteKeys[i]] = Sprites[i];
            }
            if (_sprites.TryGetValue(key, out res))
            {
                return res;
            }
            else
            {
                return null;
            }
        }
        else
        {
            return res;
        }
    }

    public Tuple<Canvas, BoardingProgressPanel> CreateBoardingProgressPanel()
    {
        Canvas boardibfCanvas = Instantiate(BoardingStatusCanvas);
        return Tuple<Canvas, BoardingProgressPanel>.Create(boardibfCanvas, boardibfCanvas.GetComponentInChildren<BoardingProgressPanel>());
    }

    public WeaponCtrlCfgLine CreateWeaponCtrlCfgLine()
    {
        return Instantiate(WeaponCtrlCfgLinePrototype);
    }

    public Projectile ProjectileTemplate;
    public Projectile PlasmaProjectileTemplate;
    public HarpaxBehavior HarpaxTemplate;
    //public LineRenderer HarpaxCableTemplate;
    public GameObject HarpaxCableSegTemplate;
    public ParticleSystem BigExplosion;
    public ParticleSystem SmallExplosion;
    public ParticleSystem FlakBurst;
    public ParticleSystem KineticImpactSparks;
    public ParticleSystem PlasmsExplosion;
    public ParticleSystem DamageElectricSparks;
    public Ship[] ShipPrototypes;
    public TurretBase[] TurretPrototypes;
    public StatusTopLevel[] StatusPanelPrototypes;
    public WeaponCtrlCfgLine WeaponCtrlCfgLinePrototype;

    public StatusSubsystem SubsystemStatusSprite;

    public string[] SpriteKeys;
    public Sprite[] Sprites;

    public Canvas BoardingStatusCanvas;

    private Dictionary<string, Ship> _shipPrototypeDictionary = new Dictionary<string, Ship>();
    private Dictionary<string, TurretBase> _turretPrototypeDictionary = new Dictionary<string, TurretBase>();
    private Dictionary<string, StatusTopLevel> _statusPanelsPrototypeDictionary = new Dictionary<string, StatusTopLevel>();
    private Dictionary<ObjectFactory.WeaponEffect, ParticleSystem> _weaponEffectsDictionary = null;
    private Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
}
