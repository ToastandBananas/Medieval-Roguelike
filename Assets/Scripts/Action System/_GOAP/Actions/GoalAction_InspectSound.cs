using GridSystem;
using System;
using System.Collections.Generic;
using UnitSystem.ActionSystem.GOAP.Goals;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UnitSystem.ActionSystem.GOAP.GoalActions
{
    public class GoalAction_InspectSound : GoalAction_Base
    {
        GridPosition inspectSoundGridPosition;
        public GridPosition SoundGridPosition { get; private set; }
        int inspectSoundIterations;
        int maxInspectSoundIterations;
        bool needsNewSoundInspectPosition = true;

        public override void OnTick()
        {
            InspectSound();
        }

        public override void OnDeactivated()
        {
            base.OnDeactivated();
            needsNewSoundInspectPosition = true;
            inspectSoundIterations = 0;
        }

        #region Inspect Sound
        void InspectSound()
        {
            if (needsNewSoundInspectPosition)
            {
                if (inspectSoundIterations == maxInspectSoundIterations)
                {
                    unit.StateController.SetToDefaultState();
                    npcActionHandler.DetermineAction();
                    return;
                }

                needsNewSoundInspectPosition = false;

                inspectSoundGridPosition = LevelGrid.Instance.GetRandomGridPositionInRange(SoundGridPosition, unit, 0 + inspectSoundIterations, 2 + inspectSoundIterations, true);
                npcActionHandler.MoveAction.QueueAction(inspectSoundGridPosition);
            }
            else if (Vector3.Distance(inspectSoundGridPosition.WorldPosition, transform.position) <= 0.1f)
            {
                // Get a new Inspect Sound Position when the current one is reached
                inspectSoundIterations++;
                needsNewSoundInspectPosition = true;
                InspectSound();
            }
            else if (npcActionHandler.MoveAction.IsMoving == false)
            {
                // Get a new Inspect Sound Position if there's now another Unit or obstruction there
                if (LevelGrid.GridPositionObstructed(inspectSoundGridPosition))
                    inspectSoundGridPosition = LevelGrid.GetNearestSurroundingGridPosition(inspectSoundGridPosition, unit.GridPosition, LevelGrid.diaganolDistance, true);

                npcActionHandler.MoveAction.QueueAction(inspectSoundGridPosition);
            }
        }

        public void SetSoundGridPosition(Vector3 soundPosition)
        {
            SoundGridPosition = LevelGrid.GetGridPosition(soundPosition);
            maxInspectSoundIterations = Random.Range(3, 7);
        }
        #endregion
    }
}
