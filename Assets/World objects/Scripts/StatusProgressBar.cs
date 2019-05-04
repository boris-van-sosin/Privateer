using System;

using UnityEngine;

public class StatusProgressBar : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        _image = GetComponent<UnityEngine.UI.Image>();
        DetachFunction();
    }

    // Update is called once per frame
    void Update()
    {
        _image.fillAmount = _progressFunction();
    }

    public void AttachFunction(Func<float> f)
    {
        _progressFunction = f;
    }
    public void DetachFunction()
    {
        _progressFunction = Zero;
    }

    public void SetColor(Color c)
    {
        _image.color = c;
    }

    private float Zero()
    {
        return 0f;
    }

    private UnityEngine.UI.Image _image;
    private Func<float> _progressFunction;
}
