using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [HideInInspector] public List<Unit> npcs = new List<Unit>();
    [HideInInspector] public int npcsFinishedTakingTurnCount;

    Unit activeUnit;

    GameManager gm;

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
        gm = GameManager.Instance;

        activeUnit = GameObject.FindGameObjectWithTag("Player").GetComponent<Unit>();
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
        gm.Player().SetIsMyTurn(false);
        gm.Player().BlockCurrentPosition();
        npcsFinishedTakingTurnCount = 0;

        DoAllNPCsTurns();
    }

    public void ReadyPlayersTurn()
    {
        gm.Player().stats.ReplenishAP();
        gm.Player().SetIsMyTurn(true);
        gm.Player().UnblockCurrentPosition();

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

        gm.Player().stats.ApplyAPLossBuildup();
        if (gm.Player().stats.CurrentAP() > 0 && gm.Player().unitActionHandler.queuedActions.Count > 0)
            gm.Player().StartCoroutine(gm.Player().unitActionHandler.GetNextQueuedAction());
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
        if (gm.Player().isDead == false)
        {
            npc.stats.ReplenishAP();
            npc.SetIsMyTurn(true);
            npc.UnblockCurrentPosition();

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
                npc.unitActionHandler.TakeTurn();
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

    public bool IsPlayerTurn() => activeUnit == gm.Player();
}
