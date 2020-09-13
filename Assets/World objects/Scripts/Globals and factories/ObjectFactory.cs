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
            GameObject.DontDestroyOnLoad(_prototypes);
        }
        if (_gunWarheads == null || _otherWarheads == null || _torpedoWarheads == null)
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
        if (_weaponImagePaths == null || _weaponSizeImagePaths == null)
        {
            LoadWeaponImages();
        }
    }

    private static Projectile CreateProjectile()
    {
        if (_prototypes != null)
        {
            Projectile p = _prototypes.CreateProjectile();
            return p;
        }
        else
        {
            return null;
        }
    }

    public static Projectile AcquireProjectile(Vector3 position, Vector3 firingVector, float velocity, float range, float projectileScale, Warhead w, ShipBase origShip)
    {
        if (_prototypes != null)
        {
            Projectile res;
            bool needsReset = false;
            if (_objCache.ProjectileCache.Count > 0)
            {
                res = _objCache.ProjectileCache.Acquire();
                needsReset = true;
            }
            else
            {
                res = CreateProjectile();
            }
            Quaternion q = Quaternion.LookRotation(Vector3.up, firingVector);
            res.transform.position = position;
            res.transform.rotation = q;
            res.Speed = velocity;
            res.Range = range;
            res.OriginShip = origShip;
            res.SetScale(projectileScale);
            res.ProjectileWarhead = w;
            if (needsReset)
            {
                res.ResetObject();
            }
            return res;
        }
        else
        {
            return null;
        }
    }

    public static void ReleaseProjectile(Projectile p)
    {
        _objCache.ProjectileCache.Release(p);
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


    private static HarpaxBehavior CreateHarpaxProjectile()
    {
        if (_prototypes != null)
        {
            HarpaxBehavior p = _prototypes.CreateHarpaxProjectile();
            return p;
        }
        else
        {
            return null;
        }
    }

    public static HarpaxBehavior AcquireHarpaxProjectile(Vector3 position, Vector3 firingVector, float velocity, float range, float scale, ShipBase origShip)
    {
        if (_prototypes != null)
        {
            HarpaxBehavior res;
            bool needsReset = false;
            if (_objCache.HarpaxCache.Count > 0)
            {
                res = _objCache.HarpaxCache.Acquire();
                needsReset = true;
            }
            else
            {
                res = CreateHarpaxProjectile();
            }
            res.transform.position = position;
            Quaternion q = Quaternion.LookRotation(Vector3.up, firingVector);
            res.transform.rotation = q;
            res.Speed = velocity;
            res.Range = range;
            res.OriginShip = origShip;
            if (needsReset)
            {
                res.ResetObject();
            }
            return res;
        }
        else
        {
            return null;
        }
    }

    public static void ReleaseHarpaxProjectile(HarpaxBehavior h)
    {
        _objCache.HarpaxCache.Release(h);
    }

    public static CableBehavior CreateHarpaxTowCable()
    {
        CableBehavior res = _prototypes.CreateHarpaxCable();
        return res;
    }

    public static CableBehavior AcquireHarpaxTowCable(Rigidbody obj1, Rigidbody obj2, Vector3 targetConnectionPoint)
    {
        if (_prototypes != null)
        {
            CableBehavior res;
            bool needsReset = false;
            if (_objCache.HarpaxCableCache.Count > 0)
            {
                res = _objCache.HarpaxCableCache.Acquire();
                needsReset = true;
            }
            else
            {
                res = CreateHarpaxTowCable();
            }
            if (needsReset)
            {
                res.ResetObject();
            }
            res.Connect(obj1, obj2, targetConnectionPoint);
            return res;
        }
        else
        {
            return null;
        }
    }

    public static void ReleaseHHarpaxTowCable(CableBehavior c)
    {
        _objCache.HarpaxCableCache.Release(c);
    }

    private static Torpedo CreateTorpedo()
    {
        if (_prototypes != null)
        {
            Torpedo t = _prototypes.CreateTorpedo();
            return t;
        }
        else
        {
            return null;
        }
    }

    public static Torpedo AcquireTorpedo(Vector3 position, Vector3 launchVector, Vector3 launchOrientation, Vector3 target, float range, Warhead w, float torpedoScale, ShipBase origShip)
    {
        if (_prototypes != null)
        {
            Torpedo t;
            bool needsReset = false;
            if (_objCache.TorpedoCache.Count > 0)
            {
                t = _objCache.TorpedoCache.Acquire();
                needsReset = true;
            }
            else
            {
                t = CreateTorpedo();
            }
            t.transform.position = position;
            t.ProjectileWarhead = w;
            t.transform.localScale = Vector3.one * torpedoScale;
            Quaternion q = Quaternion.LookRotation(Vector3.up, launchOrientation);
            t.transform.rotation = q;
            t.OriginShip = origShip;
            t.Target = target;
            t.Range = range;
            t.ColdLaunchVec = launchVector;
            if (needsReset)
            {
                t.ResetObject();
            }
            return t;
        }
        else
        {
            return null;
        }
    }

    public static void ReleaseTorpedo(Torpedo t)
    {
        _objCache.TorpedoCache.Release(t);
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
            ObjectCache.CacheWithRecycler<ParticleSystem> currCache = _objCache.GetOrCreateParticleSystemCache(assetBundleSource, asset);
            _objCache.AdvanceAllParticleSystemRecyclers(Time.time);
            if (currCache.Count > 0)
            {
                ParticleSystem res = currCache.Acquire();
                res.transform.position = position;
                res.gameObject.SetActive(true);
                //Debug.LogFormat("Got a particle system ({0}) from cache", asset);
                return res;
            }
            else
            {
                GameObject resObj = _loader.CreateObjectByPath(assetBundleSource, asset, "");
                //Debug.LogFormat("Created a new particle system ({0})", asset);
                ParticleSystem res = resObj.GetComponent<ParticleSystem>();
                if (res != null)
                {
                    res.transform.position = position;
                    res.gameObject.SetActive(true);
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
        ps.gameObject.SetActive(false);
        ObjectCache.CacheWithRecycler<ParticleSystem> currCache = _objCache.GetOrCreateParticleSystemCache(assetBundleSource, asset);
        currCache.Recycle(ps);
    }

    public static void ReleaseParticleSystem(string assetBundleSource, string asset, ParticleSystem ps, float delay)
    {
        if (_prototypes != null)
        {
            float recycleTime = Time.time + delay;
            _objCache.RecycleParticleSystem(assetBundleSource, asset, ps, recycleTime);
            //_prototypes.QueueDelayedAction(_objCache.AdvanceAllParticleSystemRecyclers, recycleTime);
        }
        else
        {
            GameObject.Destroy(ps, delay);
        }
    }

    /*
    public static ParticleSystem CreateParticleSystem(string assetBundleSource, string asset)
    {
        GameObject resObj = _loader.CreateObjectByPath(assetBundleSource, asset, "");
        //Debug.LogFormat("Created a new particle system ({0})", asset);
        ParticleSystem res = resObj.GetComponent<ParticleSystem>();
        if (res != null)
        {
            return res;
        }
        else
        {
            GameObject.Destroy(resObj);
            return null;
        }
    }
    */

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
        if (_gunWarheads == null || _otherWarheads == null || _torpedoWarheads == null)
        {
            LoadWarheads();
        }
        return _gunWarheads[(weaponType, size, ammo)].WarheadData;
    }

    public static Warhead CreateWarhead(string weaponType, string size)
    {
        if (_gunWarheads == null || _otherWarheads == null || _torpedoWarheads == null)
        {
            LoadWarheads();
        }
        return _otherWarheads[(weaponType, size)].WarheadData;
    }

    public static Warhead CreateWarhead(string torpType)
    {
        if (_gunWarheads == null || _otherWarheads == null || _torpedoWarheads == null)
        {
            LoadWarheads();
        }
        return _torpedoWarheads[torpType].WarheadData;
    }

    public static (int, float, float, Warhead) TorpedoLaunchDataFromTorpedoType(string torpType)
    {
        WarheadDataEntry4 torpData = _torpedoWarheads[torpType];
        return (torpData.SpeardSize, torpData.MaxRange, torpData.ProjectileScale, torpData.WarheadData);
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

    public static IReadOnlyList<string> GetAllShipTypes()
    {
        if (_shipHulls == null)
        {
            _shipHulls = GetAllShipHulls().Select(h => h.HullName).ToList();
        }
        return _shipHulls;
    }

    public static Ship CreateShip(string prodKey)
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

        GameObject hullObj = HierarchyConstructionUtil.ConstructHierarchy(hullRoot, _loader, ShipsLayer, ShipsLayer, EffectsLayer);
        GameObject[] particleSysObjs = new GameObject[]
        {
            HierarchyConstructionUtil.ConstructHierarchy(damageSmoke, _loader, ShipsLayer, ShipsLayer, EffectsLayer),
            HierarchyConstructionUtil.ConstructHierarchy(engineExhaustIdle, _loader, ShipsLayer, ShipsLayer, EffectsLayer),
            HierarchyConstructionUtil.ConstructHierarchy(engineExhaustOn, _loader, ShipsLayer, ShipsLayer, EffectsLayer),
            HierarchyConstructionUtil.ConstructHierarchy(engineExhaustBrake, _loader, ShipsLayer, ShipsLayer, EffectsLayer),
            HierarchyConstructionUtil.ConstructHierarchy(magneticField, _loader, ShipsLayer, ShipsLayer, EffectsLayer),
        };
        GameObject[] teamColorObjs = teamColor.Select(r => HierarchyConstructionUtil.ConstructHierarchy(r, _loader, ShipsLayer, ShipsLayer, EffectsLayer)).ToArray();

        GameObject resObj = _loader.CreateObjectEmpty();
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
            GameObject hardPointObj = _loader.CreateObjectEmpty();
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

        GameObject navBoxObj = _loader.CreateObjectEmpty();
        navBoxObj.name = "NavBox";
        navBoxObj.layer = NavCollidersLayer;
        navBoxObj.transform.parent = resObj.transform;
        navBoxObj.transform.localPosition = Vector3.zero;
        navBoxObj.transform.localRotation = Quaternion.identity;
        navBoxObj.transform.localScale = Vector3.one;

        GameObject meshSrc = _loader.GetObjectByPath(hullDef.CollisionMesh.AssetBundlePath, hullDef.CollisionMesh.AssetPath, hullDef.CollisionMesh.MeshPath);
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

        if (hullDef.CarrierModuleData != null)
        {
            CarrierBehavior carrierAddon = resObj.AddComponent<CarrierBehavior>();
            carrierAddon.CarrierHangerAnim = new GenericOpenCloseAnim[hullDef.CarrierModuleData.HangerRootNodes.Length];
            int animIdx = 0;
            foreach (HierarchyNode carreirComp in hullDef.CarrierModuleData.HangerRootNodes)
            {
                GameObject carrierCompObj = HierarchyConstructionUtil.ConstructHierarchy(carreirComp, _loader, ShipsLayer, ShipsLayer, EffectsLayer);
                Vector3 pos = carrierCompObj.transform.position;
                Quaternion rot = carrierCompObj.transform.rotation;
                Vector3 scale = carrierCompObj.transform.localScale;
                carrierCompObj.transform.parent = resObj.transform;
                carrierCompObj.transform.position = pos;
                carrierCompObj.transform.rotation = rot;
                carrierCompObj.transform.localScale = scale;

                carrierAddon.CarrierHangerAnim[animIdx++] = carrierCompObj.GetComponent<GenericOpenCloseAnim>();
            }
            carrierAddon.MaxFormations = hullDef.CarrierModuleData.MaxFormations;
        }

        s.PostAwake();

        return s;
    }

    public static Transform CreateShipDummy(string prodKey)
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
        HierarchyNode[] teamColor = hullDef.TeamColorComponents;

        GameObject hullObj = HierarchyConstructionUtil.ConstructHierarchy(hullRoot, _loader, ShipsLayer, ShipsLayer, EffectsLayer);
        GameObject[] particleSysObjs = new GameObject[]
        {
            HierarchyConstructionUtil.ConstructHierarchy(damageSmoke, _loader, ShipsLayer, ShipsLayer, EffectsLayer),
            HierarchyConstructionUtil.ConstructHierarchy(engineExhaustIdle, _loader, ShipsLayer, ShipsLayer, EffectsLayer),
            HierarchyConstructionUtil.ConstructHierarchy(engineExhaustOn, _loader, ShipsLayer, ShipsLayer, EffectsLayer),
            HierarchyConstructionUtil.ConstructHierarchy(engineExhaustBrake, _loader, ShipsLayer, ShipsLayer, EffectsLayer),
        };
        GameObject[] teamColorObjs = teamColor.Select(r => HierarchyConstructionUtil.ConstructHierarchy(r, _loader, ShipsLayer, ShipsLayer, EffectsLayer)).ToArray();

        GameObject resObj = _loader.CreateObjectEmpty();
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

        foreach (WeaponHardpointDefinition hardpoint in hullDef.WeaponHardpoints)
        {
            GameObject hardPointObj = _loader.CreateObjectEmpty();
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

        if (hullDef.CarrierModuleData != null)
        {
            foreach (HierarchyNode carreirComp in hullDef.CarrierModuleData.HangerRootNodes)
            {
                GameObject carrierCompObj = HierarchyConstructionUtil.ConstructHierarchy(carreirComp, _loader, ShipsLayer, ShipsLayer, EffectsLayer);
                Vector3 pos = carrierCompObj.transform.position;
                Quaternion rot = carrierCompObj.transform.rotation;
                Vector3 scale = carrierCompObj.transform.localScale;
                carrierCompObj.transform.parent = resObj.transform;
                carrierCompObj.transform.position = pos;
                carrierCompObj.transform.rotation = rot;
                carrierCompObj.transform.localScale = scale;
            }
        }

        return resObj.transform;
    }

    public static ShipHullDefinition GetShipTemplate(string prodKey)
    {
        if (_shipHullDefinitions == null)
        {
            LoadShipHullDefinitions();
        }
        ShipHullDefinition res;
        if (_shipHullDefinitions.TryGetValue(prodKey, out res))
        {
            return res;
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
        GameObject resObj = HierarchyConstructionUtil.ConstructHierarchy(root, _loader, WeaponsLayer, WeaponsLayer, EffectsLayer);
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
        resTurret.GlobalMaxHitpoints = md.HitPoints;
        resTurret.ComponentHitPoints = md.HitPoints;
        resTurret.RotationSpeed = md.RotationSpeed;
        resTurret.ComponentHitPoints = resTurret.ComponentGlobalMaxHitPoints;
        resTurret.Init(string.Format("{0}{1}{2}", turretMountType , weaponNum , weaponSize));

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
            gt.SetAmmoType(0, "KineticPenetrator");
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

    public static Transform CreateTurretDummy(string turretMountType, string weaponNum, string weaponSize, string weaponType)
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
        GameObject resObj = HierarchyConstructionUtil.ConstructHierarchy(root, _loader, WeaponsLayer, WeaponsLayer, EffectsLayer);

        return resObj.transform;
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
        StrikeCraft res = _prototypes.CreateStrikeCraft(prodKey);
        res.PostAwake();
        return res;
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
                if (t != null)
                {
                    BomberTorpedoLauncher tl = (BomberTorpedoLauncher)t;
                    tl.DummyTorpedoString = s.DummyTorpedoString;
                    tl.LoadedTorpedoType = "Heavy";
                }
                s.PlaceTurret(hp, t);
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

    public static IEnumerable<ShipHullDefinition> GetAllShipHulls()
    {
        if (_shipHullDefinitions == null)
        {
            LoadShipHullDefinitions();
        }
        return _shipHullDefinitions.Values;
    }

    public static IEnumerable<TurretDefinition> GetAllTurretTypes()
    {
        if (_turretDefinitions == null)
        {
            LoadTurretDefinitions();
        }
        return _turretDefinitions.Values;
    }

    public static IReadOnlyList<(string, string)> GetAllWeaponTypesAndSizes()
    {
        if (_weapons_beam == null || _weapons_projectile == null || _weapons_torpedo == null)
        {
            LoadWeapons();
        }
        return _weaponTypesAndSizes;
    }

    public static IReadOnlyList<string> GetAllWeaponTypes()
    {
        if (_weapons_beam == null || _weapons_projectile == null || _weapons_torpedo == null)
        {
            LoadWeapons();
        }
        List<string> res = new List<string>();
        for (int i = 0; i < _weaponTypesAndSizes.Count; ++i)
        {
            if (!res.Contains(_weaponTypesAndSizes[i].Item1))
            {
                res.Add(_weaponTypesAndSizes[i].Item1);
            }
        }
        return res;
    }

    public static IReadOnlyList<string> GetAllWeaponSizes()
    {
        if (_weapons_beam == null || _weapons_projectile == null || _weapons_torpedo == null)
        {
            LoadWeapons();
        }
        List<string> res = new List<string>();
        for (int i = 0; i < _weaponTypesAndSizes.Count; ++i)
        {
            if (!res.Contains(_weaponTypesAndSizes[i].Item2))
            {
                res.Add(_weaponTypesAndSizes[i].Item2);
            }
        }
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
        if (_penetrationTable == null)
        {
            LoadPenetrationTable();
        }
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

    public static Sprite GetShipPhoto(Ship s, Camera cam)
    {
        Sprite res;
        if (_shipPhotos.TryGetValue(s, out res))
        {
            return res;
        }
        else
        {
            res = ShipPhotoUtil.TakePhoto(s, 512, 512, cam);
            _shipPhotos[s] = res;
            return res;
        }
    }
    private static Dictionary<Ship, Sprite> _shipPhotos = new Dictionary<Ship, Sprite>();

    public static Sprite GetObjectPhoto(Transform t, bool cache, Camera cam)
    {
        Sprite res;
        if (cache && _objectPhotos.TryGetValue(t, out res))
        {
            return res;
        }
        else
        {
            res = ShipPhotoUtil.TakeObjectPhoto(t, 512, 512, cam);
            if (cache)
                _objectPhotos[t] = res;
            return res;
        }
    }
    private static Dictionary<Transform, Sprite> _objectPhotos = new Dictionary<Transform, Sprite>();

    public static Sprite GetWeaponImage(string weaponType)
    {
        if (_weaponImagePaths == null)
        {
            LoadWeaponImages();
        }
        return GetSpriteSpecInner(_weaponImagePaths, _weaponSpriteCache, weaponType);
    }

    public static Sprite GetWeaponSizeImage(string weaponSize)
    {
        if (_weaponSizeImagePaths == null)
        {
            LoadWeaponImages();
        }
        return GetSpriteSpecInner(_weaponSizeImagePaths, _weaponSizeSpriteCache, weaponSize);
    }

    public static Sprite GeAmmonImage(string weaponType)
    {
        if (_weaponImagePaths == null)
        {
            LoadWeaponImages();
        }
        return GetSpriteSpecInner(_weaponImagePaths, _weaponSpriteCache, weaponType);
    }

    public static Sprite GetTorpedoTypeImage(string weaponType)
    {
        if (_weaponImagePaths == null)
        {
            LoadWeaponImages();
        }
        return GetSpriteSpecInner(_weaponImagePaths, _weaponSpriteCache, weaponType);
    }

    private static Sprite GetSpriteSpecInner(Dictionary<string, (string, int, int, int, int, int, int)> imgPathDict, Dictionary<string, Sprite> spriteDict, string key)
    {
        (string, int, int, int, int, int, int) spriteItem;
        if (imgPathDict.TryGetValue(key, out spriteItem))
        {
            Sprite res;
            if (!spriteDict.TryGetValue(key, out res))
            {
                int w = spriteItem.Item2, h = spriteItem.Item3;
                res = Sprite.Create(_loader.GetImageByPath(spriteItem.Item1, w, h), new Rect(spriteItem.Item4, spriteItem.Item5, spriteItem.Item6, spriteItem.Item7), Vector2.zero);
                spriteDict[key] = res;
            }
            return res;
        }
        else
        {
            return null;
        }
    }

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
        string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine("TextData", "WeaponMounts.csv"));
        _weaponMounts = new Dictionary<string, TurretMountDataEntry>();
        foreach (string l in lines)
        {
            if (l.Trim().StartsWith("WeaponMount"))
            {
                TurretMountDataEntry tm = TurretMountDataEntry.FromString(l);
                string key = tm.MountSize + tm.Mount;
                _weaponMounts.Add(key, tm);
            }
        }
        _weaponMountTypes = _weaponMounts.Keys.ToList();
    }

    private static void LoadPenetrationTable()
    {
        string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine("TextData", "PenetrationChart.csv"));
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
        string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine("TextData", "Weapons.csv"));
        _weapons_projectile = new Dictionary<(string, string), WeaponProjectileDataEntry>();
        _weapons_beam = new Dictionary<(string, string), WeaponBeamDataEntry>();
        _weaponTypesAndSizes = new List<(string, string)>();
        foreach (string l in lines)
        {   
            if (l.Trim().StartsWith("ProjectileWeapon"))
            {
                WeaponProjectileDataEntry w = WeaponProjectileDataEntry.FromString(l);
                _weapons_projectile.Add((w.WeaponSize, w.Weapon), w);
                if (!_weaponTypesAndSizes.Contains((w.Weapon, w.WeaponSize)))
                {
                    _weaponTypesAndSizes.Add((w.Weapon, w.WeaponSize));
                }
            }
            else if (l.Trim().StartsWith("BeamWeapon"))
            {
                WeaponBeamDataEntry w = WeaponBeamDataEntry.FromString(l);
                _weapons_beam.Add((w.WeaponSize, w.Weapon), w);
                if (!_weaponTypesAndSizes.Contains((w.Weapon, w.WeaponSize)))
                {
                    _weaponTypesAndSizes.Add((w.Weapon, w.WeaponSize));
                }
            }
            else if (l.Trim().StartsWith("TorpedoWeapon"))
            {
                _weapons_torpedo = WeaponTorpedoDataEntry.FromString(l);
                if (!_weaponTypesAndSizes.Contains(("TorpedoWeapon", string.Empty)))
                {
                    _weaponTypesAndSizes.Add(("TorpedoWeapon", string.Empty));
                }
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


        System.IO.File.WriteAllText(System.IO.Path.Combine("TextData","Warheads.csv"), sb.ToString());
    }

    private static void LoadWeaponImages()
    {
        string[] lines = File.ReadAllLines(Path.Combine("TextData", "WeaponImages.csv"));
        _weaponImagePaths = new Dictionary<string, (string, int, int, int, int, int, int)>();
        _weaponSizeImagePaths = new Dictionary<string, (string, int, int, int, int, int, int)>();
        _ammoImagePaths = new Dictionary<string, (string, int, int, int, int, int, int)>();
        _torpedoTypeImagePaths = new Dictionary<string, (string, int, int, int, int, int, int)>();
        foreach (string l in lines)
        {
            string trimLn = l.Trim();
            if (!trimLn.StartsWith("#"))
            {
                string[] items = trimLn.Split(',');
                int i = 1;
                string key = items[i++];
                string imgFile = items[i++];
                int imgW = int.Parse(items[i++]);
                int imgH = int.Parse(items[i++]);
                int spriteX = int.Parse(items[i++]);
                int spriteY = int.Parse(items[i++]);
                int spriteW = int.Parse(items[i++]);
                int spriteH = int.Parse(items[i++]);
                if (items[0] == "Weapon")
                {
                    _weaponImagePaths.Add(key, (imgFile, imgW, imgH, spriteX, spriteY, spriteW, spriteH));
                }
                else if (items[0] == "Size")
                {
                    _weaponSizeImagePaths.Add(key, (imgFile, imgW, imgH, spriteX, spriteY, spriteW, spriteH));
                }
                else if (items[0] == "Ammo")
                {
                    _ammoImagePaths.Add(key, (imgFile, imgW, imgH, spriteX, spriteY, spriteW, spriteH));
                }
                else if (items[0] == "Torpedo")
                {
                    _torpedoTypeImagePaths.Add(key, (imgFile, imgW, imgH, spriteX, spriteY, spriteW, spriteH));
                }
            }
        }
    }

    public enum WeaponBehaviorType { Unknown, Gun, Beam, ContinuousBeam, Torpedo, BomberTorpedo, Special }
    public enum WeaponEffect { None, SmallExplosion, BigExplosion, FlakBurst, KineticImpactSparks, PlasmaExplosion, DamageElectricSparks }
    public enum ShipSize { Sloop = 0, Frigate = 1, Destroyer = 2, Cruiser = 3, CapitalShip = 4 }
    public enum TacMapEntityType { Torpedo, StrikeCraft, Sloop, Frigate, Destroyer, Cruiser, CapitalShip, StaticDefence }

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
    private static Dictionary<string, (string, int, int, int, int, int, int)> _weaponImagePaths = null;
    private static Dictionary<string, (string, int, int, int, int, int, int)> _weaponSizeImagePaths = null;
    private static Dictionary<string, (string, int, int, int, int, int, int)> _ammoImagePaths = null;
    private static Dictionary<string, (string, int, int, int, int, int, int)> _torpedoTypeImagePaths = null;
    private static Dictionary<string, Sprite> _weaponSpriteCache = new Dictionary<string, Sprite>();
    private static Dictionary<string, Sprite> _weaponSizeSpriteCache = new Dictionary<string, Sprite>();
    private static Dictionary<string, Sprite> _ammoSpriteCache = new Dictionary<string, Sprite>();
    private static Dictionary<string, Sprite> _torpedoTypeSpriteCache = new Dictionary<string, Sprite>();
    private static List<string> _shipHulls;
    private static List<(string, string)> _weaponTypesAndSizes;
    private static List<string> _weaponMountTypes;

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
    private static ObjectLoader _loader = new ObjectLoader();
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
