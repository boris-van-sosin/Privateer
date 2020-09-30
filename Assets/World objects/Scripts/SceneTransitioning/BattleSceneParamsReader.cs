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
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator CreateShips(BattleSceneParams args)
    {
        yield return _waitFrame;

        Faction[] factions = FindObjectsOfType<Faction>();
        Faction faction1 = factions.Where(f => f.PlayerFaction).First(), faction2 = factions.Where(f => !f.PlayerFaction).First();

        int idx1 = 0, idx2 = 0;
        while (idx1 < args.Faction1Ships.Count && idx2 < args.Faction2Ships.Count)
        {
            if (idx1 < args.Faction1Ships.Count)
            {
                Vector3 offset = new Vector3(-(idx1 % 4), 0f, -(idx1 / 4)) * 6;
                Ship s = CombatSceneShipCreator.CreateAndFitOutShip(args.Faction1Ships[idx1], faction1, idx1 == 0 ? InputControl : null, offset);
                ++idx1;
                yield return _waitFrame;
            }
            if (idx2 < args.Faction2Ships.Count)
            {
                Vector3 offset = new Vector3(7 + (idx2 % 4), 0f, -(idx2 / 4)) * 6;
                Ship s = CombatSceneShipCreator.CreateAndFitOutShip(args.Faction2Ships[idx2], faction2, null, offset);
                ++idx2;
                yield return _waitFrame;
            }
        }
        yield return _waitFrame;
        Destroy(gameObject);
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

    private static readonly WaitForEndOfFrame _waitFrame = new WaitForEndOfFrame();
}
