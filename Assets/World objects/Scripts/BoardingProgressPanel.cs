using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BoardingProgressPanel : MonoBehaviour
{
    void Awake()
    {
        _phaseText = transform.Find("PhaseText").GetComponent<TextMeshProUGUI>();
        _progressText = transform.Find("ProgressText").GetComponent<TextMeshProUGUI>();
        _outerBar = transform.Find("BG bar").GetComponent<UnityEngine.UI.Image>();
        _innerBar = _outerBar.transform.Find("BoardingBar").GetComponent<UnityEngine.UI.Image>();
    }

    public void StartBreaching(Ship attacker, Ship defender)
    {
        CurrentPhase = BoardingPhase.Breaching;
        _attacker = attacker;
        _defender = defender;
        _phaseText.text = "Breaching...";
        UpdateBreaching(0);
    }

    public void UpdateBreaching(int progress)
    {
        float proportion = ((float)progress) / 100;
        _innerBar.rectTransform.localScale = new Vector3(proportion, 1, 1);
        _progressText.text = string.Format("{0}%", progress);
    }

    public void StartBoarding(int attackerForce, int defenderForce)
    {
        CurrentPhase = BoardingPhase.Breaching;
        _phaseText.text = "Boarding...";
        _innerBar.color = Color.red;
        _outerBar.color = Color.blue;
        UpdateBoarding(attackerForce, defenderForce);
    }

    public void UpdateBoarding(int attackerForce, int defenderForce)
    {
        float forcesSum = attackerForce + defenderForce;
        float proportion = ((float)attackerForce) / forcesSum;
        _innerBar.transform.localScale = new Vector3(proportion, 1, 1);
        _progressText.text = string.Format("{0}:{1}", attackerForce, defenderForce);
    }

    public BoardingPhase CurrentPhase { get; private set; }

    public enum BoardingPhase { Breaching, Boarding }

    private TextMeshProUGUI _phaseText;
    private TextMeshProUGUI _progressText;
    private UnityEngine.UI.Image _outerBar;
    private UnityEngine.UI.Image _innerBar;
    private Ship _attacker, _defender;
}
