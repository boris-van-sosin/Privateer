using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class HierarchyConstructionUtil
{
    public static GameObject ConstructHierarchy(HierarchyNode root, ObjectPrototypes proto)
    {
        GameObject res = ConstructHierarchyRecursive(root, true, proto);
        FixTransformsRecursive(res.transform, root);
        CombineMeshes(res.transform, root);
        return res;
    }

    private static GameObject ConstructHierarchyRecursive(HierarchyNode obj, bool setName, ObjectPrototypes proto)
    {
        GameObject resObj;
        if (obj.NodeMesh != null)
        {
            resObj = proto.CreateObjectByPath(obj.NodeMesh.AssetBundlePath, obj.NodeMesh.AssetPath, obj.NodeMesh.MeshPath);
        }
        else if (obj.NodeParticleSystem != null)
        {
            resObj = proto.CreateObjectByPath(obj.NodeParticleSystem.AssetBundlePath, obj.NodeParticleSystem.AssetPath, obj.NodeParticleSystem.ParticleSystemPath);
        }
        else
        {
            resObj = proto.CreateObjectEmpty();
        }

        if (setName)
        {
            resObj.name = obj.Name;
        }

        Transform tr = resObj.transform;
        foreach (HierarchyNode subNode in obj.SubNodes)
        {
            Transform subTr = ConstructHierarchyRecursive(subNode, true, proto).transform;
            subTr.SetParent(tr);
        }

        return resObj;
    }

    private static void FixTransformsRecursive(Transform currObj, HierarchyNode currData)
    {
        currObj.localPosition = currData.Position.Vector3FromSerializable();
        currObj.localRotation = currData.Rotation.QuaternionFromSerializable();
        currObj.localScale = currData.Scale.Vector3FromSerializable();
        for (int i = 0; i < currObj.childCount; ++i)
        {
            FixTransformsRecursive(currObj.GetChild(i), currData.SubNodes[i]);
        }
    }

    private static void CombineMeshes(Transform currObj, HierarchyNode currData)
    {
        if (currData.NodeMesh != null)
        {
            CombineMeshesRecursiveInner(currObj, currData, Matrix4x4.identity, currObj, currData);
        }
        else
        {
            for (int i = 0; i < currObj.childCount; ++i)
            {
                CombineMeshes(currObj.GetChild(i), currData.SubNodes[i]);
            }
        }
    }

    private static (Transform, IReadOnlyCollection<CombineInstance>, IReadOnlyCollection<Transform>) CombineMeshesRecursiveInner(Transform currObj, HierarchyNode currData, Matrix4x4 mat, Transform rootObj, HierarchyNode rootData)
    {
        Matrix4x4 localMat = currData.Mat;
        List<CombineInstance> toCombine = null;
        List<Transform> toDelete = null;
        bool mergeCurr = currData.NodeMesh != null && currData.NodeMesh.DoCombine;
        if (mergeCurr)
        {
            toCombine = new List<CombineInstance>();
            toCombine.Add(new CombineInstance() { mesh = currObj.GetComponent<MeshFilter>().sharedMesh, transform = mat });
        }

        bool isTopLevel = currObj == rootObj;

        Matrix4x4 nextMat = (!isTopLevel && mergeCurr) ? mat * localMat : Matrix4x4.identity;

        for (int i = 0; i < Mathf.Min(currObj.childCount, currData.SubNodes.Length); ++i)
        {
            (Transform, IReadOnlyCollection<CombineInstance>, IReadOnlyCollection<Transform>) subRes =
                CombineMeshesRecursiveInner(currObj.GetChild(i), currData.SubNodes[i], nextMat, rootObj, rootData);
            if (mergeCurr)
            {
                if (subRes.Item2 != null)
                {
                    toCombine.AddRange(subRes.Item2);
                    if (toDelete == null)
                    {
                        toDelete = new List<Transform>();
                    }
                    toDelete.Add(subRes.Item1);
                    if (subRes.Item3 != null)
                    {
                        toDelete.AddRange(subRes.Item3);
                    }
                }
                else
                {
                    if (!isTopLevel)
                    {
                        KickUp(subRes.Item1, rootObj, rootData, true);
                    }
                }
            }
            else
            {
                // This is a non-merge node. Merge the current sub-tree.
                if (subRes.Item2 != null && subRes.Item2.Count > 1)
                {
                    MeshFilter mergingFilter = subRes.Item1.GetComponent<MeshFilter>();
                    mergingFilter.mesh = new Mesh();
                    mergingFilter.mesh.CombineMeshes(subRes.Item2.ToArray(), true, true);
                    mergingFilter.mesh.UploadMeshData(true);
                    if (subRes.Item3 != null)
                    {
                        foreach (Transform trToDelete in subRes.Item3)
                        {
                            GameObject.Destroy(trToDelete.gameObject);
                        }
                    }
                }
            }
        }

        if (mergeCurr && isTopLevel)
        {
            if (toCombine.Count > 1)
            {
                MeshFilter mergingFilter = currObj.GetComponent<MeshFilter>();
                mergingFilter.mesh = new Mesh();
                mergingFilter.mesh.CombineMeshes(toCombine.ToArray(), true, true);
                mergingFilter.mesh.UploadMeshData(true);
            }
            if (toDelete != null)
            {
                foreach (Transform trToDelete in toDelete)
                {
                    GameObject.Destroy(trToDelete.gameObject);
                }
            }
            return (currObj, null, null);
        }
        else if (mergeCurr && !isTopLevel)
        {
            return (currObj, toCombine, toDelete);
        }
        else
        {
            return (currObj, null, null);
        }
    }

    private static (bool, bool) KickUp(Transform toKickUp, Transform currObj, HierarchyNode currData, bool isTopLevel)
    {
        if (currObj == toKickUp)
        {
            return (true, true);
        }

        bool found = false;
        bool doPropagateUp = false;
        for (int i = 0; i < Mathf.Min(currObj.childCount, currData.SubNodes.Length); ++i)
        {
            (bool, bool) foundInSubtree = KickUp(toKickUp, currObj.GetChild(i), currData.SubNodes[i], false);
            found = foundInSubtree.Item1;
            doPropagateUp = foundInSubtree.Item2;
            if (found)
            {
                if (isTopLevel || (doPropagateUp && (currData.NodeMesh == null || !currData.NodeMesh.DoCombine)))
                {
                    toKickUp.SetParent(currObj);
                    return (true, false);
                }
                else
                {
                    return (true, doPropagateUp);
                }
            }
        }

        return (false, false);
    }
}
