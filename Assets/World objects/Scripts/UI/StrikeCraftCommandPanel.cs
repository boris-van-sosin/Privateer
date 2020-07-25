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
            _attachedCarrier.onLaunchStart += CarrierEventWrapper;
            _attachedCarrier.onLaunchFinish += CarrierEventWrapper;
            _attachedCarrier.onRecoveryStart += CarrierEventWrapper;
            _attachedCarrier.onRecoveryFinish += CarrierEventWrapper;
            _attachedCarrier.onFormationRemoved += CarrierFormationEventWrapper;
            LockButton.onValueChangedViaClick += ToggleLock;
            LockButton.Value = _attachedCarrier.LockFormationNumGet(strikeCraftType);
            UpdateCount();
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
            "{0}/{1}\n{2}",
            _attachedCarrier.NumActiveFormationsOfType(_strikeCraftType),
            _attachedCarrier.MaxFormations,
            _attachedCarrier.AvailableCraft[_strikeCraftType]);
    }

    private void IncreaseFormations()
    {
        _attachedCarrier?.QueueLaunchFormationOfType(_strikeCraftType);
    }

    private void DecreaseFormations()
    {
        _attachedCarrier?.QueueRecallFormationOfType(_strikeCraftType);
    }

    private void ToggleLock(bool val)
    {
        _attachedCarrier?.LockFormationNumSet(_strikeCraftType, val);
    }

    public Button UpButton;
    public Button DownButton;
    public OnOffButton LockButton;
    public TextMeshProUGUI NumText;

    private CarrierBehavior _attachedCarrier;
    private string _strikeCraftType;
}
