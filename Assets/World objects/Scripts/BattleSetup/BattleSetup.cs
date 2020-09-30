using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleSetup : MonoBehaviour
{
    public void StartBattle()
    {
        BattleSceneParamsReader.BattleSceneParams args = new BattleSceneParamsReader.BattleSceneParams(PlayerFleetSetup.SelectedFleet, EnemyFleetSetup.SelectedFleet);
        SceneStack.CallScene("TmpScene", args);
    }

    public FleetSetupPanel PlayerFleetSetup;
    public FleetSetupPanel EnemyFleetSetup;
}
