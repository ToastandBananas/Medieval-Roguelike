using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public List<Unit> units_HaventFinishedTurn { get; private set; }
    public List<Unit> units_FinishedTurn { get; private set; }
    int unitTurnIndex;

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

        units_HaventFinishedTurn = new List<Unit>();
        units_FinishedTurn = new List<Unit>();

        StartCoroutine(DelayStartNextUnitsTurn(activeUnit));
    }

    public void FinishTurn(Unit unit)
    {
        unit.SetIsMyTurn(false);
        unit.BlockCurrentPosition();

        units_FinishedTurn.Add(unit);
        units_HaventFinishedTurn.Remove(unit);

        if (unit.IsPlayer())
        {
            // Debug.Log("Player finished their turn");
            GridSystemVisual.Instance.HideAllGridPositions();
        }

        StartCoroutine(StartNextUnitsTurn(unit));
    }

    void StartUnitsTurn(Unit unit)
    {
        if (unit.isDead)
        {
            Debug.LogError(unit + " is dead, but they are trying to take their turn...");
            if (unit.IsNPC())
            {
                UnitManager.Instance.deadNPCs.Add(unit);
                UnitManager.Instance.livingNPCs.Remove(unit);
            }

            if (units_HaventFinishedTurn.Contains(unit))
                units_HaventFinishedTurn.Remove(unit);

            if (units_FinishedTurn.Contains(unit))
                units_FinishedTurn.Remove(unit);
        }
        else
        {
            activeUnit = unit;
            unit.SetIsMyTurn(true);

            if (unit.hasStartedTurn == false)
            {
                unit.vision.UpdateVisibleUnits();
                unit.vision.FindVisibleUnits();

                unit.SetHasStartedTurn(true);
                unit.stats.ReplenishAP();

                /*
                unit.status.UpdateBuffs();
                unit.status.UpdateInjuries();
                unit.status.RegenerateStamina();
                unit.nutrition.DrainStaminaBonus();

                if (unit.unitActionHandler.canPerformActions || unit.IsPlayer())
                {
                    unit.nutrition.DrainNourishment();
                    unit.nutrition.DrainWater();
                    unit.nutrition.DrainNausea();
                }
                */

                unit.stats.ApplyAPLossBuildup();
            }
            
            unit.unitActionHandler.TakeTurn();
        }
    }

    public IEnumerator DelayStartNextUnitsTurn(Unit unitFinishingAction)
    {
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(StartNextUnitsTurn(unitFinishingAction));
    }

    public IEnumerator StartNextUnitsTurn(Unit unitFinishingAction)
    {
        if (activeUnit != unitFinishingAction)
            yield break;

        // If the final Unit is still performing an action
        while (units_HaventFinishedTurn.Count == 1 && units_HaventFinishedTurn[0].unitActionHandler.isPerformingAction)
        {
            yield return null;
        }

        // If every Unit finished their turn
        if (units_HaventFinishedTurn.Count == 0)
            OnCompleteAllTurns();

        // Increase the turn index
        if (unitTurnIndex >= units_HaventFinishedTurn.Count - 1)
            unitTurnIndex = 0;
        else
            unitTurnIndex++;

        activeUnit.SetIsMyTurn(false);

        // Set new Active Unit
        activeUnit = units_HaventFinishedTurn[unitTurnIndex];

        StartUnitsTurn(units_HaventFinishedTurn[unitTurnIndex]);
    }

    void OnCompleteAllTurns()
    {
        //gm.tileInfoDisplay.DisplayTileInfo();

        TimeSystem.IncreaseTime(); 
        
        units_FinishedTurn.Clear();
        SortNPCsBySpeed();
        unitTurnIndex = UnitManager.Instance.livingNPCs.Count - 1;

        for (int i = 0; i < units_HaventFinishedTurn.Count; i++)
        {
            units_HaventFinishedTurn[i].SetHasStartedTurn(false);
        }

        //gm.healthDisplay.UpdateTooltip();
    }

    void SortNPCsBySpeed() => units_HaventFinishedTurn = UnitManager.Instance.livingNPCs.OrderByDescending(npc => npc.stats.Speed()).ToList();

    public bool IsPlayerTurn() => activeUnit == UnitManager.Instance.player;
}
