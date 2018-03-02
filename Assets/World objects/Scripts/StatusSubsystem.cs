using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusSubsystem : MonoBehaviour
{
    void Awake()
    {
        _image = GetComponent<UnityEngine.UI.Image>();
    }

    public void AttachComponent(IShipActiveComponent comp)
    {
        _attachedComponent = comp;
        _attachedComponent.OnHitpointsChanged += UpdateHitPointsDisplay;
    }

    public void SetImage(Sprite s)
    {
        _image.sprite = s;
    }

    void OnDestroy()
    {
        _attachedComponent.OnHitpointsChanged -= UpdateHitPointsDisplay;
    }

    private void UpdateHitPointsDisplay()
    {
        _image.color = _colorGradient.Evaluate(((float)_attachedComponent.ComponentHitPoints) / _attachedComponent.ComponentMaxHitPoints);
    }

    private IShipActiveComponent _attachedComponent;
    private UnityEngine.UI.Image _image;
    private static readonly Gradient _colorGradient = new Gradient()
    {
        colorKeys = new GradientColorKey[]
        {
            new GradientColorKey(Color.black, 0.0f),
            new GradientColorKey(Color.red, 0.1f),
            new GradientColorKey(new Color(249.0f/255.0f, 170.0f/255.0f, 6.0f/255.0f), 0.25f),
            new GradientColorKey(Color.yellow, 0.5f),
            new GradientColorKey(Color.green, 1f)
        },
        mode = GradientMode.Blend
    };
}
