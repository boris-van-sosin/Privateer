using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

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
    }

    public static Projectile CreateProjectile(Vector3 firingVector, float velocity, float range, Warhead w, Ship origShip)
    {
        if (_prototypes != null)
        {
            Projectile p = _prototypes.CreateProjectile(firingVector, velocity, range, origShip);
            p.ProjectileWarhead = w;
            return p;
        }
        else
        {
            return null;
        }
    }

    public static Projectile CreatePlasmaProjectile(Vector3 firingVector, float velocity, float range, Warhead w, Ship origShip)
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

    public static HarpaxBehavior CreateHarpaxProjectile(Vector3 firingVector, float velocity, float range, Ship origShip)
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

    public static GameObject[] CreateHarpaxTowCable(Vector3 start, Vector3 end)
    {
        float HarapxCableSegLength = 0.5f;
        Vector3 cableVec = end - start, cableDir = cableVec.normalized;
        float length = cableVec.magnitude;
        int numSegs = Mathf.CeilToInt(length / HarapxCableSegLength);
        Quaternion cableRotation = Quaternion.FromToRotation(Vector3.up, cableDir);

        Vector3 currPt = start;
        GameObject[] res = new GameObject[numSegs];
        Joint prevJoint = null;
        for (int i = 0; i < numSegs; ++i)
        {
            GameObject currSeg = _prototypes.CreateHarpaxCableSeg();
            currSeg.transform.position = currPt + (0.5f * HarapxCableSegLength * cableDir);
            currSeg.transform.rotation = cableRotation;
            if (prevJoint != null)
            {
                Rigidbody rb = currSeg.GetComponent<Rigidbody>();
                prevJoint.connectedBody = rb;
            }
            prevJoint = currSeg.GetComponent<Joint>();
            currPt += (cableDir * HarapxCableSegLength);
            res[i] = currSeg;
        }

        return res;
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
        return _gunWarheads[Tuple<WeaponType, WeaponSize, AmmoType>.Create(w, sz, a)];
    }

    public static Warhead CreateWarhead(WeaponType w, WeaponSize sz)
    {
        return _otherWarheads[Tuple<WeaponType, WeaponSize>.Create(w, sz)];
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
                    gt.MaxRange = wpd.MaxRange;
                    gt.MuzzleVelocity = wpd.MuzzleVelocity;
                    gt.FiringInterval = wpd.FiringInterval;
                    gt.EnergyToFire = wpd.EnergyToFire;
                    gt.HeatToFire = wpd.HeatToFire;
                }
                break;
            case WeaponType.Lance:
                {
                    WeaponBeamDataEntry wbd = _weapons_beam[SlotAndWeaponToWeaponKey(turretType, weaponType)];
                    BeamTurret bt = t as BeamTurret;
                    bt.MaxRange = wbd.MaxRange;
                    bt.FiringInterval = wbd.FiringInterval;
                    bt.BeamDuration = wbd.BeamDuration;
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
                    st.EnergyToFire = wpd.EnergyToFire;
                    st.HeatToFire = wpd.HeatToFire;
                }
                break;
            default:
                break;
        }
        return t;
    }

    public static Sprite GetSprite(string key)
    {
        return _prototypes.GetSprite(key);
    }

    public static StatusSubsystem CreateStatusSubsytem(IShipActiveComponent comp)
    {
        StatusSubsystem res = _prototypes.CreateStatusSprite();
        res.SetImage(_prototypes.GetSprite(comp.SpriteKey));
        res.AttachComponent(comp);
        return res;
    }

    public static StatusTopLevel CreateStatusPanel(string prodKey)
    {
        StatusTopLevel res = _prototypes.CreateStatusPanel(prodKey);
        return res;
    }

    public static StatusTopLevel CreateStatusPanel(Ship s, Transform containingPanel)
    {
        StatusTopLevel res = CreateStatusPanel(s.ProductionKey);
        RectTransform rt = res.GetComponent<RectTransform>();
        rt.SetParent(containingPanel);
        res.AttachShip(s);
        return res;
    }

    public static Tuple<Canvas, BoardingProgressPanel> CreateBoardingProgressPanel()
    {
        return _prototypes.CreateBoardingProgressPanel();
    }

    public static WeaponCtrlCfgLine CreateWeaponCtrlCfgLine(string label)
    {
        WeaponCtrlCfgLine res = _prototypes.CreateWeaponCtrlCfgLine();
        res.WeaponTextBox.text = label;
        return res;
    }

    private static void LoadWarheads()
    {
        string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine("TextData", "Warheads.txt"));
        _gunWarheads = new Dictionary<Tuple<WeaponType, WeaponSize, AmmoType>, Warhead>();
        _otherWarheads = new Dictionary<Tuple<WeaponType, WeaponSize>, Warhead>();
        foreach (string l in lines)
        {
            if (l.Trim().StartsWith("3"))
            {
                WarheadDataEntry3 d3 = WarheadDataEntry3.FromString(l);

                _gunWarheads.Add(new Tuple<WeaponType, WeaponSize, AmmoType>(d3.LaunchWeaponType, d3.LaunchWeaponSize, d3.Ammo), d3.WarheadData);
            }
            else if (l.Trim().StartsWith("2"))
            {
                WarheadDataEntry2 d2 = WarheadDataEntry2.FromString(l);
                _otherWarheads.Add(new Tuple<WeaponType, WeaponSize>(d2.LaunchWeaponType, d2.LaunchWeaponSize), d2.WarheadData);
            }
        }
    }

    private static Tuple<WeaponSize, TurretMountType> SlotTypeToMountKey(ComponentSlotType t)
    {
        switch (t)
        {
            case ComponentSlotType.SmallFixed:
                return Tuple<WeaponSize, TurretMountType>.Create(WeaponSize.Light, TurretMountType.Fixed);
            case ComponentSlotType.SmallBroadside:
                return Tuple<WeaponSize, TurretMountType>.Create(WeaponSize.Light, TurretMountType.Broadside);
            case ComponentSlotType.SmallBarbette:
                return Tuple<WeaponSize, TurretMountType>.Create(WeaponSize.Light, TurretMountType.Barbette);
            case ComponentSlotType.SmallTurret:
                return Tuple<WeaponSize, TurretMountType>.Create(WeaponSize.Light, TurretMountType.Turret);
            case ComponentSlotType.SmallBarbetteDual:
                return Tuple<WeaponSize, TurretMountType>.Create(WeaponSize.Light, TurretMountType.Barbette);
            case ComponentSlotType.SmallTurretDual:
                return Tuple<WeaponSize, TurretMountType>.Create(WeaponSize.Light, TurretMountType.Turret);
            case ComponentSlotType.MediumBroadside:
                return Tuple<WeaponSize, TurretMountType>.Create(WeaponSize.Medium, TurretMountType.Barbette);
            case ComponentSlotType.MediumBarbette:
                return Tuple<WeaponSize, TurretMountType>.Create(WeaponSize.Medium, TurretMountType.Barbette);
            case ComponentSlotType.MediumTurret:
                return Tuple<WeaponSize, TurretMountType>.Create(WeaponSize.Medium, TurretMountType.Turret);
            case ComponentSlotType.MediumBarbetteDualSmall:
                return Tuple<WeaponSize, TurretMountType>.Create(WeaponSize.Medium, TurretMountType.Barbette);
            case ComponentSlotType.MediumTurretDualSmall:
                return Tuple<WeaponSize, TurretMountType>.Create(WeaponSize.Medium, TurretMountType.Turret);
            case ComponentSlotType.LargeBarbette:
                return Tuple<WeaponSize, TurretMountType>.Create(WeaponSize.Heavy, TurretMountType.Barbette);
            case ComponentSlotType.LargeTurret:
                return Tuple<WeaponSize, TurretMountType>.Create(WeaponSize.Heavy, TurretMountType.Turret);
            default:
                return null;
        }
    }

    private static Tuple<WeaponSize, WeaponType> SlotAndWeaponToWeaponKey(ComponentSlotType t, WeaponType w)
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
                return Tuple<WeaponSize, WeaponType>.Create(WeaponSize.Light, w);
            case ComponentSlotType.MediumBroadside:
            case ComponentSlotType.MediumBarbette:
            case ComponentSlotType.MediumTurret:
                return Tuple<WeaponSize, WeaponType>.Create(WeaponSize.Medium, w);
            case ComponentSlotType.LargeBarbette:
            case ComponentSlotType.LargeTurret:
                return Tuple<WeaponSize, WeaponType>.Create(WeaponSize.Heavy, w);
            default:
                return null;
        }
    }

    private static void LoadWeaponMounts()
    {
        string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine("TextData", "WeaponMounts.txt"));
        _weaponMounts = new Dictionary<Tuple<WeaponSize, TurretMountType>, TurretMountDataEntry>();
        foreach (string l in lines)
        {
            if (l.Trim().StartsWith("WeaponMount"))
            {
                TurretMountDataEntry tm = TurretMountDataEntry.FromString(l);
                _weaponMounts.Add(Tuple<WeaponSize, TurretMountType>.Create(tm.MountSize, tm.Mount), tm);
            }
        }
    }

    private static void LoadWeapons()
    {
        string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine("TextData", "Weapons.txt"));
        _weapons_projectile = new Dictionary<Tuple<WeaponSize, WeaponType>, WeaponProjectileDataEntry>();
        _weapons_beam = new Dictionary<Tuple<WeaponSize, WeaponType>, WeaponBeamDataEntry>();
        foreach (string l in lines)
        {
            if (l.Trim().StartsWith("ProjectileWeapon"))
            {
                WeaponProjectileDataEntry w = WeaponProjectileDataEntry.FromString(l);
                _weapons_projectile.Add(Tuple<WeaponSize, WeaponType>.Create(w.MountSize, w.Weapon), w);
            }
            else if (l.Trim().StartsWith("BeamWeapon"))
            {
                WeaponBeamDataEntry w = WeaponBeamDataEntry.FromString(l);
                _weapons_beam.Add(Tuple<WeaponSize, WeaponType>.Create(w.MountSize, w.Weapon), w);
            }
        }
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

    public enum TurretMountType { Fixed, Broadside, Barbette, Turret }
    public enum WeaponType { Autocannon, Howitzer, HVGun, Lance, Laser, PlasmaCannon }
    public enum WeaponSize { Light, Medium, Heavy }
    public enum AmmoType { KineticPenetrator, ShapedCharge, ShrapnelRound }
    public enum WeaponEffect { None, SmallExplosion, BigExplosion, FlakBurst, KineticImpactSparks, PlasmaExplosion, DamageElectricSparks }
    public enum ShipSize { Sloop = 0, Frigate = 1, Destroyer = 2, Cruiser = 3, CapitalShip = 4 }

    private static Dictionary<Tuple<WeaponType, WeaponSize, AmmoType>, Warhead> _gunWarheads = null;
    private static Dictionary<Tuple<WeaponType, WeaponSize>, Warhead> _otherWarheads = null;
    private static Dictionary<Tuple<WeaponSize, TurretMountType>, TurretMountDataEntry> _weaponMounts = null;
    private static Dictionary<Tuple<WeaponSize, WeaponType>, WeaponProjectileDataEntry> _weapons_projectile = null;
    private static Dictionary<Tuple<WeaponSize, WeaponType>, WeaponBeamDataEntry> _weapons_beam = null;

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
                WarheadData.HeatGenerated.ToString()
            };
            return string.Join(",", elements);
        }

        public static WarheadDataEntry3 FromString(string s)
        {
            string[] elements = s.Trim().Split(',');
            if (elements[0].Trim() == "3")
            {
                return new WarheadDataEntry3()
                {
                    LaunchWeaponSize = (WeaponSize) System.Enum.Parse(typeof(WeaponSize), elements[1].Trim(), true),
                    LaunchWeaponType = (WeaponType)System.Enum.Parse(typeof(WeaponType), elements[2].Trim(), true),
                    Ammo = (AmmoType)System.Enum.Parse(typeof(AmmoType), elements[3].Trim(), true),
                    WarheadData = new Warhead()
                    {
                        ShieldDamage = int.Parse(elements[4].Trim()),
                        ArmourPenetration = int.Parse(elements[5].Trim()),
                        ArmourDamage = int.Parse(elements[6].Trim()),
                        SystemDamage = int.Parse(elements[7].Trim()),
                        HullDamage = int.Parse(elements[8].Trim()),
                        HeatGenerated = int.Parse(elements[9].Trim()),
                        WeaponEffectScale = new Vector3(float.Parse(elements[10].Trim()), float.Parse(elements[11].Trim()), float.Parse(elements[12].Trim()))
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
                WarheadData.HeatGenerated.ToString()
            };
            return string.Join(",", elements);
        }

        public static WarheadDataEntry2 FromString(string s)
        {
            string[] elements = s.Trim().Split(',');
            if (elements[0].Trim() == "2")
            {
                return new WarheadDataEntry2()
                {
                    LaunchWeaponSize = (WeaponSize)System.Enum.Parse(typeof(WeaponSize), elements[1].Trim(), true),
                    LaunchWeaponType = (WeaponType)System.Enum.Parse(typeof(WeaponType), elements[2].Trim(), true),
                    WarheadData = new Warhead()
                    {
                        ShieldDamage = int.Parse(elements[3].Trim()),
                        ArmourPenetration = int.Parse(elements[4].Trim()),
                        ArmourDamage = int.Parse(elements[5].Trim()),
                        SystemDamage = int.Parse(elements[6].Trim()),
                        HullDamage = int.Parse(elements[7].Trim()),
                        HeatGenerated = int.Parse(elements[8].Trim()),
                        WeaponEffectScale = new Vector3(float.Parse(elements[9].Trim()), float.Parse(elements[10].Trim()), float.Parse(elements[11].Trim()))
                    }
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

