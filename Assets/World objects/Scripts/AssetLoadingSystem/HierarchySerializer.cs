using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeResolvers;

public static class HierarchySerializer
{
    static public string SerializeObject(Transform root)
    {
        System.Object toSerialize = null;
        Type targetType = null;

        HierarchyNode hierarchy = null;
        TurretBase turret;
        ShipBase ship;
        if ((turret = root.GetComponent<TurretBase>()) != null)
        {
            TurretDefinition turretDef = new TurretDefinition()
            {
                Geometry = root.ToSerializableHierarchy(),
                TurretAxis = turret.TurretAxis,
                TurretType = turret.TurretType,
                WeaponSize = turret.TurretSize,
                WeaponType = turret.TurretWeaponType
            };
            toSerialize = turretDef;
            targetType = typeof(TurretDefinition);
        }
        else
        {
            hierarchy = root.ToSerializableHierarchy();
        }

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
            Position = SerializableVector3.ToSerializable(t.localPosition),
            Rotation = SerializableVector3.ToSerializable(t.localRotation),
            Scale = SerializableVector3.ToSerializable(t.localScale),
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
            res.OpenCloseData = new OpenCloseComponentData();
            res.OpenCloseData.ClosedState = openClose.ClosedState;
            res.OpenCloseData.OpenState = openClose.OpenState;
            res.OpenCloseData.AnimWaypoints = new GenericOpenCloseAnim.AnimState[openClose.AnimWaypoints.Length];
            openClose.AnimWaypoints.CopyTo(res.OpenCloseData.AnimWaypoints, 0);
            res.OpenCloseData.AnimComponentPaths = openClose.AnimComponents.Select(c => TransformPath(c, t)).ToArray();
        }

        res.Name = t.name;
        res.Position = SerializableVector3.ToSerializable(t.localPosition);
        res.Rotation = SerializableVector3.ToSerializable(t.localRotation);
        res.Scale = SerializableVector3.ToSerializable(t.localScale);

        List<HierarchyNode> subNodes = new List<HierarchyNode>();
        for (int i = 0; i < t.childCount; i++)
        {
            subNodes.Add(t.GetChild(i).ToSerializableHierarchy());
        }
        res.SubNodes = subNodes.Where(n => n != null).ToArray();

        return res;
    }

    static private string TransformPath(Transform t, Transform upTo)
    {
        if (t == null)
            return string.Empty;
        else if (t.parent == null || t.parent == upTo)
            return t.name;
        else
            return string.Format("{0}/{1}", TransformPath(t.parent, upTo), t.name);
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
    public Matrix4x4 Mat => Matrix4x4.TRS(Position.Vector3FromSerializable(), Rotation.QuaternionFromSerializable(), Scale.Vector3FromSerializable());
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

    static public SerializableVector3 ToSerializable(Vector3 v) => new SerializableVector3() { x = v.x, y = v.y, z = v.z };

    static public SerializableVector3 ToSerializable(Quaternion q) => ToSerializable(q.eulerAngles);

    public Vector3 Vector3FromSerializable() => new Vector3(x, y, z);

    public Quaternion QuaternionFromSerializable() => Quaternion.Euler(x, y, z);
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
    public GenericOpenCloseAnim.AnimState OpenState { get; set; }
    public GenericOpenCloseAnim.AnimState ClosedState { get; set; }
    public GenericOpenCloseAnim.AnimState[] AnimWaypoints { get; set; }
}
