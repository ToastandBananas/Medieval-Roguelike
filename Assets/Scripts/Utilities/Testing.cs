using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using GridSystem;
using SoundSystem;
using GeneralUI;
using UnitSystem;

namespace Utilities
{
    public class Testing : MonoBehaviour
    {
        void Update()
        {
            // PauseWhenLessThanTargetFPS(30);
            if (Input.GetKeyDown(KeyCode.T))
            {
                // DebugPause();
                PlayTestSound();
                // DamagePlayer(10, BodyPartType.Torso);
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                // HealPlayer(15, BodyPartType.Torso);
            }
        }

        void DamagePlayer(int damage, BodyPartType bodyPartType, BodyPartSide bodyPartSide = BodyPartSide.NotApplicable, BodyPartIndex bodyPartIndex = BodyPartIndex.Only)
        {
            UnitManager.player.HealthSystem.GetBodyPart(bodyPartType, bodyPartSide, bodyPartIndex).TakeDamage(damage, null);
        }

        void HealPlayer(int healAmount, BodyPartType bodyPartType, BodyPartSide bodyPartSide = BodyPartSide.NotApplicable, BodyPartIndex bodyPartIndex = BodyPartIndex.Only)
        {
            UnitManager.player.HealthSystem.GetBodyPart(bodyPartType, bodyPartSide, bodyPartIndex).Heal(healAmount);
        }

        void PauseWhenLessThanTargetFPS(int targetFPS)
        {
            float currentFPS = 1f / Time.deltaTime;
            if (currentFPS < targetFPS)
                DebugPause();
        }

        void DebugPause() => Debug.Break();

        void ShowDebugPathToMousePosition()
        {
            GridPosition mouseGridPosition = LevelGrid.GetGridPosition(WorldMouse.GetPosition());
            GridPosition startGridPosition = UnitManager.player.GridPosition;
            ABPath path = ABPath.Construct(LevelGrid.GetWorldPosition(startGridPosition), LevelGrid.GetWorldPosition(mouseGridPosition));
            path.traversalProvider = LevelGrid.DefaultTraversalProvider;

            AstarPath.StartPath(path);
            path.BlockUntilCalculated();

            List<GridPosition> gridPositionList = new();

            for (int i = 0; i < path.vectorPath.Count; i++)
                gridPositionList.Add(LevelGrid.GetGridPosition(path.vectorPath[i]));

            for (int i = 0; i < gridPositionList.Count - 1; i++)
                Debug.DrawLine(LevelGrid.GetWorldPosition(gridPositionList[i]), LevelGrid.GetWorldPosition(gridPositionList[i + 1]), Color.white, 4f);
        }

        void PlayTestSound() => AudioManager.PlayRandomSound(AudioManager.Instance.humanMaleGruntSounds, UnitManager.player.WorldPosition, UnitManager.player, true);
    }
}
