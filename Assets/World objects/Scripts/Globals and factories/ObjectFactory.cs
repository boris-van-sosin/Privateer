using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;
using System;
using System.IO;

public static class ObjectFactory
{
    public static void SetPrototypes(ObjectPrototypes p)
    {
        if (_prototypes == null)
        {
            _prototypes = p;
        }
        if (_gunWarheads == null || _otherWarheads == null)
        {
            LoadWarheads();
        }
        if (_weaponMounts == null)
        {
            LoadWeaponMounts();
        }
        if (_weapons_projectile == null || _weapons_beam == null)
        {
            LoadWeapons();
        }
        if (_penetrationTable == null)
        {
            LoadPenetrationTable();
        }
        if (_cultureNamingLists == null)
        {
            LoadNamingLists();
        }
        if (_defaultPriorityLists == null)
        {
            LoadDefaultPriorityLists();
        }
    }

    public static Projectile CreateProjectile(Vector3 firingVector, float velocity, float range, float projectileScale, Warhead w, ShipBase origShip)
    {
        if (_prototypes != null)
        {
            Projectile p = _prototypes.CreateProjectile(firingVector, velocity, range, origShip);
            p.SetScale(projectileScale);
            p.ProjectileWarhead = w;
            return p;
        }
        else
        {
            return null;
        }
    }

    public static Projectile CreatePlasmaProjectile(Vector3 firingVector, float velocity, float range, Warhead w, ShipBase origShip)
    {
        if (_prototypes != null)
        {
            Projectile p = _prototypes.CreatePlasmaProjectile(firingVector, velocity, range, origShip);
            p.ProjectileWarhead = w;
            return p;
        }
        else
        {
            return null;
        }
    }

    public static (string, string) GetEffectKey(string weaponType, string weaponSize, string ammo)
    {
        WarheadDataEntry3 w3 = _gunWarheads[(weaponType, weaponSize, ammo)];
        return (w3.EffectAssetBundle, w3.EffectAsset);
    }

    public static (string, string) GetEffectKey(string weaponType, string weaponSize)
    {
        WarheadDataEntry2 w2 = _otherWarheads[(weaponType, weaponSize)];
        return (w2.EffectAssetBundle, w2.EffectAsset);
    }


    public static (string, string) GetEffectKey(string torpType)
    {
        WarheadDataEntry4 w4 = _torpedoWarheads[torpType];
        return (w4.EffectAssetBundle, w4.EffectAsset);
    }


    public static HarpaxBehavior CreateHarpaxProjectile(Vector3 firingVector, float velocity, float range, ShipBase origShip)
    {
        if (_prototypes != null)
        {
            HarpaxBehavior p = _prototypes.CreateHarpaxProjectile(firingVector, velocity, range, origShip);
            return p;
        }
        else
        {
            return null;
        }
    }

    public static CableBehavior CreateHarpaxTowCable(Rigidbody obj1, Rigidbody obj2)
    {
        CableBehavior res = _prototypes.CreateHarpaxCable();
        res.Connect(obj1, obj2);
        return res;
    }

    public static CableBehavior CreateHarpaxTowCable(Rigidbody obj1, Rigidbody obj2, Vector3 targetConnectionPoint)
    {
        CableBehavior res = _prototypes.CreateHarpaxCable();
        res.Connect(obj1, obj2, targetConnectionPoint);
        return res;
    }

    public static Torpedo CreateTorpedo(Vector3 launchVector, Vector3 launchOrientation, Vector3 target, float range, Warhead w, ShipBase origShip)
    {
        if (_prototypes != null)
        {
            Torpedo t = _prototypes.CreateTorpedo(launchVector, launchOrientation, target, range, origShip);
            t.ProjectileWarhead = w;
            return t;
        }
        else
        {
            return null;
        }
    }

    public static Torpedo CreateTorpedo(Vector3 launchVector, Vector3 launchOrientation, Vector3 target, string torpType, ShipBase origShip)
    {
        if (_prototypes != null)
        {
            WarheadDataEntry4 torpData = _torpedoWarheads[torpType];
            Torpedo t = CreateTorpedo(launchVector, launchOrientation, target, torpData.MaxRange, CreateWarhead(torpType), origShip);
            t.transform.localScale = Vector3.one * torpData.ProjectileScale;
            return t;
        }
        else
        {
            return null;
        }
    }

    public static ParticleSystem CreateWeaponEffect(WeaponEffect e, Vector3 position)
    {
        if (e == WeaponEffect.None)
        {
            return null;
        }
        if (_prototypes != null)
        {
            return _prototypes.CreateWeaponEffect(e, position);
        }
        else
        {
            return null;
        }
    }

    public static ParticleSystem AcquireParticleSystem(string assetBundleSource, string asset, Vector3 position)
    {
        if (_prototypes != null)
        {
            ObjectCache.SpecificCache<ParticleSystem> currCache;
            if (!_objCache.ParticleSystems.TryGetValue((assetBundleSource, asset), out currCache))
            {
                _objCache.ParticleSystems.Add((assetBundleSource, asset), currCache = new ObjectCache.SpecificCache<ParticleSystem>());
            }
            if (currCache.Count > 0)
            {
                return currCache.Acquire();
            }
            else
            {
                GameObject resObj = _prototypes.CreateObjectByPath(assetBundleSource, asset, "");
                ParticleSystem res = resObj.GetComponent<ParticleSystem>();
                if (res != null)
                {
                    res.transform.position = position;
                    return res;
                }
                else
                {
                    return null;
                }
            }
        }
        else
        {
            return null;
        }
    }

    public static void ReleaseParticleSystem(string assetBundleSource, string asset, ParticleSystem ps)
    {
        ObjectCache.SpecificCache<ParticleSystem> currCache;
        if (!_objCache.ParticleSystems.TryGetValue((assetBundleSource, asset), out currCache))
        {
            _objCache.ParticleSystems.Add((assetBundleSource, asset), currCache = new ObjectCache.SpecificCache<ParticleSystem>());
        }
        currCache.Release(ps);
    }

    public static T GetRandom<T>(IEnumerable<T> lst)
    {
        int numElems = lst.Count();
        if (numElems == 0)
        {
            return lst.ElementAt(10000);
        }
        return lst.ElementAt(UnityEngine.Random.Range(0, numElems));
    }

    public static Warhead CreateWarhead(string weaponType, string size, string ammo)
    {
        return _gunWarheads[(weaponType, size, ammo)].WarheadData;
    }

    public static Warhead CreateWarhead(string weaponType, string size)
    {
        return _otherWarheads[(weaponType, size)].WarheadData;
    }

    public static Warhead CreateWarhead(string torpType)
    {
        return _torpedoWarheads[torpType].WarheadData;
    }

