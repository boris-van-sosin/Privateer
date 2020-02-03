using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;
using System;

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

    public static Torpedo CreateTorpedo(Vector3 launchVector, Vector3 launchOrientation, Vector3 target, TorpedoType tt, ShipBase origShip)
    {
        if (_prototypes != null)
        {
            WarheadDataEntry4 torpData = _torpedoWarheads[tt];
            Torpedo t = CreateTorpedo(launchVector, launchOrientation, target, torpData.MaxRange, CreateWarhead(tt), origShip);
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

    public static T GetRandom<T>(IEnumerable<T> lst)
    {
        int numElems = lst.Count();
        if (numElems == 0)
        {
            return lst.ElementAt(10000);
        }
        return lst.ElementAt(UnityEngine.Random.Range(0, numElems));
    }

    public static Warhead CreateWarhead(WeaponType w, WeaponSize sz, AmmoType a)
    {
        return _gunWarheads[new ValueTuple<WeaponType, WeaponSize, AmmoType>(w, sz, a)];
    }

    public static Warhead CreateWarhead(WeaponType w, WeaponSize sz)
    {
        return _otherWarheads[new ValueTuple<WeaponType, WeaponSize>(w, sz)];
    }

    public static Warhead CreateWarhead(TorpedoType tt)
    {
        return _torpedoWarheads[tt].WarheadData;
    }

    public static ValueTuple<int, float> TorpedoLaunchDataFromTorpedoType(TorpedoType tt)
    {
        WarheadDataEntry4 tropData = _torpedoWarheads[tt];
        return new ValueTuple<int, float>(tropData.SpeardSize, tropData.MaxRange);
    }

    public static string[] GetAllShipTypes()
    {
        return _prototypes.GetAllShipTypes();
    }

    public static Ship CreateShip(string prodKey)
    {
        return _prototypes.CreateShip(prodKey);
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

    public static TurretBase CreateTurret(ComponentSlotType turretType, WeaponType weaponType)
    {
        string prodKey = turretType.ToString() + weaponType.ToString();
        TurretBase t = _prototypes.CreateTurret(prodKey);
        TurretMountDataEntry md = _weaponMounts[SlotTypeToMountKey(turretType)];
        t.MaxHitpoints = md.HitPoints;
        t.ComponentHitPoints = md.HitPoints;
        t.RotationSpeed = md.RotationSpeed;
        switch (weaponType)
        {
            case WeaponType.Autocannon:
            case WeaponType.Howitzer:
            case WeaponType.HVGun:
                {
                    WeaponProjectileDataEntry wpd = _weapons_projectile[SlotAndWeaponToWeaponKey(turretType, weaponType)];
                    GunTurret gt = t as GunTurret;
                    gt.ProjectileScale = wpd.ProjectileScale;
                    gt.MaxRange = wpd.MaxRange;
                    gt.MuzzleVelocity = wpd.MuzzleVelocity;
                    gt.FiringInterval = wpd.FiringInterval;
                    gt.Inaccuracy = wpd.MaxSpread;
                    gt.EnergyToFire = wpd.EnergyToFire;
                    gt.HeatToFire = wpd.HeatToFire;
                    gt.DefaultAlternatingFire = (weaponType == WeaponType.Autocannon);
                }
                break;
            case WeaponType.Lance:
                {
                    WeaponBeamDataEntry wbd = _weapons_beam[SlotAndWeaponToWeaponKey(turretType, weaponType)];
                    BeamTurret bt = t as BeamTurret;
                    bt.MaxRange = wbd.MaxRange;
                    bt.FiringInterval = wbd.FiringInterval;
                    bt.BeamDuration = wbd.BeamDuration;
                    bt.Inaccuracy = 0f;
                    bt.EnergyToFire = wbd.EnergyToFire;
                    bt.HeatToFire = wbd.HeatToFire;
                }
                break;
            case WeaponType.Laser:
                {
                    WeaponBeamDataEntry wbd = _weapons_beam[SlotAndWeaponToWeaponKey(turretType, weaponType)];
                    ContinuousBeamTurret cbt = t as ContinuousBeamTurret;
                    cbt.MaxRange = wbd.MaxRange;
                    cbt.FiringInterval = wbd.FiringInterval;
                    cbt.BeamDuration = wbd.BeamDuration;
                    cbt.Inaccuracy = 0f;
                    cbt.EnergyToFire = wbd.EnergyToFire;
                    cbt.HeatToFire = wbd.HeatToFire;
                }
                break;
            case WeaponType.PlasmaCannon:
                {
                    WeaponProjectileDataEntry wpd = _weapons_projectile[SlotAndWeaponToWeaponKey(turretType, weaponType)];
                    SpecialProjectileTurret st = t as SpecialProjectileTurret;
                    st.MaxRange = wpd.MaxRange;
                    st.MuzzleVelocity = wpd.MuzzleVelocity;
                    st.FiringInterval = wpd.FiringInterval;
                    st.Inaccuracy = wpd.MaxSpread;
                    st.EnergyToFire = wpd.EnergyToFire;
                    st.HeatToFire = wpd.HeatToFire;
                }
                break;
            case WeaponType.TorpedoTube:
                {
                    WeaponTorpedoDataEntry tordpedoData = _weapons_torpedo;
                    TorpedoTurret tt = t as TorpedoTurret;
                    tt.FiringInterval = tordpedoData.FiringInterval;
                    tt.EnergyToFire = tordpedoData.EnergyToFire;
                    tt.HeatToFire = tordpedoData.HeatToFire;
                }
                break;
            default:
                break;
        }
        return t;
    }

    public static TurretBase CreateStrikeCraftTurret(ComponentSlotType turretType, WeaponType weaponType)
    {
        string prodKey = turretType.ToString() + weaponType.ToString();
        TurretBase t = _prototypes.CreateTurret(prodKey);
        switch (weaponType)
        {
            case WeaponType.FighterCannon:
            case WeaponType.FighterAutoannon:
                {
                    WeaponProjectileDataEntry wpd = _weapons_projectile[SlotAndWeaponToWeaponKey(turretType, weaponType)];
                    GunTurret gt = t as GunTurret;
                    gt.ProjectileScale = wpd.ProjectileScale;
                    gt.MaxRange = wpd.MaxRange;
                    gt.MuzzleVelocity = wpd.MuzzleVelocity;
                    gt.FiringInterval = wpd.FiringInterval;
                    gt.DefaultAlternatingFire = (weaponType == WeaponType.FighterAutoannon);
                }
                break;
            case WeaponType.TorpedoTube:
                {
                    WeaponTorpedoDataEntry tordpedoData = _weapons_torpedo;
                    BomberTorpedoLauncher tt = t as BomberTorpedoLauncher;
                    tt.FiringInterval = tordpedoData.FiringInterval;
                }
                break;
            default:
                break;
        }
        t.EnergyToFire = 0;
        t.HeatToFire = 0;
        return t;
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
            if (hp.AllowedWeaponTypes.Contains(ComponentSlotType.FighterCannon))
            {
                TurretBase t = CreateStrikeCraftTurret(ComponentSlotType.FighterCannon, WeaponType.FighterCannon);
                s.PlaceTurret(hp, t);
            }
            else if (hp.AllowedWeaponTypes.Contains(ComponentSlotType.FighterAutogun))
            {
                TurretBase t = CreateStrikeCraftTurret(ComponentSlotType.FighterAutogun, WeaponType.FighterAutoannon);
                s.PlaceTurret(hp, t);
            }
            else if (hp.AllowedWeaponTypes.Contains(ComponentSlotType.BomberAutogun))
            {
                TurretBase t = CreateStrikeCraftTurret(ComponentSlotType.BomberAutogun, WeaponType.FighterAutoannon);
                s.PlaceTurret(hp, t);
            }
            else if (hp.AllowedWeaponTypes.Contains(ComponentSlotType.BomberTorpedoTube))
            {
                TurretBase t = CreateStrikeCraftTurret(ComponentSlotType.BomberTorpedoTube, WeaponType.TorpedoTube);
                s.PlaceTurret(hp, t);
            }
        }
        s.gameObject.AddComponent<StrikeCraftAIController>();
        return s;
    }

    public static StrikeCraftFormation CreateStrikeCraftFormation(string prodKey)
    {
        StrikeCraftFormation res = _prototypes.CreateStrikeCraftFormation(prodKey);
        res.CreatePositions(4);
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

    public static List<TacMapEntityType> GetDefaultTargetPriorityList(WeaponType weaponType, WeaponSize sz)
    {
        List<TacMapEntityType> res;
        if (_defaultPriorityLists.TryGetValue(new ValueTuple<WeaponType, WeaponSize>(weaponType, sz), out res))
        {
            return new List<TacMapEntityType>(res);
        }
        return null;
    }

    public static int AllTargetableLayerMask { get { return _allTargetableLayerMask; } }
    public static int AllShipsLayerMask { get { return _allShipsLayerMask; } }
    public static int AllStikeCraftLayerMask { get { return _allStrikeCraftLayerMask; } }
    public static int AllShipsNoStikeCraftLayerMask { get { return _allShipsNoStikeCraftLayerMask; } }
    public static int AllShipsNoShieldsLayerMask { get { return _allShipsNoShieldsLayerMask; } }
    public static int AllShipsNoShieldsNoStikeCraftLayerMask { get { return _allShipsNoStrikeCraftNoShieldsLayerMask; } }
    public static int NavBoxesLayerMask { get { return _navBoxesLayerMask; } }
    public static int NavBoxesStrikeCraftLayerMask { get { return _navBoxesStrikeCraftLayerMask; } }
    public static int NavBoxesAllLayerMask { get { return _navBoxesAllLayerMask; } }

    private static void LoadWarheads()
    {
        string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine("TextData", "Warheads.txt"));
        _gunWarheads = new Dictionary<ValueTuple<WeaponType, WeaponSize, AmmoType>, Warhead>();
        _torpedoWarheads = new Dictionary<TorpedoType, WarheadDataEntry4>();
        _otherWarheads = new Dictionary<ValueTuple<WeaponType, WeaponSize>, Warhead>();
        foreach (string l in lines)
        {
            if (l.Trim().StartsWith("3"))
            {
                WarheadDataEntry3 d3 = WarheadDataEntry3.FromString(l);

                _gunWarheads.Add(new ValueTuple<WeaponType, WeaponSize, AmmoType>(d3.LaunchWeaponType, d3.LaunchWeaponSize, d3.Ammo), d3.WarheadData);
            }
            else if (l.Trim().StartsWith("2"))
            {
                WarheadDataEntry2 d2 = WarheadDataEntry2.FromString(l);
                _otherWarheads.Add(new ValueTuple<WeaponType, WeaponSize>(d2.LaunchWeaponType, d2.LaunchWeaponSize), d2.WarheadData);
            }
            else if (l.Trim().StartsWith("4"))
            {
                WarheadDataEntry4 d4 = WarheadDataEntry4.FromString(l);
                _torpedoWarheads.Add(d4.LaunchTorpedoType, d4);
            }
        }
    }

    private static ValueTuple<WeaponSize, TurretMountType> SlotTypeToMountKey(ComponentSlotType t)
    {
        switch (t)
        {
            case ComponentSlotType.SmallFixed:
                return new ValueTuple<WeaponSize, TurretMountType>(WeaponSize.Light, TurretMountType.Fixed);
            case ComponentSlotType.SmallBroadside:
                return new ValueTuple<WeaponSize, TurretMountType>(WeaponSize.Light, TurretMountType.Broadside);
            case ComponentSlotType.SmallBarbette:
                return new ValueTuple<WeaponSize, TurretMountType>(WeaponSize.Light, TurretMountType.Barbette);
            case ComponentSlotType.SmallTurret:
                return new ValueTuple<WeaponSize, TurretMountType>(WeaponSize.Light, TurretMountType.Turret);
            case ComponentSlotType.SmallBarbetteDual:
                return new ValueTuple<WeaponSize, TurretMountType>(WeaponSize.Light, TurretMountType.Barbette);
            case ComponentSlotType.SmallTurretDual:
                return new ValueTuple<WeaponSize, TurretMountType>(WeaponSize.Light, TurretMountType.Turret);
            case ComponentSlotType.MediumBroadside:
                return new ValueTuple<WeaponSize, TurretMountType>(WeaponSize.Medium, TurretMountType.Barbette);
            case ComponentSlotType.MediumBarbette:
                return new ValueTuple<WeaponSize, TurretMountType>(WeaponSize.Medium, TurretMountType.Barbette);
            case ComponentSlotType.MediumTurret:
                return new ValueTuple<WeaponSize, TurretMountType>(WeaponSize.Medium, TurretMountType.Turret);
            case ComponentSlotType.MediumBarbetteDualSmall:
                return new ValueTuple<WeaponSize, TurretMountType>(WeaponSize.Medium, TurretMountType.Barbette);
            case ComponentSlotType.MediumTurretDualSmall:
                return new ValueTuple<WeaponSize, TurretMountType>(WeaponSize.Medium, TurretMountType.Turret);
            case ComponentSlotType.LargeBarbette:
                return new ValueTuple<WeaponSize, TurretMountType>(WeaponSize.Heavy, TurretMountType.Barbette);
            case ComponentSlotType.LargeTurret:
                return new ValueTuple<WeaponSize, TurretMountType>(WeaponSize.Heavy, TurretMountType.Turret);
            case ComponentSlotType.TorpedoTube:
                return new ValueTuple<WeaponSize, TurretMountType>(WeaponSize.TorpedoTube, TurretMountType.TorpedoTube);
            default:
                throw new Exception("Weapon mount not found");
        }
    }

    private static ValueTuple<WeaponSize, WeaponType> SlotAndWeaponToWeaponKey(ComponentSlotType t, WeaponType w)
    {
        switch (t)
        {
            case ComponentSlotType.SmallFixed:
            case ComponentSlotType.SmallBroadside:
            case ComponentSlotType.SmallBarbette:
            case ComponentSlotType.SmallTurret:
            case ComponentSlotType.SmallBarbetteDual:
            case ComponentSlotType.SmallTurretDual:
            case ComponentSlotType.MediumBarbetteDualSmall:
            case ComponentSlotType.MediumTurretDualSmall:
                return new ValueTuple<WeaponSize, WeaponType>(WeaponSize.Light, w);
            case ComponentSlotType.MediumBroadside:
            case ComponentSlotType.MediumBarbette:
            case ComponentSlotType.MediumTurret:
                return new ValueTuple<WeaponSize, WeaponType>(WeaponSize.Medium, w);
            case ComponentSlotType.LargeBarbette:
            case ComponentSlotType.LargeTurret:
                return new ValueTuple<WeaponSize, WeaponType>(WeaponSize.Heavy, w);
            case ComponentSlotType.FighterCannon:
            case ComponentSlotType.FighterAutogun:
            case ComponentSlotType.BomberAutogun:
                return new ValueTuple<WeaponSize, WeaponType>(WeaponSize.StrikeCraft, w);
            default:
                throw new Exception("Weapon not found");
        }
    }

    private static void LoadDefaultPriorityLists()
    {
        _defaultPriorityLists = new Dictionary<(WeaponType, WeaponSize), List<TacMapEntityType>>();
        string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine("TextData", "DefaultPriorities.csv"));
        int idx = 0;
        while (!lines[idx].Trim().StartsWith("x"))
        {
            ++idx;
        }
        WeaponType[] typesOrder = lines[idx].Split(',').Skip(1).Select(s => (WeaponType) Enum.Parse(typeof(WeaponType), s)).ToArray();
        foreach (string line in lines.Skip(idx + 1))
        {
            string[] items = line.Split(',');
            WeaponSize sz = (WeaponSize) Enum.Parse(typeof(WeaponSize), items[0]);
            for (int i = 0; i < items.Length - 1; ++i)
            {
                if (items[i + 1] != string.Empty)
                {
                    _defaultPriorityLists.Add(
                        new ValueTuple<WeaponType, WeaponSize>(typesOrder[i], sz),
                        items[i + 1].Split(':').Select(s => (TacMapEntityType)Enum.Parse(typeof(TacMapEntityType), s)).ToList());
                }
            }
        }
    }

    private static void LoadWeaponMounts()
    {
        string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine("TextData", "WeaponMounts.txt"));
        _weaponMounts = new Dictionary<ValueTuple<WeaponSize, TurretMountType>, TurretMountDataEntry>();
        foreach (string l in lines)
        {
            if (l.Trim().StartsWith("WeaponMount"))
            {
                TurretMountDataEntry tm = TurretMountDataEntry.FromString(l);
                _weaponMounts.Add(new ValueTuple<WeaponSize, TurretMountType>(tm.MountSize, tm.Mount), tm);
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
        _weapons_projectile = new Dictionary<ValueTuple<WeaponSize, WeaponType>, WeaponProjectileDataEntry>();
        _weapons_beam = new Dictionary<ValueTuple<WeaponSize, WeaponType>, WeaponBeamDataEntry>();
        foreach (string l in lines)
        {
            if (l.Trim().StartsWith("ProjectileWeapon"))
            {
                WeaponProjectileDataEntry w = WeaponProjectileDataEntry.FromString(l);
                _weapons_projectile.Add(new ValueTuple<WeaponSize, WeaponType>(w.MountSize, w.Weapon), w);
            }
            else if (l.Trim().StartsWith("BeamWeapon"))
            {
                WeaponBeamDataEntry w = WeaponBeamDataEntry.FromString(l);
                _weapons_beam.Add(new ValueTuple<WeaponSize, WeaponType>(w.MountSize, w.Weapon), w);
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
            new WarheadDataEntry3 { LaunchWeaponSize = WeaponSize.Light, LaunchWeaponType = WeaponType.Autocannon, Ammo = AmmoType.KineticPenetrator, WarheadData = dummyWarhead },
            new WarheadDataEntry3 { LaunchWeaponSize = WeaponSize.Light, LaunchWeaponType = WeaponType.Autocannon, Ammo = AmmoType.ShapedCharge, WarheadData = dummyWarhead },
        };
        WarheadDataEntry2[] d2 = new WarheadDataEntry2[]
        {
            new WarheadDataEntry2 { LaunchWeaponSize = WeaponSize.Light, LaunchWeaponType = WeaponType.Lance, WarheadData = dummyWarhead },
            new WarheadDataEntry2 { LaunchWeaponSize = WeaponSize.Light, LaunchWeaponType = WeaponType.Laser, WarheadData = dummyWarhead },
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

    public enum TurretMountType { Fixed, Broadside, Barbette, Turret, TorpedoTube }
    public enum WeaponType { Autocannon, Howitzer, HVGun, Lance, Laser, PlasmaCannon, TorpedoTube, FighterCannon, FighterAutoannon }
    public enum WeaponSize { Light, Medium, Heavy, TorpedoTube, StrikeCraft }
    public enum AmmoType { KineticPenetrator, ShapedCharge, ShrapnelRound }
    public enum TorpedoType { LongRange, Heavy, Tracking }
    public enum WeaponEffect { None, SmallExplosion, BigExplosion, FlakBurst, KineticImpactSparks, PlasmaExplosion, DamageElectricSparks }
    public enum ShipSize { Sloop = 0, Frigate = 1, Destroyer = 2, Cruiser = 3, CapitalShip = 4 }
    public enum TacMapEntityType { Torpedo, SrikeCraft, Sloop, Frigate, Destroyer, Cruiser, CapitalShip, StaticDefence }

    private static Dictionary<ValueTuple<WeaponType, WeaponSize, AmmoType>, Warhead> _gunWarheads = null;
    private static Dictionary<ValueTuple<WeaponType, WeaponSize>, Warhead> _otherWarheads = null;
    private static Dictionary<TorpedoType, WarheadDataEntry4> _torpedoWarheads = null;
    private static Dictionary<ValueTuple<WeaponSize, TurretMountType>, TurretMountDataEntry> _weaponMounts = null;
    private static Dictionary<ValueTuple<WeaponSize, WeaponType>, WeaponProjectileDataEntry> _weapons_projectile = null;
    private static Dictionary<ValueTuple<WeaponSize, WeaponType>, WeaponBeamDataEntry> _weapons_beam = null;
    private static WeaponTorpedoDataEntry _weapons_torpedo = null;
    private static ArmourPenetrationTable _penetrationTable = null;
    private static Dictionary<string, CultureNames> _cultureNamingLists = null;
    private static Dictionary<ValueTuple<WeaponType, WeaponSize>, List<TacMapEntityType>> _defaultPriorityLists = null;
    private static readonly int _allTargetableLayerMask = LayerMask.GetMask("Ships", "Shields", "Strike Craft", "Torpedoes");
    private static readonly int _allShipsLayerMask = LayerMask.GetMask("Ships", "Shields", "Strike Craft");
    private static readonly int _allStrikeCraftLayerMask = LayerMask.GetMask("Strike Craft");
    private static readonly int _allShipsNoStikeCraftLayerMask = LayerMask.GetMask("Ships", "Shields");
    private static readonly int _allShipsNoShieldsLayerMask = LayerMask.GetMask("Ships", "Strike Craft");
    private static readonly int _allShipsNoStrikeCraftNoShieldsLayerMask = LayerMask.GetMask("Ships");
    private static readonly int _navBoxesLayerMask = LayerMask.GetMask("NavColliders");
    private static readonly int _navBoxesStrikeCraftLayerMask = LayerMask.GetMask("NavCollidersStrikeCraft");
    private static readonly int _navBoxesAllLayerMask = LayerMask.GetMask("NavColliders", "NavCollidersStrikeCraft", "NavCollidersTorpedoes");

    public class WarheadDataEntry3
    {
        public WeaponType LaunchWeaponType;
        public WeaponSize LaunchWeaponSize;
        public AmmoType Ammo;
        public Warhead WarheadData;

        public string ToTextLine()
        {
            string[] elements = new string[]
            {
                "3",
                LaunchWeaponSize.ToString(),
                LaunchWeaponType.ToString(),
                Ammo.ToString(),
                WarheadData.ShieldDamage.ToString(),
                WarheadData.ArmourPenetration.ToString(),
                WarheadData.ArmourDamage.ToString(),
                WarheadData.SystemDamage.ToString(),
                WarheadData.HullDamage.ToString(),
                WarheadData.HeatGenerated.ToString(),
                WarheadData.HitMultiplicity.ToString(),
                WarheadData.BlastRadius.ToString(),
                WarheadData.AntiPersonnel.ToString(),
                WarheadData.WeaponEffectScale.ToString()
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
                    LaunchWeaponSize = (WeaponSize)System.Enum.Parse(typeof(WeaponSize), elements[i++].Trim(), true),
                    LaunchWeaponType = (WeaponType)System.Enum.Parse(typeof(WeaponType), elements[i++].Trim(), true),
                    Ammo = (AmmoType)System.Enum.Parse(typeof(AmmoType), elements[i++].Trim(), true),
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
                    }
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
        public WeaponType LaunchWeaponType;
        public WeaponSize LaunchWeaponSize;
        public Warhead WarheadData;

        public string ToTextLine()
        {
            string[] elements = new string[]
            {
                "2",
                LaunchWeaponSize.ToString(),
                LaunchWeaponType.ToString(),
                WarheadData.ShieldDamage.ToString(),
                WarheadData.ArmourPenetration.ToString(),
                WarheadData.ArmourDamage.ToString(),
                WarheadData.SystemDamage.ToString(),
                WarheadData.HullDamage.ToString(),
                WarheadData.HeatGenerated.ToString(),
                WarheadData.HitMultiplicity.ToString(),
                WarheadData.AntiPersonnel.ToString(),
                WarheadData.WeaponEffectScale.ToString()

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
                    LaunchWeaponSize = (WeaponSize)System.Enum.Parse(typeof(WeaponSize), elements[i++].Trim(), true),
                    LaunchWeaponType = (WeaponType)System.Enum.Parse(typeof(WeaponType), elements[i++].Trim(), true),
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
                    }
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
        public TorpedoType LaunchTorpedoType;
        public int SpeardSize;
        public float MaxRange;
        public Warhead WarheadData;
        public float ProjectileScale;

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
                ProjectileScale.ToString()
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
                    LaunchTorpedoType = (TorpedoType)System.Enum.Parse(typeof(TorpedoType), elements[i++].Trim(), true),
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
                    ProjectileScale = float.Parse(elements[i++].Trim())
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
        public WeaponSize MountSize;
        public TurretMountType Mount;
        public int HitPoints;
        public float RotationSpeed;

        public static TurretMountDataEntry FromString(string s)
        {
            string[] elements = s.Trim().Split(',');
            if (elements[0].Trim() == "WeaponMount")
            {
                return new TurretMountDataEntry()
                {
                    MountSize = (WeaponSize)System.Enum.Parse(typeof(WeaponSize), elements[1].Trim(), true),
                    Mount = (TurretMountType)System.Enum.Parse(typeof(TurretMountType), elements[2].Trim(), true),
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
        public WeaponSize MountSize;
        public WeaponType Weapon;
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
                    MountSize = (WeaponSize)System.Enum.Parse(typeof(WeaponSize), elements[i++].Trim(), true),
                    Weapon = (WeaponType)System.Enum.Parse(typeof(WeaponType), elements[i++].Trim(), true),
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
        public WeaponSize MountSize;
        public WeaponType Weapon;
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
                    MountSize = (WeaponSize)System.Enum.Parse(typeof(WeaponSize), elements[i++].Trim(), true),
                    Weapon = (WeaponType)System.Enum.Parse(typeof(WeaponType), elements[i++].Trim(), true),
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
    public ComponentSlotType[] CenterComponentSlots;
    public ComponentSlotType[] ForeComponentSlots;
    public ComponentSlotType[] AftComponentSlots;
    public ComponentSlotType[] LeftComponentSlots;
    public ComponentSlotType[] RightComponentSlots;
    public int DefaultArmorFront;
    public int DefaultArmorAft;
    public int DefaultArmorLeft;
    public int DefaultArmorRight;
}

