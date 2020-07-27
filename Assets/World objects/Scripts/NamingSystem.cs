using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class NamingSystem
{
    public static void GenerateDummy()
    {
        string[] BritishList = new string[] { "British" };
        Subculture[] AllSubcultureList = new Subculture[]
        {
            new Subculture() { Name = "British", Prefix = "HMS" },
            new Subculture() { Name = "Civilian", Prefix = null }
        };
        ShipName s0 = new ShipName() { ShortName = "Dummy Merchant", FullName = "Long Dummy Merchant", Fluff = "Dummy fluff", Subcultures = null };
        ShipName s1 = new ShipName() { ShortName = "Dummy Sloop", FullName = "Long Dummy Sloop", Fluff = "Dummy fluff", Subcultures = null };
        ShipName s2 = new ShipName() { ShortName = "Dummy Frigate", FullName = "Long Dummy Frigate", Fluff = "Dummy fluff", Subcultures = null };
        ShipName s3 = new ShipName() { ShortName = "Dummy Destroyer", FullName = "Long Dummy Destroyer", Fluff = "Dummy fluff", Subcultures = null };
        ShipName s4 = new ShipName() { ShortName = "Dummy Cruiser", FullName = "Long Dummy Cruiser", Fluff = "Dummy fluff", Subcultures = null };
        ShipName s5 = new ShipName() { ShortName = "Dummy Capital Ship", FullName = "Long Dummy Capital Ship", Fluff = "Dummy fluff", Subcultures = null };

        NameGroup g0 = new NameGroup() { ClassName = null, ShipNames = new ShipName[] { s0 } };
        NameGroup g1 = new NameGroup() { ClassName = null, ShipNames = new ShipName[] { s1 } };
        NameGroup g2 = new NameGroup() { ClassName = null, ShipNames = new ShipName[] { s2 } };
        NameGroup g3 = new NameGroup() { ClassName = null, ShipNames = new ShipName[] { s3 } };
        NameGroup g4 = new NameGroup() { ClassName = null, ShipNames = new ShipName[] { s4 } };
        NameGroup g5 = new NameGroup() { ClassName = null, ShipNames = new ShipName[] { s5 } };

        FleetName n0 = new FleetName() { Name = "Blah", PreferredNumber = null };

        CultureNames names = new CultureNames()
        {
            Name = "Terran",
            Subcultures = AllSubcultureList,
            MerchantShips = new NameGroup[] { g0 },
            Sloops = new NameGroup[] { g1 },
            Frigates= new NameGroup[] { g2 },
            Destroyers = new NameGroup[] { g3 },
            Cruisers = new NameGroup[] { g4 },
            CapitalShips = new NameGroup[] { g5 },
            FleetNames = new FleetName[] { n0 },
            NumberConvention = NumberingSystem.Roman
        };
        using (StreamWriter tw = new StreamWriter(Path.Combine("TextData", "ShipNamesTerranExample.txt"), false, System.Text.Encoding.UTF8))
        {
            YamlDotNet.Serialization.Serializer s = new YamlDotNet.Serialization.Serializer();
            s.Serialize(tw, names);
        }
    }

    public static CultureNames Load()
    {
        //string nameListText = File.ReadAllText(Path.Combine("TextData", "ShipNamesTerran.txt"), System.Text.Encoding.UTF8);
        using (StreamReader sr = new StreamReader(Path.Combine("TextData", "ShipNamesTerran.txt"), System.Text.Encoding.UTF8))
        {
            YamlDotNet.Serialization.Deserializer ds = new YamlDotNet.Serialization.Deserializer();
            CultureNames res = ds.Deserialize<CultureNames>(sr);
            return res;
        }
    }

    public static ShipDisplayName GenShipName(CultureNames nameList, string subculture, ShipType shipType)
    {
        return GenShipName(nameList, subculture, shipType, Enumerable.Empty<string>());
    }

    public static ShipDisplayName GenShipName(CultureNames nameList, string subculture, ShipType shipType, IEnumerable<string> usedNames)
    {
        string prefix;
        Subculture currSubCulture = nameList.Subcultures.FirstOrDefault(s => s.Name == subculture);
        if (currSubCulture != null && currSubCulture.Prefix != null && currSubCulture.Prefix != string.Empty)
        {
            prefix = currSubCulture.Prefix;
        }
        else
        {
            prefix = string.Empty;
        }

        NameGroup ng = nameList.RandomNameGroup(shipType, subculture);
        ShipName n =
            ng.HasUnusedNames(subculture, usedNames) ? 
                ng.RandomName(subculture, usedNames)
                :
                ng.RandomName(subculture);
        if (!n.Numbered)
        {
            return new ShipDisplayName()
            {
                ShortName = prefix != string.Empty ? string.Format("{0} {1}", prefix, n.ShortName) : n.ShortName,
                FullName = prefix != string.Empty ? string.Format("{0} {1}", prefix, n.FullName) : n.FullName,
                FullNameKey = n.FullName,
                Fluff = n.Fluff
            };
        }
        else
        {
            return ng.GetNumberedName(prefix, n);
        }
    }

    public enum ShipType { Any, Merchant, Sloop, Frigate, Destroyer, Cruiser, CapitalShip, SpecialCapitalShip }
}

public struct ShipDisplayName
{
    public string ShortName { get; set; }
    public string FullName { get; set; }
    public string FullNameKey { get; set; }
    public string Fluff { get; set; }
}

[Serializable]
public struct ShipName
{
    public string ShortName { get; set; }
    public string FullName { get; set; }
    public string Fluff { get; set; }
    public string[] Subcultures { get; set; }
    public bool Numbered { get; set; }