    public static ValueTuple<int, float> TorpedoLaunchDataFromTorpedoType(string torpType)
    {
        WarheadDataEntry4 tropData = _torpedoWarheads[torpType];
        return new ValueTuple<int, float>(tropData.SpeardSize, tropData.MaxRange);
    }

    public static IEnumerable<string> GetAllTurretMounts()
    {
        return _weaponMounts.Keys;
    }

    public static IEnumerable<(string, string, string)> GetAllProjectileWarheads()
    {
        return _gunWarheads.Keys;
    }

    public static IEnumerable<string> GetAllTorpedoWarheads()
    {
        return _torpedoWarheads.Keys;
    }

    public static IEnumerable<(string, string)> GetAllOtherWarheads()
    {
        return _otherWarheads.Keys;
    }

    public static string[] GetAllShipTypes()
    {
        return _prototypes.GetAllShipTypes();
    }

    public static Ship CreateShip(string prodKey)
    {
        return CreateShip2(prodKey);
        //return _prototypes.CreateShip(prodKey);
    }

    public static Ship CreateShip2(string prodKey)
    {
        if (_shipHullDefinitions == null)
        {
            LoadShipHullDefinitions();
        }
        ShipHullDefinition hullDef;
        if (!_shipHullDefinitions.TryGetValue(prodKey, out hullDef))
        {
            return null;
        }

        HierarchyNode hullRoot = hullDef.Geometry;
        HierarchyNode damageSmoke = hullDef.DamageSmoke;
        HierarchyNode engineExhaustIdle = hullDef.EngineExhaustIdle;
        HierarchyNode engineExhaustOn = hullDef.EngineExhaustOn;
        HierarchyNode engineExhaustBrake = hullDef.EngineExhaustBrake;
        HierarchyNode magneticField = hullDef.MagneticField;
        HierarchyNode[] teamColor = hullDef.TeamColorComponents;
        HierarchyNode shield = hullDef.Shield;
        HierarchyNode statusRing = hullDef.StatusRing;

        GameObject hullObj = HierarchyConstructionUtil.ConstructHierarchy(hullRoot, _prototypes, ShipsLayer, ShipsLayer, EffectsLayer);
        GameObject[] particleSysObjs = new GameObject[]
        {
            HierarchyConstructionUtil.ConstructHierarchy(damageSmoke, _prototypes, ShipsLayer, ShipsLayer, EffectsLayer),
            HierarchyConstructionUtil.ConstructHierarchy(engineExhaustIdle, _prototypes, ShipsLayer, ShipsLayer, EffectsLayer),
            HierarchyConstructionUtil.ConstructHierarchy(engineExhaustOn, _prototypes, ShipsLayer, ShipsLayer, EffectsLayer),
            HierarchyConstructionUtil.ConstructHierarchy(engineExhaustBrake, _prototypes, ShipsLayer, ShipsLayer, EffectsLayer),
            HierarchyConstructionUtil.ConstructHierarchy(magneticField, _prototypes, ShipsLayer, ShipsLayer, EffectsLayer),
        };
        GameObject[] teamColorObjs = teamColor.Select(r => HierarchyConstructionUtil.ConstructHierarchy(r, _prototypes, ShipsLayer, ShipsLayer, EffectsLayer)).ToArray();

        GameObject resObj = _prototypes.CreateObjectEmpty();
        resObj.layer = ShipsLayer;
        resObj.name = hullDef.HullName + " ship";
        hullObj.transform.parent = resObj.transform;
        hullRoot.ApplyToTransform(hullObj.transform);

        foreach (GameObject o in teamColorObjs.Union(particleSysObjs))
        {
            Vector3 pos = o.transform.position;
            Quaternion rot = o.transform.rotation;
            Vector3 scale = o.transform.localScale;

            o.transform.parent = resObj.transform;
            o.transform.position = pos;
            o.transform.rotation = rot;
            o.transform.localScale = scale;
        }

        GameObject shieldObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        shieldObj.layer = ShieldsLayer;
        shieldObj.GetComponent<MeshRenderer>().sharedMaterial = _prototypes.GetMaterial("ShieldMtl");
        shieldObj.transform.parent = resObj.transform;
        shield.ApplyToTransform(shieldObj.transform);

        LineRenderer lr = _prototypes.CreateSelectionRing();
        lr.transform.parent = resObj.transform;
        statusRing.ApplyToTransform(lr.transform);

        foreach (WeaponHardpointDefinition hardpoint in hullDef.WeaponHardpoints)
        {
            GameObject hardPointObj = _prototypes.CreateObjectEmpty();
            hardPointObj.layer = ShipsLayer;
            hardPointObj.transform.parent = resObj.transform;
            hardpoint.HardpointNode.ApplyToTransform(hardPointObj.transform);
            if (hardpoint.TurretHardpoint != null)
            {
                TurretHardpoint turretHardpoint = hardPointObj.AddComponent<TurretHardpoint>();
                hardpoint.TurretHardpoint.SetHardpointFields(turretHardpoint);
            }
            else if (hardpoint.TorpedoHardpoint != null)
            {
                TorpedoHardpoint torpHardpoint = hardPointObj.AddComponent<TorpedoHardpoint>();
                hardpoint.TorpedoHardpoint.SetHardpointFields(torpHardpoint);
            }
        }

        GameObject navBoxObj = _prototypes.CreateObjectEmpty();
        navBoxObj.name = "NavBox";
        navBoxObj.layer = NavCollidersLayer;
        navBoxObj.transform.parent = resObj.transform;
        navBoxObj.transform.localPosition = Vector3.zero;
        navBoxObj.transform.localRotation = Quaternion.identity;
        navBoxObj.transform.localScale = Vector3.one;

        GameObject meshSrc = _prototypes.GetObjectByPath(hullDef.CollisionMesh.AssetBundlePath, hullDef.CollisionMesh.AssetPath, hullDef.CollisionMesh.MeshPath);
        if (null != meshSrc)
        {
            MeshFilter mesh = meshSrc.GetComponent<MeshFilter>();
            MeshCollider meshCollider = resObj.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh.sharedMesh;
            meshCollider.convex = true;
        }

        Rigidbody rb = resObj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.angularDrag = 0.025f;
        rb.drag = 0.025f;
        rb.mass = hullDef.Mass;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

        Ship s = resObj.AddComponent<Ship>();
        s.HullObject = hullObj.transform;
        s.TeamColorComponents = teamColorObjs.Select(c => c.GetComponent<MeshRenderer>()).Where(mr => mr != null).ToArray();
        s.ProductionKey = prodKey;
        hullDef.MovementData.FillShipData(s);
        hullDef.HullProtection.FillShipData(s);
        hullDef.ComponentSlots.FillShipData(s);
        s.Mass = hullDef.Mass;
        s.MaxCrew = hullDef.MaxCrew;
        s.OperationalCrew = hullDef.OperationalCrew;
        s.SkeletonCrew = hullDef.SkeletonCrew;
        s.MaxSpecialCharacters = hullDef.MaxSpecialCharacters;
        hullDef.MovementData.FillShipData(s);
        Enum.TryParse(hullDef.ShipSize, out s.ShipSize);
        s.PostAwake();

        return s;
    }

