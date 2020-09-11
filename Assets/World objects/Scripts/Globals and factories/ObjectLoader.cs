using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ObjectLoader
{
    public GameObject CreateObjectByPath(string assetBundlePath, string assetPath, string objPath)
    {
        GameObject proto = GetObjectByPath(assetBundlePath, assetPath, objPath);
        if (null != proto)
        {
            return GameObject.Instantiate(proto);
        }
        else
        {
            Debug.LogWarningFormat("Requested asset does not exist: ({0},{1},{2})", assetBundlePath, assetPath, objPath);
            return null;
        }
    }

    public GameObject GetObjectByPath(string assetBundlePath, string assetPath, string objPath)
    {
        GameObject res;
        if (!_objCache.TryGetValue((assetBundlePath, assetPath, objPath), out res))
        {
            res = AssetBundleUtil.LoadSingleObject<GameObject>(assetBundlePath, assetPath, objPath, true);
            if (res == null)
            {
                return null;
            }
            _objCache[(assetBundlePath, assetPath, objPath)] = res;
        }
        return res;
    }

    public Texture2D GetImageByPath(string imgPath, int w, int h)
    {
        Texture2D res;
        if (_imgCache.TryGetValue(imgPath, out res))
        {
            return res;
        }
        if (!File.Exists(imgPath))
        {
            return null;
        }
        byte[] data = File.ReadAllBytes(imgPath);
        res = new Texture2D(w, h, TextureFormat.RGBA32, false);
        res.LoadImage(data, false);
        return res;
    }

    public GameObject CreateObjectEmpty()
    {
        return new GameObject();
    }

    private Dictionary<(string, string, string), GameObject> _objCache = new Dictionary<(string, string, string), GameObject>();
    private Dictionary<string, Texture2D> _imgCache = new Dictionary<string, Texture2D>();
}

