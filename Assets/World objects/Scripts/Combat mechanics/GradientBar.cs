using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GradientBar : MonoBehaviour
{
    void Awake()
    {
        _barMask = GetComponentInChildren<Mask>();
        _valueText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void UpdateBar()
    {
        float proprtion = (MaxValue != 0) ? ((float)Value) / MaxValue : 0;
        _valueText.text = string.Format("{0}/{1}", Value, MaxValue);
        _barMask.rectTransform.localScale = new Vector3(1, proprtion, 1);
    }

    public int Value
    {
        get
        {
            return _value;
        }
        set
        {
            _value = value;
            UpdateBar();
        }
    }
    private int _value;
    public int MaxValue;
    Mask _barMask;
    TextMeshProUGUI _valueText;
}
