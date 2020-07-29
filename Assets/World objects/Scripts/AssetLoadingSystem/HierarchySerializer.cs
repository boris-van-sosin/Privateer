using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeResolvers;

public static class HierarchySerializer
{
    static public string SerializeObject(TurretBase turret)
    {
        TurretDefinition turretDef = TurretDefinition.FromTurret(turret);
        return SerializeObjectInner(turretDef, typeof(TurretDefinition));
    }

    static public string SerializeObject(Ship ship)
    {
        ShipHullDefinition shipHullDef = ShipHullDefinition.FromShip(ship);
        return SerializeObjectInner(shipHullDef, typeof(ShipHullDefinition));
    }

    static public string SerializeObject(Transform root)
    {
        HierarchyNode hierarchy = root.ToSerializableHierarchy();
        return SerializeObjectInner(hierarchy, typeof(HierarchyNode));
    }

    static private string SerializeObjectInner(System.Object toSerialize, Type targetType)
    {
        DynamicTypeResolver tr = new DynamicTypeResolver();

        YamlDotNet.Serialization.SerializerBuilder builder = new SerializerBuilder();
        builder.EnsureRoundtrip();
        builder.EmitDefaults();
        builder.DisableAliases();
        //builder.WithTypeResolver(tr);
        YamlDotNet.Serialization.Serializer s = builder.Build();

        System.IO.StringWriter tw = new System.IO.StringWriter();
        s.Serialize(tw, toSerialize, targetType);
        return tw.ToString();
    }

    static public T LoadHierarchy<T>(TextReader reader)
    {
        YamlDotNet.Serialization.Deserializer ds = new YamlDotNet.Serialization.Deserializer();
        return ds.Deserialize<T>(reader);
    }

    static public string TransformPath(Transform t, Transform upTo)
    {
        if (t == null)
            return string.Empty;
        else if (t.parent == null || t.parent == upTo)
            return t.name;
        else
            return string.Format("{0}/{1}", TransformPath(t.parent, upTo), t.name);
    }
}

public static class HierarchySerializationExtensions
{
    static public HierarchyNode ToSerializableHierarchy(this Transform t)
    {
        if (t == null)
        {
            return null;
        }

        HierarchyNode res = new HierarchyNode()
        {
            Name = t.name,
            Position = SerializableVector3.FromSerializable(t.localPosition),
            Rotation = SerializableVector3.FromSerializable(t.localRotation),
            Scale = SerializableVector3.FromSerializable(t.localScale),
            NodeMesh = null,
            NodeParticleSystem = null,
            OpenCloseData = null
        };

        MeshFilter meshFilter;
        ParticleSystem particleSys;
        GenericOpenCloseAnim openClose;
        if ((meshFilter = t.GetComponent<MeshFilter>()) != null)
        {
            res.NodeMesh = new MeshData()
            {
                DoCombine = true,
                AssetBundlePath = "",
                AssetPath = "",
                MeshPath = meshFilter.sharedMesh.name
            };
            if (res.NodeMesh.MeshPath.EndsWith(" Instance"))
            {
                res.NodeMesh.MeshPath = res.NodeMesh.MeshPath.Substring(0, res.NodeMesh.MeshPath.IndexOf(" Instance"));
            }
        }
        else if ((particleSys = t.GetComponent<ParticleSystem>()) != null)
        {
            res.NodeParticleSystem = new ParticleSystemData()
            {
                AssetBundlePath = "",
                AssetPath = "",
                ParticleSystemPath = particleSys.name
            };
        }
        else if ((openClose = t.GetComponent<GenericOpenCloseAnim>()) != null)
        {
            res.OpenCloseData = OpenCloseComponentData.FromOpenCloseAnim(openClose);
        }

        res.Name = t.name;
        res.Position = SerializableVector3.FromSerializable(t.localPosition);
        res.Rotation = SerializableVector3.FromSerializable(t.localRotation);
        res.Scale = SerializableVector3.FromSerializable(t.localScale);

        List<HierarchyNode> subNodes = new List<HierarchyNode>();
        for (int i = 0; i < t.childCount; i++)
        {
            subNodes.Add(t.GetChild(i).ToSerializableHierarchy());
        }
        res.SubNodes = subNodes.Where(n => n != null).ToArray();

        return res;
    }
}

[Serializable]
public class HierarchyNode
{
    public string Name { get; set; }
    public SerializableVector3 Position { get; set; }
    public SerializableVector3 Rotation { get; set; }
    public SerializableVector3 Scale { get; set; }

    [YamlIgnore]
    public Matrix4x4 Mat => Matrix4x4.TRS(Position.ToVector3(), Rotation.ToQuaternion(), Scale.ToVector3());
    public MeshData NodeMesh { get; set; }
    public ParticleSystemData NodeParticleSystem { get; set; }
    public OpenCloseComponentData OpenCloseData { get; set; }
    public HierarchyNode[] SubNodes { get; set; }
}

[Serializable]
public struct SerializableVector3
{
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }

    static public SerializableVector3 FromSerializable(Vector3 v) => new SerializableVector3() { x = v.x, y = v.y, z = v.z };

    static public SerializableVector3 FromSerializable(Quaternion q) => FromSerializable(q.eulerAngles);

    public Vector3 ToVector3() => new Vector3(x, y, z);

    public Quaternion ToQuaternion() => Quaternion.Euler(x, y, z);
}

[Serializable]
public class MeshData
{
    public string AssetBundlePath { get; set; }
    public string AssetPath { get; set; }
    public string MeshPath { get; set; }
    public bool DoCombine { get; set; }
}

[Serializable]
public class ParticleSystemData
{
    public string AssetBundlePath { get; set; }
    public string AssetPath { get; set; }
    public string ParticleSystemPath { get; set; }
}

[Serializable]
public class OpenCloseComponentData
{
    public string[] AnimComponentPaths { get; set; }
    public SerializableAnimState ClosedState { get; set; }
    public SerializableAnimState OpenState { get; set; }
    public SerializableAnimState[] AnimWaypoints { get; set; }

    public static OpenCloseComponentData FromOpenCloseAnim(GenericOpenCloseAnim openClose)
    {
        return new OpenCloseComponentData()
        {
            ClosedState = SerializableAnimState.FromAnimState(openClose.ClosedState),
            OpenState = SerializableAnimState.FromAnimState(openClose.OpenState),
            AnimWaypoints = openClose.AnimWaypoints.Select(w => SerializableAnimState.FromAnimState(w)).ToArray(),
            AnimComponentPaths = openClose.AnimComponents.Select(c => HierarchySerializer.TransformPath(c, openClose.transform)).ToArray()
        };
    }
}

[Serializable]
public class SerializableAnimState
{
    public SerializableVector3[] Positions { get; set; }
    public SerializableVector3[] Rotations { get; set; }
    public float Duration { get; set; }

    public static SerializableAnimState FromAnimState(GenericOpenCloseAnim.AnimState orig)
    {
        SerializableAnimState res = new SerializableAnimState()
        {
            Positions = orig.Positions.Select(p => SerializableVector3.FromSerializable(p)).ToArray(),
            Rotations = orig.Rotations.Select(r => SerializableVector3.FromSerializable(r)).ToArray(),
            Duration = orig.Duration
        };

        return res;
    }
}