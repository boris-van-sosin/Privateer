using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectedShipCard : MonoBehaviour, ICollapsable
{
    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        CardImage.onClick.AddListener(ImageClicked);
        _closedWidth = _openWidth = CardImage.GetComponent<RectTransform>().sizeDelta.x;

        _commandPanelImages = CommandPanel.GetComponentsInChildren<MaskableGraphic>(true).Select(x => new Tuple<MaskableGraphic, float>(x, x.color.a)).ToArray();
        _openWidth += CommandPanel.GetComponent<RectTransform>().sizeDelta.x;
        CommandPanel.gameObject.SetActive(false);
        foreach (Tuple<MaskableGraphic, float> img in _commandPanelImages)
        {
            img.Item1.color =
                new Color(img.Item1.color.r,
                            img.Item1.color.g,
                            img.Item1.color.b,
                            0f);

        }

        _figterPanelImages = FighterPanel.GetComponentsInChildren<MaskableGraphic>(true).Select(x => new Tuple<MaskableGraphic, float>(x, x.color.a)).ToArray();
        _openWidth += FighterPanel.GetComponent<RectTransform>().sizeDelta.x;
        FighterPanel.gameObject.SetActive(false);
        foreach (Tuple<MaskableGraphic, float> img in _figterPanelImages)
        {
            img.Item1.color =
                new Color(img.Item1.color.r,
                            img.Item1.color.g,
                            img.Item1.color.b,
                            0f);

        }

        _bomberPanelImages = BomberPanel.GetComponentsInChildren<MaskableGraphic>(true).Select(x => new Tuple<MaskableGraphic, float>(x, x.color.a)).ToArray();
        _openWidth += BomberPanel.GetComponent<RectTransform>().sizeDelta.x;
        BomberPanel.gameObject.SetActive(false);
        foreach (Tuple<MaskableGraphic, float> img in _bomberPanelImages)
        {
            img.Item1.color =
                new Color(img.Item1.color.r,
                            img.Item1.color.g,
                            img.Item1.color.b,
                            0f);

        }

        AIButton.onValueChangedViaClick += AIButtonClicked;

        _fighterPanelEnabled = _bomberPanelEnabled = true;
        _open = false;
        _currPhase = 0f;
        _rt.sizeDelta = new Vector2(_closedWidth, _rt.sizeDelta.y);
    }

    public void AttachShip(Ship s)
    {
        _controlledShip = s;

        // Start closed:
        CommandPanel.gameObject.SetActive(false);
        FighterPanel.gameObject.SetActive(false);
        BomberPanel.gameObject.SetActive(false);
        _open = false;
        _currPhase = 0f;
        _rt.sizeDelta = new Vector2(_closedWidth, _rt.sizeDelta.y);

        _openWidth = _closedWidth + CommandPanel.GetComponent<RectTransform>().sizeDelta.x;
        if ((_controlledCarrierModule = s.GetComponent<CarrierBehavior>()) != null)
        {
            _openWidth += FighterPanel.GetComponent<RectTransform>().sizeDelta.x;
            _openWidth += BomberPanel.GetComponent<RectTransform>().sizeDelta.x;
            FighterPanel.Attach(_controlledCarrierModule, "Fed Fighter");
            BomberPanel.Attach(_controlledCarrierModule, "Fed Torpedo Bomber");
        }
        _fighterPanelEnabled = _bomberPanelEnabled = (_controlledCarrierModule != null);

        ShipNameBox.text = s.DisplayName.ShortName;

        Image img = CardImage.GetComponentsInChildren<Image>(false).Where(x => x.gameObject != CardImage.gameObject).First();
        img.sprite = ObjectFactory.GetShipPhoto(s);

        _controlledShip = s;
        _controlledShipAI = s.GetComponent<ShipAIHandle>();
        AIButton.Value = _controlledShipAI.GetControlType() == ShipControlType.Autonomous;
    }

    private void ImageClicked()
    {
        if (_open)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    private IEnumerator SlowOpenClose(bool open)
    {
        float StartTime = Time.time;
        if (open)
        {
            if (CommandPanel != null)
                CommandPanel.gameObject.SetActive(true);
            if (_fighterPanelEnabled && FighterPanel != null)
                FighterPanel.gameObject.SetActive(true);
            if (_bomberPanelEnabled && BomberPanel != null)
                BomberPanel.gameObject.SetActive(true);
        }

        while (true)
        {
            yield return _wait;
            _currPhase = (Time.time - StartTime) / _animTime;
            if (!open)
            {
                _currPhase = 1f - _currPhase;
            }
            bool finished = (open && _currPhase > 1f) || (!open && _currPhase < 0f);
            _currPhase = Mathf.Clamp(_currPhase, 0f, 1f);

            float widthPhase = Mathf.Clamp(_currPhase, 0f, 0.5f) * 2f;
            float width = Mathf.Lerp(_closedWidth, _openWidth, widthPhase);
            _rt.sizeDelta = new Vector2(width, _rt.sizeDelta.y);

            float fadePhase = Mathf.Clamp(_currPhase - 0.5f, 0f, 0.5f) * 2f;
            if (_commandPanelImages != null)
            {
                foreach (Tuple<MaskableGraphic, float> img in _commandPanelImages)
                {
                    img.Item1.color =
                        new Color(img.Item1.color.r,
                                    img.Item1.color.g,
                                    img.Item1.color.b,
                                    img.Item2 * fadePhase);

                }
            }
            if (_fighterPanelEnabled && _figterPanelImages != null)
            {
                foreach (Tuple<MaskableGraphic, float> img in _figterPanelImages)
                {
                    img.Item1.color =
                        new Color(img.Item1.color.r,
                                    img.Item1.color.g,
                                    img.Item1.color.b,
                                    img.Item2 * fadePhase);

                }
            }
            if (_bomberPanelEnabled && _bomberPanelImages != null)
            {
                foreach (Tuple<MaskableGraphic, float> img in _bomberPanelImages)
                {
                    img.Item1.color =
                        new Color(img.Item1.color.r,
                                    img.Item1.color.g,
                                    img.Item1.color.b,
                                    img.Item2 * fadePhase);

                }
            }

            if (finished)
            {
                if (!open)
                {
                    if (CommandPanel != null)
                        CommandPanel.gameObject.SetActive(false);
                    if (_fighterPanelEnabled && FighterPanel != null)
                        FighterPanel.gameObject.SetActive(false);
                    if (_bomberPanelEnabled && BomberPanel != null)
                        BomberPanel.gameObject.SetActive(false);
                }
                break;
            }
        }
        _openCloseCoroutine = null;
    }

    private void AIButtonClicked(bool val)
    {
        ShipControlType currControlType = _controlledShipAI.GetControlType();
        if (val && currControlType == ShipControlType.SemiAutonomous)
        {
            _controlledShipAI.SetControlType(ShipControlType.Autonomous);
        }
        else if (!val && currControlType == ShipControlType.Autonomous)
        {
            _controlledShipAI.SetControlType(ShipControlType.SemiAutonomous);
        }
        else if (val && currControlType == ShipControlType.Manual)
        {
            _controlledShipAI.SetControlType(ShipControlType.Autonomous);
        }
    }

    public void Open()
    {
        if (_open)
            return;
        _open = true;
        InnerOpenClose();
        onOpened?.Invoke(this);
    }

    public void Close()
    {
        if (!_open)
            return;
        _open = false;
        InnerOpenClose();
        onClosed?.Invoke(this);
    }

    private void InnerOpenClose()
    {
        if (_openCloseCoroutine != null)
        {
            StopCoroutine(_openCloseCoroutine);
        }
        _openCloseCoroutine = StartCoroutine(SlowOpenClose(_open));
    }

    public TextMeshProUGUI ShipNameBox;
    public Button CardImage;
    public RectTransform CommandPanel;
    public StrikeCraftCommandPanel FighterPanel;
    public StrikeCraftCommandPanel BomberPanel;
    private bool _fighterPanelEnabled;
    private bool _bomberPanelEnabled;

    private Tuple<MaskableGraphic, float>[] _commandPanelImages;
    private Tuple<MaskableGraphic, float>[] _figterPanelImages;
    private Tuple<MaskableGraphic, float>[] _bomberPanelImages;

    private Ship _controlledShip;
    private ShipAIHandle _controlledShipAI;
    private CarrierBehavior _controlledCarrierModule;

    public OnOffButton AIButton;

    private bool _open;
    private float _closedWidth;
    private float _openWidth;
    private static float _animTime = 0.2f;
    private float _currPhase;
    private RectTransform _rt;
    private static WaitForEndOfFrame _wait = new WaitForEndOfFrame();
    private Coroutine _openCloseCoroutine = null;

    public event Action<ICollapsable> onOpened;
    public event Action<ICollapsable> onClosed;
}
