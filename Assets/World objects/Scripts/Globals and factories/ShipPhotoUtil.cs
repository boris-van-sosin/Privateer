using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ShipPhotoUtil
{
    public static Sprite TakePhoto(Ship s, int ImgH, int ImgW)
    {
        return TakePhotoInner(s, ImgH, ImgW, false).Item1;
    }

    public static ValueTuple<Sprite, IEnumerable<ValueTuple<TurretHardpoint, Vector3>>> TakePhotoWithTurretPos(Ship s, int ImgH, int ImgW)
    {
        return TakePhotoInner(s, ImgH, ImgW, true);
    }

    private static ValueTuple<Sprite, IEnumerable<ValueTuple<TurretHardpoint, Vector3>>> TakePhotoInner(Ship s, int ImgH, int ImgW, bool withTurretPos)
    {
        float scaleFactor = 1.2f;
        float requiredSize = scaleFactor * Mathf.Max(s.ShipLength, s.ShipWidth);
        Vector3 shipAxis = s.transform.up;
        Vector3 downDir = Vector3.down;
        Camera cam = ObjectFactory.GetShipStatusPanelCamera();
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

    private static float ShipLenCenter(ShipBase sb)
    {
        Mesh m = sb.HullObject.GetComponent<MeshFilter>().mesh;
        float localCenter = (m.bounds.max.y + m.bounds.min.y) / 2.0f;
        return localCenter * sb.HullObject.transform.lossyScale.y;
    }
}
