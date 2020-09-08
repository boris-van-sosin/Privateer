using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ShipPhotoUtil
{
    public static Sprite TakePhoto(Ship s, int ImgH, int ImgW)
    {
        return TakePhotoInner(s, ImgH, ImgW, false, ObjectFactory.GetShipStatusPanelCamera()).Item1;
    }

    public static ValueTuple<Sprite, IEnumerable<ValueTuple<TurretHardpoint, Vector3>>> TakePhotoWithTurretPos(Ship s, int ImgH, int ImgW)
    {
        return TakePhotoInner(s, ImgH, ImgW, true, ObjectFactory.GetShipStatusPanelCamera());
    }

    public static Sprite TakePhoto(Ship s, int ImgH, int ImgW, Camera cam)
    {
        return TakePhotoInner(s, ImgH, ImgW, false, cam).Item1;
    }

    public static ValueTuple<Sprite, IEnumerable<ValueTuple<TurretHardpoint, Vector3>>> TakePhotoWithTurretPos(Ship s, int ImgH, int ImgW, Camera cam)
    {
        return TakePhotoInner(s, ImgH, ImgW, true, cam);
    }

    private static ValueTuple<Sprite, IEnumerable<ValueTuple<TurretHardpoint, Vector3>>> TakePhotoInner(Ship s, int ImgH, int ImgW, bool withTurretPos, Camera cam)
    {
        float scaleFactor = 1.2f;
        float requiredSize = scaleFactor * Mathf.Max(s.ShipLength, s.ShipWidth);
        Vector3 shipAxis = s.transform.up;
        Vector3 downDir = Vector3.down;
        cam.transform.position = s.transform.position + (s.transform.up * ShipLenCenter(s));
        float height = requiredSize * 0.5f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        cam.transform.position += new Vector3(0, height, 0);
        cam.transform.rotation = Quaternion.LookRotation(downDir, shipAxis);

        Texture2D shipImg;
        shipImg = new Texture2D(ImgW, ImgH);
        RenderTexture rt = RenderTexture.GetTemporary(ImgW, ImgH);
        cam.enabled = true;
        cam.targetTexture = rt;
        RenderTexture orig = RenderTexture.active;
        RenderTexture.active = rt;
        cam.Render();
        shipImg.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        shipImg.Apply();

        IEnumerable<ValueTuple<TurretHardpoint, Vector3>> turretInfo;
        if (withTurretPos)
        {
            turretInfo = s.WeaponHardpoints.Select(hp => (hp, cam.WorldToViewportPoint(hp.transform.position))).ToArray();
        }
        else
        {
            turretInfo = null;
        }

        RenderTexture.active = orig;
        RenderTexture.ReleaseTemporary(rt);
        cam.enabled = false;

        Sprite sp = Sprite.Create(shipImg, new Rect(0, 0, ImgW, ImgH), new Vector2(0, 0));
        return (sp, turretInfo);
    }

    public static Sprite TakeObjectPhoto(Transform t, int ImgH, int ImgW, Camera cam)
    {
        return TakeObjectPhotoInner(t, ImgH, ImgW, false, cam).Item1;
    }

    public static ValueTuple<Sprite, IEnumerable<ValueTuple<TurretHardpoint, Vector3>>> TakeObjectPhotoWithTurretPos(Transform t, int ImgH, int ImgW, Camera cam)
    {
        return TakeObjectPhotoInner(t, ImgH, ImgW, true, cam);
    }

    private static ValueTuple<Sprite, IEnumerable<ValueTuple<TurretHardpoint, Vector3>>> TakeObjectPhotoInner(Transform t, int ImgH, int ImgW, bool withTurretPos, Camera cam)
    {
        float scaleFactor = 1.2f;
        (Vector2, Vector2) objBoundsData = ObjBoundsData(t);
        float requiredSize = scaleFactor * Mathf.Max(objBoundsData.Item2.x, objBoundsData.Item2.y);
        Vector3 shipAxis = t.up;
        Vector3 downDir = Vector3.down;
        cam.transform.position = t.position + (t.up * objBoundsData.Item1.y);
        float height = requiredSize * 0.5f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        cam.transform.position += new Vector3(0, height, 0);
        cam.transform.rotation = Quaternion.LookRotation(downDir, shipAxis);

        Texture2D shipImg;
        shipImg = new Texture2D(ImgW, ImgH);
        RenderTexture rt = RenderTexture.GetTemporary(ImgW, ImgH);
        cam.enabled = true;
        cam.targetTexture = rt;
        RenderTexture orig = RenderTexture.active;
        RenderTexture.active = rt;
        cam.Render();
        shipImg.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        shipImg.Apply();

        IEnumerable<ValueTuple<TurretHardpoint, Vector3>> turretInfo;
        if (withTurretPos)
        {
            turretInfo = t.GetComponentsInChildren<TurretHardpoint>().Select(hp => (hp, cam.WorldToViewportPoint(hp.transform.position))).ToArray();
        }
        else
        {
            turretInfo = null;
        }

        RenderTexture.active = orig;
        RenderTexture.ReleaseTemporary(rt);
        cam.enabled = false;

        Sprite sp = Sprite.Create(shipImg, new Rect(0, 0, ImgW, ImgH), new Vector2(0, 0));
        return (sp, turretInfo);
    }

    private static float ShipLenCenter(ShipBase sb)
    {
        Mesh m = sb.HullObject.GetComponent<MeshFilter>().mesh;
        float localCenter = (m.bounds.max.y + m.bounds.min.y) / 2.0f;
        return localCenter * sb.HullObject.transform.lossyScale.y;
    }

    private static (Vector2, Vector2) ObjBoundsData(Transform t)
    {
        MeshFilter[] meshes = t.GetComponentsInChildren<MeshFilter>();
        float minX = meshes[0].mesh.bounds.min.x * meshes[0].transform.lossyScale.x,
              maxX = meshes[0].mesh.bounds.max.x * meshes[0].transform.lossyScale.x,
              minY = meshes[0].mesh.bounds.min.y * meshes[0].transform.lossyScale.y,
              maxY = meshes[0].mesh.bounds.max.y * meshes[0].transform.lossyScale.y;
        for (int i = 1; i < meshes.Length; ++i)
        {
            minX = Mathf.Min(minX, meshes[i].mesh.bounds.min.x * meshes[0].transform.lossyScale.x);
            maxX = Mathf.Max(maxX, meshes[i].mesh.bounds.max.x * meshes[0].transform.lossyScale.x);
            minY = Mathf.Min(minY, meshes[i].mesh.bounds.min.y * meshes[0].transform.lossyScale.y);
            maxY = Mathf.Max(maxY, meshes[i].mesh.bounds.max.y * meshes[0].transform.lossyScale.y);
        }
        Vector2 localCenter = new Vector2(minX + maxX, minY + maxY) / 2.0f;
        Vector2 widthHeight = new Vector2(maxX - minX, maxY - minY);
        return (localCenter, widthHeight);
    }
}
