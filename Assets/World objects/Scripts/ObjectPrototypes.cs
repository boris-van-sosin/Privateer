using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class ObjectPrototypes : MonoBehaviour
{
    void Awake()
    {
        ObjectFactory.SetPrototypes(this);
    }

    public Projectile CreateProjectile(Vector3 firingVector, float velocity, float range, ShipBase origShip)
    {
        Projectile res = Instantiate(ProjectileTemplate);
        Quaternion q = Quaternion.FromToRotation(res.transform.up, firingVector);
        res.transform.rotation = q;
        res.Speed = velocity;
        res.Range = range;
        res.OriginShip = origShip;
        return res;
    }

    public Projectile CreatePlasmaProjectile(Vector3 firingVector, float velocity, float range, ShipBase origShip)
    {
        Projectile res = Instantiate(PlasmaProjectileTemplate);
        Quaternion q = Quaternion.FromToRotation(res.transform.up, firingVector);
        res.transform.rotation = q;
        res.Speed = velocity;
        res.Range = range;
        res.OriginShip = origShip;
        return res;
    }

    public HarpaxBehavior CreateHarpaxProjectile(Vector3 firingVector, float velocity, float range, ShipBase origShip)
    {
        HarpaxBehavior res = Instantiate(HarpaxTemplate);
        Quaternion q = Quaternion.FromToRotation(res.transform.up, firingVector);
        res.transform.rotation = q;
        res.Speed = velocity;
        res.Range = range;
        res.OriginShip = origShip;
        return res;
    }

    public CableBehavior CreateHarpaxCable()
    {
        return Instantiate<CableBehavior>(HarpaxCableTemplate);
    }

    public Torpedo CreateTorpedo(Vector3 launchVector, Vector3 launchOrientation, Vector3 target, float range, ShipBase origShip)
    {
        Torpedo res = Instantiate(TorpedoTemplate);
        Quaternion q = Quaternion.FromToRotation(res.transform.up, launchOrientation);
        res.transform.rotation = q;
        res.OriginShip = origShip;
        res.Target = target;
        res.Range = range;
        res.ColdLaunchVec = launchVector;
        return res;
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

    public StrikeCraft CreateStrikeCraft(string prodKey)
    {
        StrikeCraft res;
        if (_strikeCraftPrototypeDictionary.TryGetValue(prodKey, out res))
        {
            return Instantiate(res);
        }
        foreach (StrikeCraft s in StrikeCraftPrototypes)
        {
            _strikeCraftPrototypeDictionary[s.ProductionKey] = s;
        }
        if (_strikeCraftPrototypeDictionary.TryGetValue(prodKey, out res))
        {
            return Instantiate(res);
        }
        else
        {
            return null;
        }
    }

    public string[] GetAllStrikeCraftTypes()
    {
        if (_strikeCraftPrototypeDictionary.Count == 0)
        {
            foreach (StrikeCraft s in StrikeCraftPrototypes)
            {
                _strikeCraftPrototypeDictionary[s.ProductionKey] = s;
            }
        }
        return _strikeCraftPrototypeDictionary.Keys.ToArray();
    }

    public StrikeCraftFormation CreateStrikeCraftFormation(string prodKey)
    {
        StrikeCraftFormation res;
        if (_strikeCraftFormationPrototypeDictionary.TryGetValue(prodKey, out res))
        {
            return Instantiate(res);
        }
        foreach (StrikeCraftFormation s in StrikeCraftFormationPrototypes)
        {
            _strikeCraftFormationPrototypeDictionary[s.ProductionKey] = s;
        }
        if (_strikeCraftFormationPrototypeDictionary.TryGetValue(prodKey, out res))
        {
            return Instantiate(res);
        }
        else
        {
            return null;
        }
    }

    public string[] GetAllStrikeCraftFormationTypes()
    {
        if (_strikeCraftFormationPrototypeDictionary.Count == 0)
        {
            foreach (StrikeCraftFormation s in StrikeCraftFormationPrototypes)
            {
                _strikeCraftFormationPrototypeDictionary[s.ProductionKey] = s;
            }
        }
        return _strikeCraftFormationPrototypeDictionary.Keys.ToArray();
    }

    public StatusTopLevel CreateStatusPanel()
    {
        StatusTopLevel res = Instantiate(StatusPanelPrototype);
        return res;
    }

    public StatusProgressBar CreateProgressBarSprite()
    {
        StatusProgressBar res = Instantiate(SubsystemProgressRingTurretPrototype);
        return res;
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

    public BspPath GetPath(string key)
    {
        if (!_paths.TryGetValue(key, out BspPath res))
        {
            for (int i = 0; i < Paths.Length; i++)
            {
                _paths[Paths[i].Key] = Paths[i];
            }
            if (_paths.TryGetValue(key, out res))
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
        return new Tuple<Canvas, BoardingProgressPanel>(boardibfCanvas, boardibfCanvas.GetComponentInChildren<BoardingProgressPanel>());
    }

    public WeaponCtrlCfgLine CreateWeaponCtrlCfgLine()
    {
        return Instantiate(WeaponCtrlCfgLinePrototype);
    }

    public Projectile ProjectileTemplate;
    public Projectile PlasmaProjectileTemplate;
    public HarpaxBehavior HarpaxTemplate;
    public CableBehavior HarpaxCableTemplate;
    public Torpedo TorpedoTemplate;
    public ParticleSystem BigExplosion;
    public ParticleSystem SmallExplosion;
    public ParticleSystem FlakBurst;
    public ParticleSystem KineticImpactSparks;
    public ParticleSystem PlasmsExplosion;
    public ParticleSystem DamageElectricSparks;
    public Ship[] ShipPrototypes;
    public StrikeCraft[] StrikeCraftPrototypes;
    public StrikeCraftFormation[] StrikeCraftFormationPrototypes;
    public TurretBase[] TurretPrototypes;
    public StatusTopLevel StatusPanelPrototype;
    public WeaponCtrlCfgLine WeaponCtrlCfgLinePrototype;
    public BspPath[] Paths;

    public StatusSubsystem SubsystemStatusSprite;
    public StatusProgressBar SubsystemProgressRingTurretPrototype;

    public string[] SpriteKeys;
    public Sprite[] Sprites;

    public Canvas BoardingStatusCanvas;

    public Camera ShipStatusPanelCamera;

    private Dictionary<string, Ship> _shipPrototypeDictionary = new Dictionary<string, Ship>();
    private Dictionary<string, StrikeCraft> _strikeCraftPrototypeDictionary = new Dictionary<string, StrikeCraft>();
    private Dictionary<string, StrikeCraftFormation> _strikeCraftFormationPrototypeDictionary = new Dictionary<string, StrikeCraftFormation>();
    private Dictionary<string, TurretBase> _turretPrototypeDictionary = new Dictionary<string, TurretBase>();
    private Dictionary<ObjectFactory.WeaponEffect, ParticleSystem> _weaponEffectsDictionary = null;
    private Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
    private Dictionary<string, BspPath> _paths = new Dictionary<string, BspPath>();
}
