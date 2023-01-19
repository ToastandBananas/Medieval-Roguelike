using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [HideInInspector] public List<Unit> npcs = new List<Unit>();
    [HideInInspector] public int npcsFinishedTakingTurnCount;

    Unit activeUnit;

    #region Singleton
    public static TurnManager Instance;

    void Awake()
    {
        if (Instance != null)
        {
            if (Instance != this)
            {
                Debug.LogWarning("More than one Instance of TurnManager. Fix me!");
                Destroy(gameObject);
            }
        }
        else
            Instance = this;
    }
    #endregion

    void Start()
    {
        activeUnit = UnitManager.Instance.player;

        foreach (Unit unit in UnitManager.Instance.units)
        {
            if (unit != UnitManager.Instance.player)
                npcs.Add(unit);
        }
    }

    public IEnumerator FinishTurn(Unit unit)
    {
        yield return null;

        if (unit.isMyTurn)
        {
            // Debug.Log(characterManager.name + " is finishing their turn");
            if (unit.IsNPC() == false)
                FinishPlayersTurn();
            else
                FinishNPCsTurn(unit);
        }
    }

    void FinishPlayersTurn()
    {
        UnitManager.Instance.player.SetIsMyTurn(false);
        UnitManager.Instance.player.BlockCurrentPosition();
        npcsFinishedTakingTurnCount = 0;

        DoAllNPCsTurns();
    }

    public void ReadyPlayersTurn()
    {
        activeUnit = UnitManager.Instance.player;

        UnitManager.Instance.player.stats.ReplenishAP();
        UnitManager.Instance.player.SetIsMyTurn(true);
        UnitManager.Instance.player.UnblockCurrentPosition();

        //gm.tileInfoDisplay.DisplayTileInfo();

        /*gm.playerManager.status.UpdateBuffs();
        gm.playerManager.status.UpdateInjuries();
        gm.playerManager.status.RegenerateStamina();
        gm.playerManager.nutrition.DrainStaminaBonus();
        gm.playerManager.nutrition.DrainNourishment();
        gm.playerManager.nutrition.DrainWater();
        gm.playerManager.nutrition.DrainNausea();*/

        TimeSystem.IncreaseTime();

        //gm.healthDisplay.UpdateTooltip();

        //gm.playerManager.vision.CheckEnemyVisibility();

        UnitManager.Instance.player.stats.ApplyAPLossBuildup();
        if (UnitManager.Instance.player.stats.CurrentAP() > 0 && UnitManager.Instance.player.unitActionHandler.queuedAction != null)
            UnitManager.Instance.player.StartCoroutine(UnitManager.Instance.player.unitActionHandler.GetNextQueuedAction());
    }

    void FinishNPCsTurn(Unit npc)
    {
        npc.SetIsMyTurn(false);
        npc.BlockCurrentPosition();
        npcsFinishedTakingTurnCount++;

        if (npcsFinishedTakingTurnCount >= npcs.Count)
            ReadyPlayersTurn();
    }

    public void TakeNPCTurn(Unit npc)
    {
        if (UnitManager.Instance.player.isDead == false)
        {
            activeUnit = npc;

            npc.stats.ReplenishAP();
            npc.SetIsMyTurn(true);
            //npc.UnblockCurrentPosition();

            //npc.status.UpdateBuffs();
            //npc.status.UpdateInjuries();
            //npc.status.RegenerateStamina();

            /*if (npc.nutrition != null)
            {
                npc.nutrition.DrainStaminaBonus();
                npc.nutrition.DrainNourishment();
                npc.nutrition.DrainWater();
                npc.nutrition.DrainNausea();
            }*/

            //npc.characterStats.ApplyAPLossBuildup();

            if (npc.stats.CurrentAP() > 0)
            {
                NPCActionHandler npcActionHandler = npc.unitActionHandler as NPCActionHandler;
                npcActionHandler.TakeTurn();
            }
        }
    }

    void DoAllNPCsTurns()
    {
        if (npcs.Count > 0)
        {
            for (int i = 0; i < npcs.Count; i++)
            {
                TakeNPCTurn(npcs[i]);
            }
        }
        else
            ReadyPlayersTurn();
    }

    public Unit ActiveUnit() => activeUnit;

    public bool IsPlayerTurn() => activeUnit == UnitManager.Instance.player;
}
