﻿using System.Collections;
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


    public Projectile ProjectileTemplate;
    public Projectile PlasmaProjectileTemplate;
    public ParticleSystem BigExplosion;
    public ParticleSystem SmallExplosion;
    public ParticleSystem FlakBurst;
    public ParticleSystem KineticImpactSparks;
    public ParticleSystem PlasmsExplosion;
    public ParticleSystem DamageElectricSparks;
    public Ship[] ShipPrototypes;
    public TurretBase[] TurretPrototypes;

    private Dictionary<string, Ship> _shipPrototypeDictionary = new Dictionary<string, Ship>();
    private Dictionary<string, TurretBase> _turretPrototypeDictionary = new Dictionary<string, TurretBase>();
    private Dictionary<ObjectFactory.WeaponEffect, ParticleSystem> _weaponEffectsDictionary = null;
}
