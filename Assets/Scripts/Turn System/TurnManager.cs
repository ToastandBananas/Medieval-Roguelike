using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public List<Unit> npcs_HaventFinishedTurn { get; private set; }
    public List<Unit> npcs_FinishedTurn { get; private set; }
    int npcTurnIndex;

    public Unit activeUnit { get; private set; }

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

        npcs_HaventFinishedTurn = new List<Unit>();
        npcs_FinishedTurn = new List<Unit>();
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

        npcs_FinishedTurn.Clear();
        SortNPCsBySpeed();

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

        npcs_FinishedTurn.Add(npc);
        npcs_HaventFinishedTurn.Remove(npc);

        if (npcs_HaventFinishedTurn.Count == 0)
            ReadyPlayersTurn();
        else
            StartNextNPCsAction(npc); // TODO: Make sure this isn't being ran somewhere else at the same time
    }

    public void TakeNPCsTurn(Unit npc)
    {
        if (UnitManager.Instance.player.isDead == false)
        {
            activeUnit = npc;

            npc.stats.ReplenishAP();

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

            npc.stats.ApplyAPLossBuildup();

            if (npc.stats.CurrentAP() > 0)
            {
                NPCActionHandler npcActionHandler = npc.unitActionHandler as NPCActionHandler;
                npcActionHandler.TakeTurn();
            }
        }
        else
        {
            Debug.LogError(npc + " is dead, but they are trying to take their turn...");
            UnitManager.Instance.deadNPCs.Add(npc);
            UnitManager.Instance.livingNPCs.Remove(npc);

            if (npcs_HaventFinishedTurn.Contains(npc))
                npcs_HaventFinishedTurn.Remove(npc);

            if (npcs_FinishedTurn.Contains(npc))
                npcs_FinishedTurn.Remove(npc);
        }
    }

    public void StartNextNPCsAction(Unit unitFinishingAction)
    {
        //yield return null;

        if (activeUnit != unitFinishingAction)
            return;

        if (npcs_HaventFinishedTurn.Count == 0)
        {
            ReadyPlayersTurn();
            return;
        }

        if (npcTurnIndex >= npcs_HaventFinishedTurn.Count - 1)
            npcTurnIndex = 0;
        else
            npcTurnIndex++;

        if (activeUnit != null)
            activeUnit.SetIsMyTurn(false);

        npcs_HaventFinishedTurn[npcTurnIndex].SetIsMyTurn(true);
        TakeNPCsTurn(npcs_HaventFinishedTurn[npcTurnIndex]);
    }

    void DoAllNPCsTurns()
    {
        SortNPCsBySpeed();

        npcTurnIndex = UnitManager.Instance.livingNPCs.Count - 1;
        if (UnitManager.Instance.livingNPCs.Count > 0)
        {
            StartNextNPCsAction(UnitManager.Instance.player);
            /*for (int i = 0; i < UnitManager.Instance.livingNPCs.Count; i++)
            {
                TakeNPCsTurn(UnitManager.Instance.livingNPCs[i]);
            }*/
        }
        else
            ReadyPlayersTurn();
    }

    void SortNPCsBySpeed() => npcs_HaventFinishedTurn = UnitManager.Instance.livingNPCs.OrderByDescending(npc => npc.stats.Speed()).ToList();

    public bool IsPlayerTurn() => activeUnit == UnitManager.Instance.player;
}