    public bool AllowedInSubculture(string subculture)
    {
        return Subcultures == null ||
            Subcultures.Length == 0 ||
            Subcultures.Contains(subculture);
    }
}

[Serializable]
public class NameGroup
{
    public string ClassName { get; set; }
    public ShipName[] ShipNames { get; set; }

    public ShipName RandomName()
    {
        return ObjectFactory.GetRandom(ShipNames);
    }

    public ShipName RandomName(IEnumerable<string> usedNames)
    {
        return ObjectFactory.GetRandom(ShipNames.Where(n => !usedNames.Contains(n.FullName)));
    }

    public ShipName RandomName(string subculture)
    {
        return ObjectFactory.GetRandom(ShipNames.Where(n => n.AllowedInSubculture(subculture)));
    }

    public ShipName RandomName(string subculture, IEnumerable<string> usedNames)
    {
        return ObjectFactory.GetRandom(ShipNames.Where(
            n =>
                ((n.Numbered || !usedNames.Contains(n.FullName)) &&
                n.AllowedInSubculture(subculture))));
    }

    public bool HasUnusedNames(string subculture, IEnumerable<string> usedNames)
    {
        return ShipNames.Any(n =>
                                ((n.Numbered || !usedNames.Contains(n.FullName)) &&
                                n.AllowedInSubculture(subculture)));
    }

    public ShipDisplayName GetNumberedName(string prefix, ShipName name)
    {
        int num;
        if (!_numberedNaming.TryGetValue(name.FullName, out num))
        {
            num = 0;
        }
        else
        {
            ++num;
        }
        _numberedNaming[name.FullName] = num;

        return new ShipDisplayName()
        {
            ShortName = prefix != string.Empty ? string.Format("{0} {1}{2}", prefix, name.ShortName, num) : string.Format("{0}{1}", name.ShortName, num),
            FullName = prefix != string.Empty ? string.Format("{0} {1}{2}", prefix, name.FullName, num) : string.Format("{0}{1}", name.FullName, num),
            FullNameKey = name.FullName,
            Fluff = name.Fluff
        };
    }

    private Dictionary<string, int> _numberedNaming = new Dictionary<string, int>();
}

[Serializable]
public class FleetName
{
    public string Name { get; set; }
    public int? PreferredNumber { get; set; }
}

[Serializable]
public class Subculture
{
    public string Name { get; set; }
    public string Prefix { get; set; }
}

[Serializable]
public class CultureNames
{
    public string Name { get; set; }
    public Subculture[] Subcultures { get; set; }
    public NameGroup[] MerchantShips { get; set; }
    public NameGroup[] Sloops { get; set; }
    public NameGroup[] Frigates { get; set; }
    public NameGroup[] Destroyers { get; set; }
    public NameGroup[] Cruisers { get; set; }
    public NameGroup[] CapitalShips { get; set; }
    public NameGroup[] SpecialCapitalShips { get; set; }
    public FleetName[] FleetNames { get; set; }
    public NumberingSystem NumberConvention { get; set; }

    public NameGroup RandomNameGroup()
    {
        int typeIdx = UnityEngine.Random.Range(0, 7);
        NameGroup[] gs;
        switch (typeIdx)
        {
            case 0:
                gs = MerchantShips;
                break;
            case 1:
                gs = Sloops;
                break;
            case 2:
                gs = Frigates;
                break;
            case 3:
                gs = Destroyers;
                break;
            case 4:
                gs = Cruisers;
                break;
            case 5:
                gs = CapitalShips;
                break;
            case 6:
                gs = SpecialCapitalShips;
                break;
            default:
                gs = null;
                break;
        }
        return ObjectFactory.GetRandom(gs);
    }

    public NameGroup RandomNameGroup(string requiredSubculture)
    {
        int typeIdx = UnityEngine.Random.Range(0, 7);
        NameGroup[] gs;
        switch (typeIdx)
        {
            case 0:
                gs = MerchantShips;
                break;
            case 1:
                gs = Sloops;
                break;
            case 2:
                gs = Frigates;
                break;
            case 3:
                gs = Destroyers;
                break;
            case 4:
                gs = Cruisers;
                break;
            case 5:
                gs = CapitalShips;
                break;
            case 6:
                gs = SpecialCapitalShips;
                break;
            default:
                gs = null;
                break;
        }
        return ObjectFactory.GetRandom(gs.Where(g => g.ShipNames.Any(n => n.AllowedInSubculture(requiredSubculture))));
    }

    public NameGroup RandomNameGroup(NamingSystem.ShipType shipType, string requiredSubculture)
    {
        NameGroup[] gs;
        switch (shipType)
        {
            case NamingSystem.ShipType.Any:
                return RandomNameGroup();
            case NamingSystem.ShipType.Merchant:
                gs = MerchantShips;
                break;
            case NamingSystem.ShipType.Sloop:
                gs = Sloops;
                break;
            case NamingSystem.ShipType.Frigate:
                gs = Frigates;
                break;
            case NamingSystem.ShipType.Destroyer:
                gs = Destroyers;
                break;
            case NamingSystem.ShipType.Cruiser:
                gs = Cruisers;
                break;
            case NamingSystem.ShipType.CapitalShip:
                gs = CapitalShips;
                break;
            case NamingSystem.ShipType.SpecialCapitalShip:
                gs = SpecialCapitalShips;
                break;
            default:
                gs = null;
                break;
        }
        return ObjectFactory.GetRandom(gs.Where(g => g.ShipNames.Any(n => n.Subcultures == null || n.Subcultures.Length == 0 || n.Subcultures.Contains(requiredSubculture))));
    }
}

public enum NumberingSystem { None, Arabic, Roman }
