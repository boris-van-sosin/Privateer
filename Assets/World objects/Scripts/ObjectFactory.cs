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

    public static ParticleSystem CreateExplosion(Vector3 position)
    {
        if (_prototypes != null)
        {
            return _prototypes.CreateExplosion(position);
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

    public static Warhead CreateWarhead(WeaponType w, WeaponSize az, AmmoType a)
    {
        return null;
    }

    public static Warhead CreateWarhead(WeaponType w, WeaponSize az)
    {
        return null;
    }

    private static void LoadWarheads()
    {
        string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine("TextData", "weapons.txt"));
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


        System.IO.File.WriteAllText(System.IO.Path.Combine("TextData","weapons.txt"), sb.ToString());
    }

    public enum WeaponType { Autocannon, Howitzer, HVGun, Lance, Laser, PlasmaCannon }
    public enum WeaponSize { Light, Medium, Heavy }
    public enum AmmoType { KineticPenetrator, ShapedCharge, ShrapnelRound }

    private static Dictionary<Tuple<WeaponType, WeaponSize, AmmoType>, Warhead> _gunWarheads = null;
    private static Dictionary<Tuple<WeaponType, WeaponSize>, Warhead> _otherWarheads = null;

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
                        HeatGenerated = int.Parse(elements[9].Trim())
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
                        HeatGenerated = int.Parse(elements[8].Trim())
                    }
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
