﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class ObjectPrototypes : MonoBehaviour
{
    void Awake()
    {
        ObjectFactory.SetPrototypes(this);
        _delayedActions = null;
    }

    public Projectile CreateProjectile()
    {
        Projectile res = Instantiate(ProjectileTemplate);
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

    public HarpaxBehavior CreateHarpaxProjectile()
    {
        HarpaxBehavior res = Instantiate(HarpaxTemplate);
        return res;
    }

    public CableBehavior CreateHarpaxCable()
    {
        return Instantiate<CableBehavior>(HarpaxCableTemplate);
    }

    public Torpedo CreateTorpedo()
    {
        Torpedo res = Instantiate(TorpedoTemplate);
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
        StrikeCraftWithFormationSize res = FindStrikeCraftPrototype(prodKey);
        if (res.CraftType != null)
            return Instantiate(res.CraftType);
        else
            return null;
    }

    private StrikeCraftWithFormationSize FindStrikeCraftPrototype(string prodKey)
    {
        StrikeCraftWithFormationSize res;
        if (_strikeCraftPrototypeDictionary.TryGetValue(prodKey, out res))
        {
            return res;
        }
        foreach (StrikeCraftWithFormationSize s in StrikeCraftPrototypes)
        {
            _strikeCraftPrototypeDictionary[s.CraftType.ProductionKey] = s;
        }
        if (_strikeCraftPrototypeDictionary.TryGetValue(prodKey, out res))
        {
            return res;
        }
        else
        {
            return new StrikeCraftWithFormationSize() { CraftType = null, FormationSize = 0 };
        }
    }

    public string[] GetAllStrikeCraftTypes()
    {
        if (_strikeCraftPrototypeDictionary.Count == 0)
        {
            foreach (StrikeCraftWithFormationSize s in StrikeCraftPrototypes)
            {
                _strikeCraftPrototypeDictionary[s.CraftType.ProductionKey] = s;
            }
        }
        return _strikeCraftPrototypeDictionary.Keys.ToArray();
    }

    public StrikeCraftFormation CreateStrikeCraftFormation(string strikeCraftKey)
    {
        StrikeCraftWithFormationSize proto = FindStrikeCraftPrototype(strikeCraftKey);
        if (proto.CraftType == null)
            return null;

        StrikeCraftFormation formation = Instantiate(StrikeCraftFormationPrototype);
        formation.CreatePositions(proto.FormationSize);
        return formation;
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
            for (int i = 0; i < Sprites.Length; i++)
            {
                _sprites[Sprites[i].Key] = Sprites[i].SpriteLink;
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

    public Material GetMaterial(string key)
    {
        Material res;
        if (!_materials.TryGetValue(key, out res))
        {
            for (int i = 0; i < Materials.Length; i++)
            {
                _materials[Materials[i].Key] = Materials[i].Mtl;
            }
            if (_materials.TryGetValue(key, out res))
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

    public NavigationGuide CreateNavGuide(Vector3 pos, Vector3 forward)
    {
        return Instantiate(NavGuide, pos, Quaternion.LookRotation(forward));
    }

    public ValueTuple<Canvas, BoardingProgressPanel> CreateBoardingProgressPanel()
    {
        Canvas boardibfCanvas = Instantiate(BoardingStatusCanvas);
        return new ValueTuple<Canvas, BoardingProgressPanel>(boardibfCanvas, boardibfCanvas.GetComponentInChildren<BoardingProgressPanel>());
    }

    public Canvas GetSelectionBoxCanvas()
    {
        return SelectionBoxCanvas;
    }

    public WeaponCtrlCfgLine CreateWeaponCtrlCfgLine()
    {
        return Instantiate(WeaponCtrlCfgLinePrototype);
    }

    public SelectedShipCard CreateShipCard()
    {
        return Instantiate(ShipCard);
    }

    public LineRenderer CreateSelectionRing()
    {
        return Instantiate(SelectionRing);
    }

    public void QueueDelayedAction(Action<float> a, float actionTime)
    {
        _delayedActionQueue.Add(a, actionTime);
        if (_delayedActions == null)
        {
            _delayedActions = StartCoroutine(DelayedActions());
        }
    }

    private IEnumerator DelayedActions()
    {
        WaitUntil waitEmptyQueue = new WaitUntil(() => _delayedActionQueue.Count > 0);
        while (true)
        {
            if (_delayedActionQueue.Count == 0)
            {
                yield return waitEmptyQueue;
            }
            (Action<float>, float) next = _delayedActionQueue.RemoveWithCost();
            float t = Time.time;
            yield return new WaitForSeconds(next.Item2 - t);
            next.Item1(t);
            yield return null;
        }
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
    public StrikeCraftWithFormationSize[] StrikeCraftPrototypes;
    public StrikeCraftFormation StrikeCraftFormationPrototype;
    public TurretBase[] TurretPrototypes;
    public StatusTopLevel StatusPanelPrototype;
    public WeaponCtrlCfgLine WeaponCtrlCfgLinePrototype;
    public BspPath[] Paths;

    public StatusSubsystem SubsystemStatusSprite;
    public StatusProgressBar SubsystemProgressRingTurretPrototype;

    public SprikeKeyValue[] Sprites;

    public MaterialKeyValue[] Materials;

    public NavigationGuide NavGuide;

    public Canvas BoardingStatusCanvas;

    public Canvas SelectionBoxCanvas;

    public Camera ShipStatusPanelCamera;

    public SelectedShipCard ShipCard;

    public LineRenderer SelectionRing;

    private Dictionary<string, Ship> _shipPrototypeDictionary = new Dictionary<string, Ship>();
    private Dictionary<string, StrikeCraftWithFormationSize> _strikeCraftPrototypeDictionary = new Dictionary<string, StrikeCraftWithFormationSize>();
    private Dictionary<string, TurretBase> _turretPrototypeDictionary = new Dictionary<string, TurretBase>();
    private Dictionary<ObjectFactory.WeaponEffect, ParticleSystem> _weaponEffectsDictionary = null;
    private Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
    private Dictionary<string, BspPath> _paths = new Dictionary<string, BspPath>();
    private Dictionary<string, Material> _materials = new Dictionary<string, Material>();

    private Coroutine _delayedActions;
    private StagPoint.Collections.BinaryMinHeap<Action<float>, float> _delayedActionQueue = new StagPoint.Collections.BinaryMinHeap<Action<float>, float>();

    [Serializable]
    public struct SprikeKeyValue
    {
        public string Key;
        public Sprite SpriteLink;
    }

    [Serializable]
    public struct MaterialKeyValue
    {
        public string Key;
        public Material Mtl;
    }

    [Serializable]
    public struct StrikeCraftWithFormationSize
    {
        public StrikeCraft CraftType;
        public int FormationSize;
    }

    private class DelayedActionComparer : IComparer<(float, Action)>
    {
        public int Compare((float, Action) x, (float, Action) y)
        {
            return x.Item1.CompareTo(y.Item1);
        }
    }
}