    public static Ship GetShipTemplate(string prodKey)
    {
        foreach (Ship s in _prototypes.ShipPrototypes)
        {
            if (s.ProductionKey == prodKey)
            {
                return s;
            }
        }
        return null;
    }

    public static TurretBase CreateTurret(string turretMountType, string weaponNum, string weaponSize, string weaponType)
    {
        if (_turretDefinitions == null)
        {
            LoadTurretDefinitions();
        }
        TurretDefinition turretDef;
        if (!_turretDefinitions.TryGetValue((turretMountType, weaponNum, weaponSize, weaponType), out turretDef))
        {
            return null;
        }

        HierarchyNode root = turretDef.Geometry;
        GameObject resObj = HierarchyConstructionUtil.ConstructHierarchy(root, _prototypes, WeaponsLayer, WeaponsLayer, EffectsLayer);
        TurretBase resTurret;
        switch (turretDef.BehaviorType)
        {
            case WeaponBehaviorType.Gun:
                resTurret = SetGunTurretData(turretDef, resObj);
                break;
            case WeaponBehaviorType.Beam:
                resTurret = SetBeamTurretData(turretDef, resObj);
                break;
            case WeaponBehaviorType.ContinuousBeam:
                resTurret = SetContinuousBeamTurretData(turretDef, resObj);
                break;
            case WeaponBehaviorType.Special:
                resTurret = SetSpecialProjectileTurretData(turretDef, resObj);
                break;
            case WeaponBehaviorType.Torpedo:
                resTurret = SetTorpedoTurretData(turretDef, resObj);
                break;
            case WeaponBehaviorType.BomberTorpedo:
                resTurret = SetBomberTorpedoTurretData(turretDef, resObj);
                break;
            default:
                resTurret = null;
                break;
        }

        TurretMountDataEntry md = _weaponMounts[turretDef.TurretType];
        resTurret.MaxHitpoints = md.HitPoints;
        resTurret.ComponentHitPoints = md.HitPoints;
        resTurret.RotationSpeed = md.RotationSpeed;
        resTurret.ComponentHitPoints = resTurret.ComponentMaxHitPoints;
        resTurret.Init(turretMountType + weaponNum + weaponSize);

        return resTurret;
    }

    private static GunTurret SetGunTurretData(TurretDefinition turretDef, GameObject turretObj)
    {
        GunTurret resTurret = turretObj.AddComponent<GunTurret>();
        resTurret.TurretAxis = turretDef.TurretAxis;
        resTurret.TurretType = turretDef.TurretType;
        resTurret.TurretWeaponSize = turretDef.WeaponSize;
        resTurret.TurretWeaponType = turretDef.WeaponType;

        WeaponProjectileDataEntry wpd = _weapons_projectile[(turretDef.WeaponSize, turretDef.WeaponType)];
        resTurret.ProjectileScale = wpd.ProjectileScale;
        resTurret.MaxRange = wpd.MaxRange;
        resTurret.MuzzleVelocity = wpd.MuzzleVelocity;
        resTurret.FiringInterval = wpd.FiringInterval;
        resTurret.Inaccuracy = wpd.MaxSpread;
        resTurret.EnergyToFire = wpd.EnergyToFire;
        resTurret.HeatToFire = wpd.HeatToFire;
        resTurret.DefaultAlternatingFire = (turretDef.WeaponType == "Autocannon");
        return resTurret;
    }

    private static BeamTurret SetBeamTurretData(TurretDefinition turretDef, GameObject turretObj)
    {
        BeamTurret resTurret = turretObj.AddComponent<BeamTurret>();
        resTurret.TurretAxis = turretDef.TurretAxis;
        resTurret.TurretType = turretDef.TurretType;
        resTurret.TurretWeaponSize = turretDef.WeaponSize;
        resTurret.TurretWeaponType = turretDef.WeaponType;

        WeaponBeamDataEntry wbd = _weapons_beam[(turretDef.WeaponSize, turretDef.WeaponType)];
        resTurret.MaxRange = wbd.MaxRange;
        resTurret.FiringInterval = wbd.FiringInterval;
        resTurret.BeamDuration = wbd.BeamDuration;
        resTurret.Inaccuracy = 0f;
        resTurret.EnergyToFire = wbd.EnergyToFire;
        resTurret.HeatToFire = wbd.HeatToFire;

        return resTurret;
    }

    private static ContinuousBeamTurret SetContinuousBeamTurretData(TurretDefinition turretDef, GameObject turretObj)
    {
        ContinuousBeamTurret resTurret = turretObj.AddComponent<ContinuousBeamTurret>();
        resTurret.TurretAxis = turretDef.TurretAxis;
        resTurret.TurretType = turretDef.TurretType;
        resTurret.TurretWeaponSize = turretDef.WeaponSize;
        resTurret.TurretWeaponType = turretDef.WeaponType;

        WeaponBeamDataEntry wbd = _weapons_beam[(turretDef.WeaponSize, turretDef.WeaponType)];
        resTurret.MaxRange = wbd.MaxRange;
        resTurret.FiringInterval = wbd.FiringInterval;
        resTurret.BeamDuration = wbd.BeamDuration;
        resTurret.Inaccuracy = 0f;
        resTurret.EnergyToFire = wbd.EnergyToFire;
        resTurret.HeatToFire = wbd.HeatToFire;

        return resTurret;
    }

    private static SpecialProjectileTurret SetSpecialProjectileTurretData(TurretDefinition turretDef, GameObject turretObj)
    {
        SpecialProjectileTurret resTurret = turretObj.AddComponent<SpecialProjectileTurret>();
        resTurret.TurretAxis = turretDef.TurretAxis;
        resTurret.TurretType = turretDef.TurretType;
        resTurret.TurretWeaponSize = turretDef.WeaponSize;
        resTurret.TurretWeaponType = turretDef.WeaponType;

        WeaponProjectileDataEntry wpd = _weapons_projectile[(turretDef.WeaponSize, turretDef.WeaponType)];
        resTurret.MaxRange = wpd.MaxRange;
        resTurret.MuzzleVelocity = wpd.MuzzleVelocity;
        resTurret.FiringInterval = wpd.FiringInterval;
        resTurret.Inaccuracy = wpd.MaxSpread;
        resTurret.EnergyToFire = wpd.EnergyToFire;
        resTurret.HeatToFire = wpd.HeatToFire;

        return resTurret;
    }

