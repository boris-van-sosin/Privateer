using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class HierarchyConstructionUtil
{
    public static GameObject ConstructHierarchy(HierarchyNode root, ObjectPrototypes proto)
    {
        int layer = ObjectFactory.DefaultLayer;
        return ConstructHierarchy(root, proto, layer, layer, layer);
    }

    public static GameObject ConstructHierarchy(HierarchyNode root, ObjectPrototypes proto, int generalLayer, int meshLayer, int particleSysLayer)
    {
        GameObject res = ConstructHierarchyRecursive(root, true, proto, generalLayer, meshLayer, particleSysLayer);
        FixTransformsRecursive(res.transform, root);
        SetOpenCloseAnims(res.transform, root);
        CombineMeshes(res.transform, root);
        return res;
    }

    private static GameObject ConstructHierarchyRecursive(HierarchyNode obj, bool setName, ObjectPrototypes proto, int generalLayer, int meshLayer, int particleSysLayer)
    {
        GameObject resObj;
        if (obj.NodeMesh != null)
        {
            resObj = proto.CreateObjectByPath(obj.NodeMesh.AssetBundlePath, obj.NodeMesh.AssetPath, obj.NodeMesh.MeshPath);
            resObj.layer = meshLayer;
        }
        else if (obj.NodeParticleSystem != null)
        {
            resObj = proto.CreateObjectByPath(obj.NodeParticleSystem.AssetBundlePath, obj.NodeParticleSystem.AssetPath, obj.NodeParticleSystem.ParticleSystemPath);
            resObj.layer = particleSysLayer;
        }
        else
        {
            resObj = proto.CreateObjectEmpty();
            resObj.layer = generalLayer;
        }

        if (setName)
        {
            resObj.name = obj.Name;
        }

        Transform tr = resObj.transform;
        foreach (HierarchyNode subNode in obj.SubNodes)
        {
            Transform subTr = ConstructHierarchyRecursive(subNode, true, proto, generalLayer, meshLayer, particleSysLayer).transform;
            subTr.SetParent(tr);
        }

        return resObj;
    }

    private static void FixTransformsRecursive(Transform currObj, HierarchyNode currData)
    {
        currObj.localPosition = currData.Position.ToVector3();
        currObj.localRotation = currData.Rotation.ToQuaternion();
        currObj.localScale = currData.Scale.ToVector3();
        if (currData.SubNodes == null || currObj.childCount != currData.SubNodes.Length)
        {
            return;
        }
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
            if (currData.SubNodes == null || currObj.childCount != currData.SubNodes.Length)
            {
                return;
            }
            for (int i = 0; i < currObj.childCount; ++i)
            {
                CombineMeshes(currObj.GetChild(i), currData.SubNodes[i]);
            }
        }
    }

    private static (Transform, IReadOnlyCollection<CombineInstance>, IReadOnlyCollection<Transform>) CombineMeshesRecursiveInner(Transform currObj, HierarchyNode currData, Matrix4x4 mat, Transform rootObj, HierarchyNode rootData)
    {
        bool isTopLevel = currObj == rootObj;
        Matrix4x4 localMat = isTopLevel ? Matrix4x4.identity : currData.Mat;
        List<CombineInstance> toCombine = null;
        List<Transform> toDelete = null;
        bool mergeCurr = currData.NodeMesh != null && currData.NodeMesh.DoCombine;
        if (mergeCurr)
        {
            toCombine = new List<CombineInstance>();
            toCombine.Add(new CombineInstance() { mesh = currObj.GetComponent<MeshFilter>().sharedMesh, transform = mat * localMat });
        }

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

    private static void SetOpenCloseAnims(Transform currObj, HierarchyNode currData)
    {
        if (currData.OpenCloseData != null)
        {
            GenericOpenCloseAnim openClose = currObj.gameObject.AddComponent<GenericOpenCloseAnim>();
            currData.OpenCloseData.SetOpenCloseAnim(openClose);
            openClose.AnimComponents = currData.OpenCloseData.AnimComponentPaths.Select(p => currObj.Find(p)).ToArray();
        }
        if (currData.SubNodes == null || currObj.childCount != currData.SubNodes.Length)
        {
            return;
        }
        for (int i = 0; i < currObj.childCount; ++i)
        {
            SetOpenCloseAnims(currObj.GetChild(i), currData.SubNodes[i]);
        }
    }
}
