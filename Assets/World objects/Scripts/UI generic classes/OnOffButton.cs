using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class OnOffButton : MonoBehaviour
{
    void Awake()
    {
        _innerBtn = GetComponent<Button>();
        _innerBtn.onClick.AddListener(Toggle);
        _containerImg = _innerBtn.GetComponent<Image>();
        Value = InitialValue;
    }

    public bool Value
    {
        get
        {
            return _value;
        }
        set
        {
            bool prevValue = _value;
            _value = value;
            if (_containerImg != null)
            {
                if (_value)
                {
                    _containerImg.color = OnColor;
                }
                else
                {
                    _containerImg.color = OffColor;
                }
            }
            else
            {
                InitialValue = value;
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

    public Image ButtonImage;
    public Color OnColor;
    public Color OffColor;
    private bool _value;
    public bool InitialValue;
    private Button _innerBtn;
    private Image _containerImg;
    public event Action<bool> onValueChanged;
    public event Action<bool> onValueChangedViaClick;
}