    private static TorpedoTurret SetTorpedoTurretData(TurretDefinition turretDef, GameObject turretObj)
    {
        TorpedoTurret resTurret = turretObj.AddComponent<TorpedoTurret>();
        resTurret.TurretAxis = turretDef.TurretAxis;
        resTurret.TurretType = turretDef.TurretType;
        resTurret.TurretWeaponSize = turretDef.WeaponSize;
        resTurret.TurretWeaponType = turretDef.WeaponType;

        WeaponTorpedoDataEntry tordpedoData = _weapons_torpedo;
        resTurret.FiringInterval = tordpedoData.FiringInterval;
        resTurret.EnergyToFire = tordpedoData.EnergyToFire;
        resTurret.HeatToFire = tordpedoData.HeatToFire;

        return resTurret;
    }

    private static BomberTorpedoLauncher SetBomberTorpedoTurretData(TurretDefinition turretDef, GameObject turretObj)
    {
        BomberTorpedoLauncher resTurret = turretObj.AddComponent<BomberTorpedoLauncher>();
        resTurret.TurretAxis = turretDef.TurretAxis;
        resTurret.TurretType = turretDef.TurretType;
        resTurret.TurretWeaponSize = turretDef.WeaponSize;
        resTurret.TurretWeaponType = turretDef.WeaponType;

        WeaponTorpedoDataEntry tordpedoData = _weapons_torpedo;
        resTurret.FiringInterval = tordpedoData.FiringInterval;
        resTurret.EnergyToFire = tordpedoData.EnergyToFire;
        resTurret.HeatToFire = tordpedoData.HeatToFire;

        return resTurret;
    }

    public static TurretBase CreateStrikeCraftTurret(string turretType, string turretSize, string weaponType)
    {
#if DEBUG
        if (turretSize != "StrikeCraft")
        {
            Debug.LogError("Tried to create a non-strike-craft weapon on a strike craft");
        }
#endif
        TurretBase t = CreateTurret(turretType, "", turretSize, weaponType);
        if (t is GunTurret)
        {
            GunTurret gt = t as GunTurret;
            gt.AmmoType = "KineticPenetrator";
            gt.DefaultAlternatingFire = weaponType.Contains("Auto");
        }
        else if (t is BomberTorpedoLauncher)
        {
            WeaponTorpedoDataEntry tordpedoData = _weapons_torpedo;
            BomberTorpedoLauncher tt = t as BomberTorpedoLauncher;
            tt.FiringInterval = tordpedoData.FiringInterval;
        }

        t.EnergyToFire = 0;
        t.HeatToFire = 0;
        return t;
    }

    private static void LoadTurretDefinitions()
    {
        _turretDefinitions = new Dictionary<(string, string, string, string), TurretDefinition>();
        string searchPath = Path.Combine("TextData", "Turrets");
        foreach (string turretFile in Directory.EnumerateFiles(searchPath, "*.yml", SearchOption.TopDirectoryOnly))
        {
            using (StreamReader sr = new StreamReader(turretFile, Encoding.UTF8))
            {
                TurretDefinition turretDef = HierarchySerializer.LoadHierarchy<TurretDefinition>(sr);
                _turretDefinitions[(turretDef.TurretType, turretDef.WeaponNum, turretDef.WeaponSize, turretDef.WeaponType)] = turretDef;
            }
        }
    }

    private static void LoadShipHullDefinitions()
    {
        _shipHullDefinitions = new Dictionary<string, ShipHullDefinition>();
        string searchPath = Path.Combine("TextData", "ShipHulls");
        foreach (string shipFile in Directory.EnumerateFiles(searchPath, "*.yml", SearchOption.TopDirectoryOnly))
        {
            using (StreamReader sr = new StreamReader(shipFile, Encoding.UTF8))
            {
                ShipHullDefinition shipHullDef = HierarchySerializer.LoadHierarchy<ShipHullDefinition>(sr);
                _shipHullDefinitions[shipHullDef.HullName] = shipHullDef;
            }
        }
    }

    public static string[] GetAllStrikeCraftTypes()
    {
        return _prototypes.GetAllStrikeCraftTypes();
    }

    public static StrikeCraft CreateStrikeCraft(string prodKey)
    {
        return _prototypes.CreateStrikeCraft(prodKey);
    }

    public static StrikeCraft CreateStrikeCraftAndFitOut(string prodKey)
    {
        StrikeCraft s = CreateStrikeCraft(prodKey);
        TurretHardpoint[] allHardpoints = s.GetComponentsInChildren<TurretHardpoint>();
        foreach (TurretHardpoint hp in allHardpoints)
        {
            if (hp.AllowedWeaponTypes.Length == 0)
            {
                continue;
            }
            TurretBase t = null;
            if (hp.AllowedWeaponTypes.Contains("FighterCannonStrikeCraft"))
            {
                t = CreateStrikeCraftTurret("FighterCannon", "StrikeCraft", "FighterCannon");
                s.PlaceTurret(hp, t);
            }
            else if (hp.AllowedWeaponTypes.Contains("FighterAutocannonStrikeCraft"))
            {
                t = CreateStrikeCraftTurret("FighterAutocannon", "StrikeCraft", "FighterAutocannon");
                s.PlaceTurret(hp, t);
            }
            else if (hp.AllowedWeaponTypes.Contains("BomberAutocannonStrikeCraft"))
            {
                t = CreateStrikeCraftTurret("BomberAutocannon", "StrikeCraft", "FighterAutocannon");
                s.PlaceTurret(hp, t);
            }
            else if (hp.AllowedWeaponTypes.Contains("BomberTorpedoStrikeCraft"))
            {
                t = CreateStrikeCraftTurret("BomberTorpedo", "StrikeCraft", "TorpedoWeapon");
                s.PlaceTurret(hp, t);
            }

            if (t != null && t is BomberTorpedoLauncher)
            {
                BomberTorpedoLauncher tl = (BomberTorpedoLauncher)t;
                tl.DummyTorpedoString = s.DummyTorpedoString;
                tl.LoadedTorpedoType = "Heavy";
            }
        }
        s.gameObject.AddComponent<StrikeCraftAIController>();
        return s;
    }

    public static StrikeCraftFormation CreateStrikeCraftFormation(string prodKey)
    {
        StrikeCraftFormation res = _prototypes.CreateStrikeCraftFormation(prodKey);
        res.SetFormationType(FormationBase.FormationType.Vee);
        res.gameObject.AddComponent<StrikeCraftFormationAIController>();
        return res;
    }

