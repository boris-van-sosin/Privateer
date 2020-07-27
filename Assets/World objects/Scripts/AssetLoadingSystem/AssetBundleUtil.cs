using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AssetBundleUtil
{
    static public T LoadAsset<T>(string assetBundlePath, string assetPath) where T : UnityEngine.Object
    {
        AssetBundle currBundle = GetAssetBundle(assetBundlePath);
        if (currBundle == null)
        {
            return null;
        }

        GameObject resObj = currBundle.LoadAsset<GameObject>(assetPath);
        if (typeof(T) == typeof(GameObject))
        {
            return resObj as T;
        }

        T res;
        if ((res = resObj.GetComponent<T>()) != null)
        {
            return GameObject.Instantiate<T>(res);
        }
        else
        {
            return null;
        }
    }

    static public T LoadSingleObject<T>(string assetBundlePath, string assetPath, string objectPath) where T : UnityEngine.Object
    {
        return LoadSingleObject<T>(assetBundlePath, assetPath, objectPath, false);
    }
    static public T LoadSingleObject<T>(string assetBundlePath, string assetPath, string objectPath, bool protorypeOnly) where T : UnityEngine.Object
    {
        AssetBundle currBundle = GetAssetBundle(assetBundlePath);
        if (currBundle == null)
        {
            return null;
        }

        GameObject rootObj = currBundle.LoadAsset<GameObject>(assetPath);
        if (rootObj == null)
        {
            return null;
        }
        Transform resTransform = rootObj.transform.Find(objectPath);
        if (resTransform == null)
        {
            return null;
        }

        if (typeof(T) == typeof(GameObject))
        {
            if (protorypeOnly)
            {
                return resTransform.gameObject as T;
            }
            else
            {
                return GameObject.Instantiate<GameObject>(resTransform.gameObject) as T;
            }
        }
        else
        {
            T res;
            if ((res = resTransform.GetComponent<T>()) != null)
            {
                if (protorypeOnly)
                {
                    return res;
                }
                else
                {
                    return GameObject.Instantiate<T>(res);
                }
            }
            else
            {
                return null;
            }
        }
    }

    static private AssetBundle GetAssetBundle(string path)
    {
        AssetBundle currBundle;
        if (!_assetBundleCache.TryGetValue(path, out currBundle))
        {
            currBundle = AssetBundle.LoadFromFile(path);
            if (currBundle == null)
            {
                return null;
            }
            _assetBundleCache[path] = currBundle;
        }
        return currBundle;
    }

    private static Dictionary<string, AssetBundle> _assetBundleCache = new Dictionary<string, AssetBundle>();
}
