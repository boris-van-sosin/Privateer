using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StrikeCraftCommandPanel : MonoBehaviour
{
    void Awake()
    {
        UpButton.onClick.AddListener(IncreaseFormations);
        DownButton.onClick.AddListener(DecreaseFormations);
    }

    public void Attach(CarrierBehavior carrier, string strikeCraftType)
    {
        _attachedCarrier = carrier;
        _strikeCraftType = strikeCraftType;

        if (_attachedCarrier != null)
        {
            _attachedCarrier.OnLaunchStart += CarrierEventWrapper;
            _attachedCarrier.OnLaunchFinish += CarrierEventWrapper;
            _attachedCarrier.OnRecoveryStart += CarrierEventWrapper;
            _attachedCarrier.OnRecoveryFinish += CarrierEventWrapper;
            _attachedCarrier.OnFormationRemoved += CarrierFormationEventWrapper;
        }
    }

    private void CarrierFormationEventWrapper(CarrierBehavior c, StrikeCraftFormation f)
    {
        UpdateCount();
    }

    private void CarrierEventWrapper(CarrierBehavior c)
    {
        UpdateCount();
    }

    private void UpdateCount()
    {
        NumText.text = string.Format(
            "{0}/{1}/{2}",
            _attachedCarrier.NumActiveFormationsOfType(_strikeCraftType),
            _attachedCarrier.MaxFormations,
            _attachedCarrier.AvailableCraft[_strikeCraftType]);
    }

    private void IncreaseFormations()
    {
        if (_attachedCarrier != null)
            _attachedCarrier.LaunchFormationOfType(_strikeCraftType);
    }

    private void DecreaseFormations()
    {
        if (_attachedCarrier != null)
            _attachedCarrier.StartRecallFormation(_strikeCraftType);
    }

    public Button UpButton;
    public Button DownButton;
    public TextMeshProUGUI NumText;

    private CarrierBehavior _attachedCarrier;
    private string _strikeCraftType;
}