    public static Sprite GetSprite(string key)
    {
        return _prototypes.GetSprite(key);
    }

    public static Material GetMaterial(string key)
    {
        return _prototypes.GetMaterial(key);
    }

    public static NavigationGuide CreateNavGuide(Vector3 pos, Vector3 forward)
    {
        return _prototypes.CreateNavGuide(pos, forward);
    }

    public static StatusSubsystem CreateStatusSubsytem(IShipActiveComponent comp)
    {
        StatusSubsystem res = _prototypes.CreateStatusSprite();
        res.SetImage(_prototypes.GetSprite(comp.SpriteKey));
        res.AttachComponent(comp);
        return res;
    }

    public static StatusProgressBar CreateSubsytemProgressRing(TurretBase t)
    {
        StatusProgressBar res = _prototypes.CreateProgressBarSprite();
        res.AttachFunction(() => (Time.time - t.LastFire) / t.ActualFiringInterval);
        return res;
    }

    public static StatusTopLevel CreateStatusPanel()
    {
        StatusTopLevel res = _prototypes.CreateStatusPanel();
        return res;
    }

    public static CultureNames GetCultureNames(string culture)
    {
        return _cultureNamingLists[culture];
    }

    public static NamingSystem.ShipType InternalShipTypeToNameType(ShipSize sz)
    {
        switch (sz)
        {
            case ShipSize.Sloop:
                return NamingSystem.ShipType.Sloop;
            case ShipSize.Frigate:
                return NamingSystem.ShipType.Frigate;
            case ShipSize.Destroyer:
                return NamingSystem.ShipType.Destroyer;
            case ShipSize.Cruiser:
                return NamingSystem.ShipType.Cruiser;
            case ShipSize.CapitalShip:
                return NamingSystem.ShipType.CapitalShip;
            default:
                return NamingSystem.ShipType.Any;
        }
    }

    public static StatusTopLevel CreateStatusPanel(Ship s, Transform containingPanel)
    {
        StatusTopLevel res = CreateStatusPanel();
        RectTransform rt = res.GetComponent<RectTransform>();
        rt.SetParent(containingPanel);
        rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
        rt.offsetMax = new Vector2(0f, rt.offsetMax.y);
        res.AttachShip(s);
        return res;
    }

    public static ValueTuple<Canvas, BoardingProgressPanel> CreateBoardingProgressPanel()
    {
        return _prototypes.CreateBoardingProgressPanel();
    }

    public static Canvas GetSelectionBoxCanvas()
    {
        return _prototypes.GetSelectionBoxCanvas();
    }

    public static WeaponCtrlCfgLine CreateWeaponCtrlCfgLine(string label)
    {
        WeaponCtrlCfgLine res = _prototypes.CreateWeaponCtrlCfgLine();
        res.WeaponTextBox.text = label;
        return res;
    }

    public static ArmourPenetrationTable GetArmourPenetrationTable()
    {
        return _penetrationTable;
    }

    public static Camera GetShipStatusPanelCamera()
    {
        return _prototypes.ShipStatusPanelCamera;
    }

    public static BspPath GetPath(string key)
    {
        return _prototypes.GetPath(key);
    }

    public static List<TacMapEntityType> GetDefaultTargetPriorityList(string weaponType, string sz)
    {
        List<TacMapEntityType> res;
        if (_defaultPriorityLists.TryGetValue((weaponType, sz), out res))
        {
            return new List<TacMapEntityType>(res);
        }
        return null;
    }

