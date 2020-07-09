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
            _value = value;
            if (_value)
            {
                _containerImg.color = OnColor;
            }
            else
            {
                _containerImg.color = OffColor;
            }
        }
    }

    private void Toggle()
    {
        Value = !Value;
    }

    public Image ButtonImage;
    public Color OnColor;
    public Color OffColor;
    private bool _value;
    public bool InitialValue;
    private Button _innerBtn;
    private Image _containerImg;
}
