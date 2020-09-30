using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleSceneParamsReader : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        BattleSceneParams args = SceneStack.GetSceneParams<BattleSceneParams>();
        if (args != null)
        {
            StartCoroutine(CreateShips(args));
        }
    }

    private IEnumerator CreateShips(BattleSceneParams args)
    {
        yield return _waitFrame;

        Faction[] factions = FindObjectsOfType<Faction>();
        Faction faction1 = factions.Where(f => f.PlayerFaction).First(), faction2 = factions.Where(f => !f.PlayerFaction).First();

        _faction1Ships = new (Ship, bool, string)[args.Faction1Ships.Count];
        _faction2Ships = new (Ship, bool, string)[args.Faction2Ships.Count];
        int idx1 = 0, idx2 = 0;
        while (idx1 < args.Faction1Ships.Count || idx2 < args.Faction2Ships.Count)
        {
            if (idx1 < args.Faction1Ships.Count)
            {
                Vector3 offset = new Vector3(-(idx1 % 4), 0f, -(idx1 / 4)) * 6;
                Ship s = CombatSceneShipCreator.CreateAndFitOutShip(args.Faction1Ships[idx1], faction1, idx1 == 0 ? InputControl : null, offset);
                s.OnShipDisabled += UpdateShipStatus;
                _faction1Ships[idx1] = (s, true, args.Faction1Ships[idx1].ShipSpriteKey);
                ++idx1;
                yield return _waitFrame;
            }

            if (idx2 < args.Faction2Ships.Count)
            {
                Vector3 offset = new Vector3(7 + (idx2 % 4), 0f, -(idx2 / 4)) * 6;
                Ship s = CombatSceneShipCreator.CreateAndFitOutShip(args.Faction2Ships[idx2], faction2, null, offset);
                s.OnShipDisabled += UpdateShipStatus;
                _faction2Ships[idx2] = (s, true, args.Faction2Ships[idx2].ShipSpriteKey);
                ++idx2;
                yield return _waitFrame;
            }
        }
    }

    private void UpdateShipStatus(Ship s)
    {
        bool lost1 = CheckFactionLost(_faction1Ships, s);
        bool lost2 = CheckFactionLost(_faction2Ships, s);
        if (lost1 || lost2)
        {
            BattleFinished();
        }
    }

    private static bool CheckFactionLost((Ship, bool, string)[] factionShips, Ship disabledShip)
    {
        bool allDisabled = true;
        for (int i = 0; i < factionShips.Length; ++i)
        {
            if (null != disabledShip && factionShips[i].Item1 == disabledShip)
            {
                factionShips[i] = (disabledShip, false, factionShips[i].Item3);
            }

            if (factionShips[i].Item2)
            {
                allDisabled = false;
                if (null == disabledShip)
                {
                    return false;
                }
            }
        }
        return allDisabled;
    }

    private void BattleFinished()
    {
        StartCoroutine(BattleFinished(3f));
    }

    private IEnumerator BattleFinished(float delay)
    {
        if (!_battleFinished)
        {
            _battleFinished = true;
            yield return new WaitForSecondsRealtime(delay);
            OutcomePanel.SetOutcome(_faction1Ships.Select(s => (s.Item1, s.Item3)), _faction2Ships.Select(s => (s.Item1, s.Item3)));
            OutcomePanel.gameObject.SetActive(true);
            yield return null;
        }
    }

    public class BattleSceneParams : ISceneParams
    {
        public BattleSceneParams(IEnumerable<ShipShadow> faction1Fleet, IEnumerable<ShipShadow> faction2Fleet)
        {
            CalledFromScene = SceneManager.GetActiveScene().name;
            Faction1Ships = new List<ShipShadow>(faction1Fleet);
            Faction2Ships = new List<ShipShadow>(faction2Fleet);
        }

        public string CalledFromScene { get; set; }

        public List<ShipShadow> Faction1Ships { get; set; }
        public List<ShipShadow> Faction2Ships { get; set; }
    }

    public UserInput InputControl;
    public BattleOutcomePanel OutcomePanel;
    private (Ship, bool, string)[] _faction1Ships;
    private (Ship, bool, string)[] _faction2Ships;
    private bool _battleFinished = false;

    private static readonly WaitForEndOfFrame _waitFrame = new WaitForEndOfFrame();
}
