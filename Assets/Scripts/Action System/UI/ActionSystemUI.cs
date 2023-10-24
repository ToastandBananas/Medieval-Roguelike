using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using GridSystem;
using UnitSystem;

namespace ActionSystem
{
    public enum ActionBarSection { None, Basic, Default, Item }

    public class ActionSystemUI : MonoBehaviour
    {
        public static ActionSystemUI Instance { get; private set; }

        [SerializeField] Transform actionButtonPrefab;

        [Header("Parent Transforms")]
        [SerializeField] Transform basicActionButtons_ParentTransform;
        [SerializeField] Transform otherActions_ParentTransform;
        [SerializeField] Transform itemActions_ParentTransform;

        [Header("Stat Texts")]
        [SerializeField] TextMeshProUGUI actionPointsText;
        [SerializeField] TextMeshProUGUI energyText;
        [SerializeField] TextMeshProUGUI healthText;

        int amountActionButtonsToPool = 8;
        List<ActionButtonUI> actionButtons = new List<ActionButtonUI>();

        PlayerActionHandler playerActionHandler;

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("There's more than one UnitActionSystemUI! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }
            Instance = this;

            playerActionHandler = UnitManager.player.unitActionHandler as PlayerActionHandler;
            playerActionHandler.OnSelectedActionChanged += UnitActionSystem_OnSelectedActionChanged;

            InitializeActionButtonPool();

            SetupUnitActionButtons();
        }

        void Start()
        {
            UpdateSelectedVisual();
            
            UpdateActionPointsText();
            UpdateEnergyText();
            UpdateHealthText();

            UpdateActionVisuals();
        }

        public static void SetupUnitActionButtons()
        {
            for (int i = 0; i < Instance.playerActionHandler.AvailableActionTypes.Count; i++)
            {
                if (Instance.playerActionHandler.AvailableActionTypes[i].GetAction(UnitManager.player).ActionBarSection() == ActionBarSection.None)
                    continue;

                AddButton(Instance.playerActionHandler.AvailableActionTypes[i]);
            }

            UpdateActionVisuals();
        }

        public static void AddButton(ActionType actionType)
        {
            for (int i = 0; i < Instance.actionButtons.Count; i++)
            {
                if (Instance.actionButtons[i].gameObject.activeSelf && Instance.actionButtons[i].ActionType == actionType)
                    return;
            }

            ActionButtonUI newActionButton = GetActionButtonFromPool();
            newActionButton.SetActionType(actionType);
            SetParent(actionType.GetAction(UnitManager.player), newActionButton);
            newActionButton.transform.SetSiblingIndex(newActionButton.transform.parent.childCount - 1);
            newActionButton.gameObject.SetActive(true);
        }

        static void SetParent(BaseAction baseAction, ActionButtonUI actionButton)
        {
            switch (baseAction.ActionBarSection())
            {
                case ActionBarSection.None:
                    Debug.LogWarning($"We shouldn't be setting up a button for {baseAction.name}...");
                    break;
                case ActionBarSection.Basic:
                    actionButton.transform.SetParent(Instance.basicActionButtons_ParentTransform, false);
                    break;
                case ActionBarSection.Default:
                    actionButton.transform.SetParent(Instance.otherActions_ParentTransform, false);
                    break;
                case ActionBarSection.Item:
                    actionButton.transform.SetParent(Instance.itemActions_ParentTransform, false);
                    break;
            }
        }

        public static void RemoveButton(ActionType actionType)
        {
            for (int i = 0; i < Instance.actionButtons.Count; i++)
            {
                if (Instance.actionButtons[i].gameObject.activeSelf && Instance.actionButtons[i].ActionType == actionType)
                    Instance.actionButtons[i].ResetButton();
            }
        }

        static void UnitActionSystem_OnSelectedActionChanged(object sender, EventArgs e)
        {
            BaseAction selectedAction = Instance.playerActionHandler.selectedActionType.GetAction(Instance.playerActionHandler.unit);
            if (selectedAction.ActionIsUsedInstantly())
                selectedAction.QueueAction(); // Instant actions don't have a target grid position, so just do a simple queue
            else
            {
                UpdateSelectedVisual();
                GridSystemVisual.UpdateAttackRangeGridVisual();
            }
        }

        static void HideAllActionButtons()
        {
            for (int i = 0; i < Instance.actionButtons.Count; i++)
            {
                Instance.actionButtons[i].ResetButton();
            }
        }

        static void InitializeActionButtonPool()
        {
            for (int i = 0; i < Instance.amountActionButtonsToPool; i++)
            {
                ActionButtonUI newActionButton = CreateNewActionButton();
                newActionButton.gameObject.SetActive(false);
            }
        }

        static ActionButtonUI GetActionButtonFromPool()
        {
            for (int i = 0; i < Instance.actionButtons.Count; i++)
            {
                if (Instance.actionButtons[i].gameObject.activeSelf == false)
                    return Instance.actionButtons[i];
            }

            return CreateNewActionButton();
        }

        static ActionButtonUI CreateNewActionButton()
        {
            ActionButtonUI newActionButton = Instantiate(Instance.actionButtonPrefab).GetComponent<ActionButtonUI>();
            Instance.actionButtons.Add(newActionButton);
            return newActionButton;
        }

        public static List<ActionButtonUI> GetActionButtonsList() => Instance.actionButtons;

        public static bool SelectedActionValid() => Instance.playerActionHandler.selectedActionType.GetAction(Instance.playerActionHandler.unit).IsValidAction();

        public static void UpdateSelectedVisual()
        {
            for (int i = 0; i < Instance.actionButtons.Count; i++)
            {
                Instance.actionButtons[i].UpdateSelectedVisual();
            }
        }

        public static void UpdateActionVisuals()
        {
            for (int i = 0; i < Instance.actionButtons.Count; i++)
            {
                Instance.actionButtons[i].UpdateActionVisual();
            }
        }

        public static void UpdateActionPointsText() => Instance.actionPointsText.text = $"Last Used AP: {Instance.playerActionHandler.unit.stats.lastUsedAP}";

        public static void UpdateEnergyText() => Instance.energyText.text = $"Energy: {Instance.playerActionHandler.unit.stats.currentEnergy}";

        public static void UpdateHealthText() => Instance.healthText.text = $"Health: {Instance.playerActionHandler.unit.health.CurrentHealth}";
    }
}
