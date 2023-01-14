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

        if (unit.IsMyTurn())
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
        npcsFinishedTakingTurnCount = 0;

        DoAllNPCsTurns();
    }

    public void ReadyPlayersTurn()
    {
        gm.Player().Stats().ReplenishAP();
        gm.Player().SetIsMyTurn(true);

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

        gm.Player().Stats().ApplyAPLossBuildup();
        if (gm.Player().Stats().CurrentAP() > 0 && gm.Player().UnitActionHandler().QueuedActions().Count > 0)
            gm.Player().StartCoroutine(gm.Player().UnitActionHandler().GetNextQueuedAction());
    }

    void FinishNPCsTurn(Unit npc)
    {
        npc.SetIsMyTurn(false);
        npcsFinishedTakingTurnCount++;

        if (npcsFinishedTakingTurnCount >= npcs.Count)
            ReadyPlayersTurn();
    }

    public void TakeNPCTurn(Unit npc)
    {
        if (gm.Player().IsDead() == false)
        {
            npc.Stats().ReplenishAP();
            npc.SetIsMyTurn(true);

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

            if (npc.Stats().CurrentAP() > 0)
                npc.UnitActionHandler().TakeTurn();
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