    public static SelectedShipCard AcquireShipCard(Ship s)
    {
        SelectedShipCard res;
        if (_objCache.ShipCards.Count > 0)
        {
            res = _objCache.ShipCards.Acquire();
        }
        else
        {
            res = _prototypes.CreateShipCard();
            RectTransform cardRT = res.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0f, 1f);
            cardRT.anchorMax = new Vector2(0f, 1f);
        }
        res.AttachShip(s);
        res.gameObject.SetActive(true);
        return res;
    }
    public static void ReleaseShipCard(SelectedShipCard c)
    {
        _objCache.ShipCards.Release(c);
    }
    private static ObjectCache _objCache = new ObjectCache();

    public static Sprite GetShipPhoto(Ship s)
    {
        Sprite res;
        if (_shipPhotos.TryGetValue(s, out res))
        {
            return res;
        }
        else
        {
            res  = ShipPhotoUtil.TakePhoto(s, 512, 512);
            _shipPhotos[s] = res;
            return res;
        }
    }
    private static Dictionary<Ship, Sprite> _shipPhotos = new Dictionary<Ship, Sprite>();

    public static int DefaultLayer => _defaultLayer;
    public static int ShipsLayer => _shipsLayer;
    public static int ShieldsLayer => _shieldsLayer;
    public static int NavCollidersLayer => _navCollidersLayer;
    public static int WeaponsLayer => _weaponsLayer;
    public static int EffectsLayer => _effectsLayer;
    public static int AllTargetableLayerMask => _allTargetableLayerMask;
    public static int AllShipsLayerMask => _allShipsLayerMask;
    public static int AllStikeCraftLayerMask => _allStrikeCraftLayerMask;
    public static int AllShipsNoStikeCraftLayerMask => _allShipsNoStikeCraftLayerMask;
    public static int AllShipsNoShieldsLayerMask => _allShipsNoShieldsLayerMask;
    public static int AllShipsNoShieldsNoStikeCraftLayerMask => _allShipsNoStrikeCraftNoShieldsLayerMask;
    public static int NavBoxesLayerMask => _navBoxesLayerMask;
    public static int NavBoxesStrikeCraftLayerMask => _navBoxesStrikeCraftLayerMask;
    public static int NavBoxesAllLayerMask => _navBoxesAllLayerMask;

    private static void LoadWarheads()
    {
        string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine("TextData", "Warheads.csv"));
        _gunWarheads = new Dictionary<(string, string, string), WarheadDataEntry3>();
        _torpedoWarheads = new Dictionary<string, WarheadDataEntry4>();
        _otherWarheads = new Dictionary<(string, string), WarheadDataEntry2>();

        foreach (string l in lines)
        {
            if (l.Trim().StartsWith("3"))
            {
                WarheadDataEntry3 d3 = WarheadDataEntry3.FromString(l);
                _gunWarheads.Add((d3.LaunchWeaponType, d3.LaunchWeaponSize, d3.Ammo), d3);
            }
            else if (l.Trim().StartsWith("2"))
            {
                WarheadDataEntry2 d2 = WarheadDataEntry2.FromString(l);
                _otherWarheads.Add((d2.LaunchWeaponType, d2.LaunchWeaponSize), d2);
            }
            else if (l.Trim().StartsWith("4"))
            {
                WarheadDataEntry4 d4 = WarheadDataEntry4.FromString(l);
                _torpedoWarheads.Add(d4.LaunchTorpedoType, d4);
            }
        }
    }

    private static void LoadDefaultPriorityLists()
    {
        _defaultPriorityLists = new Dictionary<(string, string), List<TacMapEntityType>>();
        string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine("TextData", "DefaultPriorities.csv"));
        int idx = 0;
        while (!lines[idx].Trim().StartsWith("x"))
        {
            ++idx;
        }
        string[] typesOrder = lines[idx].Split(',').Skip(1).ToArray();
        foreach (string line in lines.Skip(idx + 1))
        {
            int cutIdx = line.IndexOf('#');
            string cleanLine = cutIdx >= 0 ? line.Substring(0, cutIdx) : line;
            string[] items = cleanLine.Split(',');
            string sz = items[0];
            for (int i = 0; i < items.Length - 1; ++i)
            {
                if (items[i + 1] != string.Empty)
                {
                    _defaultPriorityLists.Add(
                        (typesOrder[i], sz),
                        items[i + 1].Split(':').Select(s => (TacMapEntityType)Enum.Parse(typeof(TacMapEntityType), s)).ToList());
                }
            }
        }
    }

    private static void LoadWeaponMounts()
    {
        string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine("TextData", "WeaponMounts.txt"));
        _weaponMounts = new Dictionary<string, TurretMountDataEntry>();
        foreach (string l in lines)
        {
            if (l.Trim().StartsWith("WeaponMount"))
            {
                TurretMountDataEntry tm = TurretMountDataEntry.FromString(l);
                _weaponMounts.Add(tm.MountSize + tm.Mount, tm);
            }
        }
    }

    private static void LoadPenetrationTable()
    {
        string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine("TextData", "PenetrationChart.txt"));
        List<string[]> cleanLines = new List<string[]>(lines.Length);
        foreach (string l in lines)
        {
            string trimmed = l.Trim();
            int commentIdx = trimmed.IndexOf('#');
            string clean;
            if (commentIdx >= 0)
            {
                clean = trimmed.Substring(0, commentIdx);
            }
            else
            {
                clean = trimmed;
            }
            string[] currArr = clean.Trim().Split(',').Select(x => x.Trim()).Where(y => y != string.Empty).ToArray();
            if (currArr.Length > 0)
            {
                cleanLines.Add(currArr);
            }
        }
        _penetrationTable = new ArmourPenetrationTable(cleanLines.ToArray());
    }

    private static void LoadWeapons()
    {
        string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine("TextData", "Weapons.txt"));
        _weapons_projectile = new Dictionary<(string, string), WeaponProjectileDataEntry>();
        _weapons_beam = new Dictionary<(string, string), WeaponBeamDataEntry>();
        foreach (string l in lines)
        {
            if (l.Trim().StartsWith("ProjectileWeapon"))
            {
                WeaponProjectileDataEntry w = WeaponProjectileDataEntry.FromString(l);
                _weapons_projectile.Add((w.WeaponSize, w.Weapon), w);
            }
            else if (l.Trim().StartsWith("BeamWeapon"))
            {
                WeaponBeamDataEntry w = WeaponBeamDataEntry.FromString(l);
                _weapons_beam.Add((w.WeaponSize, w.Weapon), w);
            }
            else if (l.Trim().StartsWith("TorpedoWeapon"))
            {
                _weapons_torpedo = WeaponTorpedoDataEntry.FromString(l);
            }
        }
    }

    private static void LoadNamingLists()
    {
        _cultureNamingLists = new Dictionary<string, CultureNames>();
        _cultureNamingLists.Add("Terran", NamingSystem.Load());
    }

    private static void GenerateWarheadsSampleFile()
    {
        Warhead dummyWarhead = new Warhead()
        {
            ShieldDamage = 40,
            ArmourPenetration = 150,
            ArmourDamage = 5,
            SystemDamage = 10,
            HullDamage = 5,
            HeatGenerated = 0
        };
        WarheadDataEntry3[] d3 = new WarheadDataEntry3[]
        {
            new WarheadDataEntry3 { LaunchWeaponSize = "Light", LaunchWeaponType = "Autocannon", Ammo = "KineticPenetrator", WarheadData = dummyWarhead },
            new WarheadDataEntry3 { LaunchWeaponSize = "Light", LaunchWeaponType = "Autocannon", Ammo = "ShapedCharge", WarheadData = dummyWarhead },
        };
        WarheadDataEntry2[] d2 = new WarheadDataEntry2[]
        {
            new WarheadDataEntry2 { LaunchWeaponSize = "Light", LaunchWeaponType = "Lance", WarheadData = dummyWarhead },
            new WarheadDataEntry2 { LaunchWeaponSize = "Light", LaunchWeaponType = "Laser", WarheadData = dummyWarhead },
        };

        StringBuilder sb = new StringBuilder();
        foreach (WarheadDataEntry3 e in d3)
        {
            sb.AppendLine(e.ToTextLine());
        }
        foreach (WarheadDataEntry2 e in d2)
        {
            sb.AppendLine(e.ToTextLine());
        }


        System.IO.File.WriteAllText(System.IO.Path.Combine("TextData","Warheads.txt"), sb.ToString());
    }

    public enum WeaponBehaviorType { Unknown, Gun, Beam, ContinuousBeam, Torpedo, BomberTorpedo, Special }
    public enum WeaponEffect { None, SmallExplosion, BigExplosion, FlakBurst, KineticImpactSparks, PlasmaExplosion, DamageElectricSparks }
    public enum ShipSize { Sloop = 0, Frigate = 1, Destroyer = 2, Cruiser = 3, CapitalShip = 4 }
    public enum TacMapEntityType { Torpedo, SrikeCraft, Sloop, Frigate, Destroyer, Cruiser, CapitalShip, StaticDefence }

    private static Dictionary<(string, string, string), WarheadDataEntry3> _gunWarheads = null;
    private static Dictionary<(string, string), WarheadDataEntry2> _otherWarheads = null;
    private static Dictionary<string, WarheadDataEntry4> _torpedoWarheads = null;

    private static Dictionary<string, TurretMountDataEntry> _weaponMounts = null; // Key: MountType
    private static Dictionary<(string, string), WeaponProjectileDataEntry> _weapons_projectile = null; // Key: WeaponSize, WeaponType
    private static Dictionary<(string, string), WeaponBeamDataEntry> _weapons_beam = null;

    private static WeaponTorpedoDataEntry _weapons_torpedo = null;
    private static ArmourPenetrationTable _penetrationTable = null;
    private static Dictionary<string, CultureNames> _cultureNamingLists = null;
    private static Dictionary<(string, string), List<TacMapEntityType>> _defaultPriorityLists = null;
    private static Dictionary<(string, string, string, string), TurretDefinition> _turretDefinitions = null; // Key: MountType, WeaponNum, WeaponSize, WeaponType
    private static Dictionary<string, ShipHullDefinition> _shipHullDefinitions = null;
    
    private static readonly int _allTargetableLayerMask = LayerMask.GetMask("Ships", "Shields", "Strike Craft", "Torpedoes");
    private static readonly int _allShipsLayerMask = LayerMask.GetMask("Ships", "Shields", "Strike Craft");
    private static readonly int _allStrikeCraftLayerMask = LayerMask.GetMask("Strike Craft");
    private static readonly int _allShipsNoStikeCraftLayerMask = LayerMask.GetMask("Ships", "Shields");
    private static readonly int _allShipsNoShieldsLayerMask = LayerMask.GetMask("Ships", "Strike Craft");
    private static readonly int _allShipsNoStrikeCraftNoShieldsLayerMask = LayerMask.GetMask("Ships");
    private static readonly int _navBoxesLayerMask = LayerMask.GetMask("NavColliders");
    private static readonly int _navBoxesStrikeCraftLayerMask = LayerMask.GetMask("NavCollidersStrikeCraft");
    private static readonly int _navBoxesAllLayerMask = LayerMask.GetMask("NavColliders", "NavCollidersStrikeCraft", "NavCollidersTorpedoes");

    private static readonly int _defaultLayer = LayerMask.NameToLayer("Default");
    private static readonly int _shipsLayer = LayerMask.NameToLayer("Ships");
    private static readonly int _shieldsLayer = LayerMask.NameToLayer("Shields");
    private static readonly int _navCollidersLayer = LayerMask.NameToLayer("NavColliders");
    private static readonly int _weaponsLayer = LayerMask.NameToLayer("Weapons");
    private static readonly int _effectsLayer = LayerMask.NameToLayer("Effects");

    public class WarheadDataEntry3
    {
        public string LaunchWeaponType;
        public string LaunchWeaponSize;
        public string Ammo;
        public Warhead WarheadData;
        public string EffectAssetBundle;
        public string EffectAsset;

        public string ToTextLine()
        {
            string[] elements = new string[]
            {
                "3",
                LaunchWeaponSize,
                LaunchWeaponType,
                Ammo,
                WarheadData.ShieldDamage.ToString(),
                WarheadData.ArmourPenetration.ToString(),
                WarheadData.ArmourDamage.ToString(),
                WarheadData.SystemDamage.ToString(),
                WarheadData.HullDamage.ToString(),
                WarheadData.HeatGenerated.ToString(),
                WarheadData.HitMultiplicity.ToString(),
                WarheadData.BlastRadius.ToString(),
                WarheadData.AntiPersonnel.ToString(),
                WarheadData.WeaponEffectScale.ToString(),
                EffectAssetBundle,
                EffectAsset
            };
            return string.Join(",", elements);
        }

        public static WarheadDataEntry3 FromString(string s)
        {
            string[] elements = s.Trim().Split(',');
            if (elements[0].Trim() == "3")
            {
                int i = 1;
                return new WarheadDataEntry3()
                {
                    LaunchWeaponSize = elements[i++].Trim(),
                    LaunchWeaponType = elements[i++].Trim(),
                    Ammo = elements[i++].Trim(),
                    WarheadData = new Warhead()
                    {
                        ShieldDamage = int.Parse(elements[i++].Trim()),
                        ArmourPenetration = int.Parse(elements[i++].Trim()),
                        ArmourDamage = int.Parse(elements[i++].Trim()),
                        SystemDamage = int.Parse(elements[i++].Trim()),
                        HullDamage = int.Parse(elements[i++].Trim()),
                        HeatGenerated = int.Parse(elements[i++].Trim()),
                        HitMultiplicity = int.Parse(elements[i++].Trim()),
                        BlastRadius = float.Parse(elements[i++].Trim()),
                        EffectVsStrikeCraft = float.Parse(elements[i++].Trim()),
                        AntiPersonnel = float.Parse(elements[i++].Trim()),
                        WeaponEffectScale = float.Parse(elements[i++].Trim())
                    },
                    EffectAssetBundle = elements[i++].Trim(),
                    EffectAsset = elements[i++].Trim()
                };
            }
            else
            {
                return null;
            }
        }
    }

    public class WarheadDataEntry2
    {
        public string LaunchWeaponType;
        public string LaunchWeaponSize;
        public Warhead WarheadData;
        public string EffectAssetBundle;
        public string EffectAsset;

        public string ToTextLine()
        {
            string[] elements = new string[]
            {
                "2",
                LaunchWeaponSize,
                LaunchWeaponType,
                WarheadData.ShieldDamage.ToString(),
                WarheadData.ArmourPenetration.ToString(),
                WarheadData.ArmourDamage.ToString(),
                WarheadData.SystemDamage.ToString(),
                WarheadData.HullDamage.ToString(),
                WarheadData.HeatGenerated.ToString(),
                WarheadData.HitMultiplicity.ToString(),
                WarheadData.AntiPersonnel.ToString(),
                WarheadData.WeaponEffectScale.ToString(),
                EffectAssetBundle,
                EffectAsset
            };
            return string.Join(",", elements);
        }

        public static WarheadDataEntry2 FromString(string s)
        {
            string[] elements = s.Trim().Split(',');
            if (elements[0].Trim() == "2")
            {
                int i = 1;
                return new WarheadDataEntry2()
                {
                    LaunchWeaponSize = elements[i++].Trim(),
                    LaunchWeaponType = elements[i++].Trim(),
                    WarheadData = new Warhead()
                    {
                        ShieldDamage = int.Parse(elements[i++].Trim()),
                        ArmourPenetration = int.Parse(elements[i++].Trim()),
                        ArmourDamage = int.Parse(elements[i++].Trim()),
                        SystemDamage = int.Parse(elements[i++].Trim()),
                        HullDamage = int.Parse(elements[i++].Trim()),
                        HeatGenerated = int.Parse(elements[i++].Trim()),
                        HitMultiplicity = int.Parse(elements[i++].Trim()),
                        AntiPersonnel = float.Parse(elements[i++].Trim()),
                        WeaponEffectScale = float.Parse(elements[i++].Trim())
                    },
                    EffectAssetBundle = elements[i++].Trim(),
                    EffectAsset = elements[i++].Trim()
                };
            }
            else
            {
                return null;
            }
        }
    }

    public class WarheadDataEntry4
    {
        public string LaunchTorpedoType;
        public int SpeardSize;
        public float MaxRange;
        public Warhead WarheadData;
        public float ProjectileScale;
        public string EffectAssetBundle;
        public string EffectAsset;

        public string ToTextLine()
        {
            string[] elements = new string[]
            {
                "4",
                LaunchTorpedoType.ToString(),
                SpeardSize.ToString(),
                MaxRange.ToString(),
                WarheadData.ShieldDamage.ToString(),
                WarheadData.ArmourPenetration.ToString(),
                WarheadData.ArmourDamage.ToString(),
                WarheadData.SystemDamage.ToString(),
                WarheadData.HullDamage.ToString(),
                WarheadData.HeatGenerated.ToString(),
                WarheadData.HitMultiplicity.ToString(),
                WarheadData.AntiPersonnel.ToString(),
                WarheadData.WeaponEffectScale.ToString(),
                ProjectileScale.ToString(),
                EffectAssetBundle,
                EffectAsset
            };
            return string.Join(",", elements);
        }

        public static WarheadDataEntry4 FromString(string s)
        {
            string[] elements = s.Trim().Split(',');
            if (elements[0].Trim() == "4")
            {
                int i = 1;
                return new WarheadDataEntry4()
                {
                    LaunchTorpedoType = elements[i++].Trim(),
                    SpeardSize = int.Parse(elements[i++].Trim()),
                    MaxRange = float.Parse(elements[i++].Trim()),
                    WarheadData = new Warhead()
                    {
                        ShieldDamage = int.Parse(elements[i++].Trim()),
                        ArmourPenetration = int.Parse(elements[i++].Trim()),
                        ArmourDamage = int.Parse(elements[i++].Trim()),
                        SystemDamage = int.Parse(elements[i++].Trim()),
                        HullDamage = int.Parse(elements[i++].Trim()),
                        HeatGenerated = int.Parse(elements[i++].Trim()),
                        HitMultiplicity = int.Parse(elements[i++].Trim()),
                        AntiPersonnel = float.Parse(elements[i++].Trim()),
                        WeaponEffectScale = float.Parse(elements[i++].Trim())
                    },
                    ProjectileScale = float.Parse(elements[i++].Trim()),
                    EffectAssetBundle = elements[i++].Trim(),
                    EffectAsset = elements[i++].Trim()
                };
            }
            else
            {
                return null;
            }
        }
    }

    public class TurretMountDataEntry
    {
        public string MountSize;
        public string Mount;
        public int HitPoints;
        public float RotationSpeed;

        public static TurretMountDataEntry FromString(string s)
        {
            string[] elements = s.Trim().Split(',');
            if (elements[0].Trim() == "WeaponMount")
            {
                return new TurretMountDataEntry()
                {
                    MountSize = elements[1].Trim(),
                    Mount = elements[2].Trim(),
                    HitPoints = int.Parse(elements[3].Trim()),
                    RotationSpeed = float.Parse(elements[4].Trim())
                };
            }
            else
            {
                return null;
            }
        }
    }

    public class WeaponProjectileDataEntry
    {
        public string WeaponSize;
        public string Weapon;
        public float MaxRange;
        public float MuzzleVelocity;
        public float FiringInterval;
        public float MaxSpread;
        public float ProjectileScale;
        public int EnergyToFire;
        public int HeatToFire;

        public static WeaponProjectileDataEntry FromString(string s)
        {
            string[] elements = s.Trim().Split(',');
            if (elements[0].Trim() == "ProjectileWeapon")
            {
                int i = 1;
                return new WeaponProjectileDataEntry()
                {
                    WeaponSize = elements[i++].Trim(),
                    Weapon = elements[i++].Trim(),
                    MaxRange = float.Parse(elements[i++].Trim()),
                    MuzzleVelocity = float.Parse(elements[i++].Trim()),
                    FiringInterval = float.Parse(elements[i++].Trim()),
                    MaxSpread = float.Parse(elements[i++].Trim()),
                    ProjectileScale = float.Parse(elements[i++].Trim()),
                    EnergyToFire = int.Parse(elements[i++].Trim()),
                    HeatToFire = int.Parse(elements[i++].Trim())
                };
            }
            else
            {
                return null;
            }
        }
    }

    public class WeaponBeamDataEntry
    {
        public string WeaponSize;
        public string Weapon;
        public float MaxRange;
        public float FiringInterval;
        public float BeamDuration;
        public float BeamScale;
        public int EnergyToFire;
        public int HeatToFire;

        public static WeaponBeamDataEntry FromString(string s)
        {
            string[] elements = s.Trim().Split(',');
            if (elements[0].Trim() == "BeamWeapon")
            {
                int i = 1;
                return new WeaponBeamDataEntry()
                {
                    WeaponSize = elements[i++].Trim(),
                    Weapon = elements[i++].Trim(),
                    MaxRange = float.Parse(elements[i++].Trim()),
                    FiringInterval = float.Parse(elements[i++].Trim()),
                    BeamDuration = float.Parse(elements[i++].Trim()),
                    BeamScale = float.Parse(elements[i++].Trim()),
                    EnergyToFire = int.Parse(elements[i++].Trim()),
                    HeatToFire = int.Parse(elements[i++].Trim())
                };
            }
            else
            {
                return null;
            }
        }
    }

    public class WeaponTorpedoDataEntry
    {
        public float FiringInterval;
        public int EnergyToFire;
        public int HeatToFire;

        public static WeaponTorpedoDataEntry FromString(string s)
        {
            string[] elements = s.Trim().Split(',');
            if (elements[0].Trim() == "TorpedoWeapon")
            {
                int i = 1;
                return new WeaponTorpedoDataEntry()
                {
                    FiringInterval = float.Parse(elements[i++].Trim()),
                    EnergyToFire = int.Parse(elements[i++].Trim()),
                    HeatToFire = int.Parse(elements[i++].Trim())
                };
            }
            else
            {
                return null;
            }
        }
    }

    private static ObjectPrototypes _prototypes = null;
}

public class ShipTemplate
{
    public string ProductionKey;
    public float MaxSpeed;
    public float Mass;
    public float Thrust;
    public float Braking;
    public float TurnRate;
    public string[] CenterComponentSlots;
    public string[] ForeComponentSlots;
    public string[] AftComponentSlots;
    public string[] LeftComponentSlots;
    public string[] RightComponentSlots;
    public int DefaultArmorFront;
    public int DefaultArmorAft;
    public int DefaultArmorLeft;
    public int DefaultArmorRight;
}

