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
        UpdateHitPointsDisplay();
    }

    public void SetImage(Sprite s)
    {
        _image.overrideSprite = s;
    }

    void OnDestroy()
    {
        _attachedComponent.OnHitpointsChanged -= UpdateHitPointsDisplay;
    }

    private void UpdateHitPointsDisplay()
    {
        _image.color = ColorGradient.Evaluate(((float)_attachedComponent.ComponentHitPoints) / _attachedComponent.ComponentMaxHitPoints);
    }

    private IShipActiveComponent _attachedComponent;
    private UnityEngine.UI.Image _image;
    public Gradient ColorGradient;
}
