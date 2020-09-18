using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class OnOffButton : MonoBehaviour
{
    protected virtual void Awake()
    {
        _innerBtn = GetComponent<Button>();
        _innerBtn.onClick.AddListener(Toggle);
        Value = InitialValue;
    }

    public virtual bool Value
    {
        get
        {
            return _value;
        }
        set
        {
            bool prevValue = _value;
            _value = value;
            if (TargetGraphic != null)
            {
                if (_value)
                {
                    TargetGraphic.color = OnColor;
                }
                else
                {
                    TargetGraphic.color = OffColor;
                }
            }

            if (_value != prevValue)
            {
                onValueChanged?.Invoke(_value);
            }
        }
    }

    private void Toggle()
    {
        Value = !Value;
        onValueChangedViaClick?.Invoke(Value);
    }

    public Color OnColor;
    public Color OffColor;
    private bool _value;
    public bool InitialValue;
    private Button _innerBtn;
    public Graphic TargetGraphic;
    public event Action<bool> onValueChanged;
    public event Action<bool> onValueChangedViaClick;
}
