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

        StartUnitsTurn(activeUnit);
        //StartCoroutine(DelayStartNextUnitsTurn(activeUnit));
    }

    public void FinishTurn(Unit unit)
    {
        unit.SetIsMyTurn(false);
        unit.BlockCurrentPosition();

        if (unit.IsNPC())
        {
            if (unit.stats.currentAP > 0)
                unit.stats.UseAP(unit.stats.currentAP);

            if (unit.stats.pooledAP <= 0)
            {
                npcs_FinishedTurn.Add(unit);
                npcs_HaventFinishedTurn.Remove(unit);
            }
            else
            {
                npcs_HaventFinishedTurn.Remove(unit);
                npcs_HaventFinishedTurn.Insert(npcs_HaventFinishedTurn.Count, unit);
                if (npcs_HaventFinishedTurn.IndexOf(unit) != npcTurnIndex)
                    npcTurnIndex--;
                unit.stats.GetAPFromPool();
            }
        }

        StartNextUnitsTurn(unit);
    }

    void StartUnitsTurn(Unit unit)
    {
        if (UnitManager.Instance.player.health.IsDead())
            return;

        activeUnit = unit;

        if (unit.health.IsDead())
        {
            unit.unitActionHandler.CancelAction();

            // Debug.LogWarning(unit + " is dead, but they are trying to take their turn...");
            if (unit.IsNPC())
            {
                if (npcs_HaventFinishedTurn.Contains(unit))
                    npcs_HaventFinishedTurn.Remove(unit);

                if (npcs_FinishedTurn.Contains(unit))
                    npcs_FinishedTurn.Remove(unit);

                if (npcTurnIndex == npcs_HaventFinishedTurn.Count)
                    npcTurnIndex = 0;
            }

            StartNextUnitsTurn(unit, false);
        }
        else
        {
            unit.SetIsMyTurn(true);
            unit.unitActionHandler.TakeTurn();
        }
    }

    IEnumerator DoNextUnitsTurn(Unit unitFinishingAction, bool increaseTurnIndex = true)
    {
        // If the final Unit is still performing an action
        while (npcs_HaventFinishedTurn.Count == 1 && npcs_HaventFinishedTurn[0].unitActionHandler.isPerformingAction)
        {
            yield return null;
        }

        if (increaseTurnIndex)
        {
            // Increase the turn index
            if (npcTurnIndex >= npcs_HaventFinishedTurn.Count - 1)
                npcTurnIndex = 0;
            else
                npcTurnIndex++;
        }

        // If every Unit finished their turn
        if (npcs_HaventFinishedTurn.Count == 0)
            OnCompleteAllTurns();
        else
        {
            // Set new Active Unit
            activeUnit.SetIsMyTurn(false);
            activeUnit = npcs_HaventFinishedTurn[npcTurnIndex];
            StartUnitsTurn(activeUnit);
        }
    }

    public void StartNextUnitsTurn(Unit unitFinishingAction, bool increaseTurnIndex = true)
    {
        if (activeUnit != unitFinishingAction)
            return;

        StartCoroutine(DoNextUnitsTurn(unitFinishingAction, increaseTurnIndex));
    }

    public IEnumerator DelayStartNextUnitsTurn(Unit unitFinishingAction)
    {
        yield return new WaitForSeconds(0.1f);
        StartNextUnitsTurn(unitFinishingAction);
    }

    void OnCompleteAllTurns()
    {
        //gm.tileInfoDisplay.DisplayTileInfo(); 
        
        npcs_FinishedTurn.Clear();
        SortNPCsBySpeed();

        if (npcs_HaventFinishedTurn.Count > 0)
        {
            npcTurnIndex = npcs_HaventFinishedTurn.Count - 1;

            for (int i = 0; i < npcs_HaventFinishedTurn.Count; i++)
            {
                npcs_HaventFinishedTurn[i].SetHasStartedTurn(false);
                npcs_HaventFinishedTurn[i].stats.GetAPFromPool();
            }

            StartNextUnitsTurn(activeUnit);
        }
        else
        {
            activeUnit.SetIsMyTurn(false);
            activeUnit = UnitManager.Instance.player;
            StartUnitsTurn(activeUnit);
        }

        //gm.healthDisplay.UpdateTooltip();
    }

    void SortNPCsBySpeed()
    {
        npcs_HaventFinishedTurn.Clear();
        for (int i = 0; i < UnitManager.Instance.livingNPCs.Count; i++)
        {
            if (UnitManager.Instance.livingNPCs[i].stats.pooledAP > 0)
                npcs_HaventFinishedTurn.Add(UnitManager.Instance.livingNPCs[i]);
        }

        if (npcs_HaventFinishedTurn.Count > 0)
            npcs_HaventFinishedTurn = npcs_HaventFinishedTurn.OrderByDescending(npc => npc.stats.Speed()).ToList();
    }

    public bool IsPlayerTurn() => activeUnit == UnitManager.Instance.player;
}
