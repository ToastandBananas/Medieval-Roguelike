using GridSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnitSystem
{
    public class TurnManager : MonoBehaviour
    {
        public List<Unit> npcs_HaventFinishedTurn { get; private set; }
        public List<Unit> npcs_FinishedTurn { get; private set; }
        public static int turnNumber { get; private set; }
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
            activeUnit = UnitManager.player;

            npcs_HaventFinishedTurn = new List<Unit>();
            npcs_FinishedTurn = new List<Unit>();

            StartUnitsTurn(activeUnit);
        }

        public void FinishTurn(Unit unit)
        {
            if (unit != activeUnit)
                return;

            if (unit.Health.IsDead == false)
                unit.BlockCurrentPosition();

            if (unit.IsNPC)
            {
                // The unit should be at 0 AP, but if they finished their turn without performing an action (because it had to be cancelled, for example) then just zero out their currentAP
                if (unit.Stats.CurrentAP > 0)
                    unit.Stats.UseAP(unit.Stats.CurrentAP);

                if (unit.Stats.PooledAP <= 0)
                {
                    // The unit has no more pooledAP, so they can't do anything else (their turn is over)
                    npcs_FinishedTurn.Add(unit);
                    npcs_HaventFinishedTurn.Remove(unit);
                }
                else
                {
                    // Put the unit at the end of the list, since they still have some AP to use
                    npcs_HaventFinishedTurn.Remove(unit);
                    npcs_HaventFinishedTurn.Insert(npcs_HaventFinishedTurn.Count, unit);

                    // Since the unit's turn isn't fully over for this round, subtract from the turn index (since we'll be adding to it in DoNextUnitsTurn), unless this is the last unit that needs to finish their turn
                    if (npcs_HaventFinishedTurn.IndexOf(unit) != npcTurnIndex && npcTurnIndex > 0)
                        npcTurnIndex--;

                    // Refill their currentAP with some (or the remainder) of their pooledAP
                    unit.Stats.GetAPFromPool();
                }
            }
            else
                GridSystemVisual.UpdateAttackGridVisual();

            StartNextUnitsTurn(unit);
        }

        void StartUnitsTurn(Unit unit)
        {
            if (UnitManager.player.Health.IsDead)
                return;
            
            activeUnit = unit;
            turnNumber++;

            if (unit.Health.IsDead)
            {
                unit.UnitActionHandler.CancelActions();
                unit.UnitActionHandler.ClearActionQueue(true, true);

                // Debug.LogWarning(unit + " is dead, but they are trying to take their turn...");
                if (unit.IsNPC)
                {
                    if (npcs_HaventFinishedTurn.Contains(unit))
                        npcs_HaventFinishedTurn.Remove(unit);

                    if (npcs_FinishedTurn.Contains(unit))
                        npcs_FinishedTurn.Remove(unit);

                    if (npcTurnIndex == npcs_HaventFinishedTurn.Count)
                        npcTurnIndex = 0;
                }

                // Start the next units turn, but don't increase the turn index (or else it'll mess up the turn order)
                StartNextUnitsTurn(unit, false);
            }
            else
            {
                if (unit.IsPlayer)
                    GridSystemVisual.UpdateAttackGridVisual();
                
                unit.UnitActionHandler.TakeTurn();
            }
        }

        public void StartNextUnitsTurn(Unit unitFinishingAction, bool increaseTurnIndex = true)
        {
            if (activeUnit != unitFinishingAction)
                return;

            StartCoroutine(DoNextUnitsTurn(increaseTurnIndex));
        }

        IEnumerator DoNextUnitsTurn(bool increaseTurnIndex = true)
        {
            // If the final Unit is still performing an action or if someone is attacking
            while (npcs_HaventFinishedTurn.Count == 1 && npcs_HaventFinishedTurn[0].UnitActionHandler.IsPerformingAction)
                yield return null;

            // Increase the turn index or go back to 0 if it's time for a new round of turns
            if (increaseTurnIndex)
            {
                if (npcTurnIndex >= npcs_HaventFinishedTurn.Count - 1)
                    npcTurnIndex = 0;
                else
                    npcTurnIndex++;
            }

            // If every Unit finished their turn
            if (npcs_HaventFinishedTurn.Count == 0 || npcTurnIndex >= npcs_HaventFinishedTurn.Count)
                OnCompleteAllTurns();
            else
            {
                // Set new Active Unit
                activeUnit = npcs_HaventFinishedTurn[npcTurnIndex];
                StartUnitsTurn(activeUnit);
            }
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
                    npcs_HaventFinishedTurn[i].Stats.GetAPFromPool();
                }

                StartNextUnitsTurn(activeUnit);
            }
            else
            {
                activeUnit = UnitManager.player;
                StartUnitsTurn(activeUnit);
            }

            //gm.healthDisplay.UpdateTooltip();
        }

        void SortNPCsBySpeed()
        {
            npcs_HaventFinishedTurn.Clear();
            for (int i = 0; i < UnitManager.livingNPCs.Count; i++)
            {
                if (UnitManager.livingNPCs[i].Stats.PooledAP > 0)
                    npcs_HaventFinishedTurn.Add(UnitManager.livingNPCs[i]);
            }

            if (npcs_HaventFinishedTurn.Count > 0)
                npcs_HaventFinishedTurn = npcs_HaventFinishedTurn.OrderByDescending(npc => npc.Stats.Speed.GetValue()).ToList();
        }

        public bool IsPlayerTurn() => activeUnit == UnitManager.player;
    }
}
