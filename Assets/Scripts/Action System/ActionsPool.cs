using System;
using System.Collections.Generic;
using System.Linq;
using UnitSystem.ActionSystem.Actions;
using UnityEngine;

namespace UnitSystem.ActionSystem
{
    public class ActionsPool : MonoBehaviour
    {
        public static ActionsPool Instance;

        static List<Action_Base> actions = new();

        [SerializeField] int amountToPool = 0;

        void Awake()
        {
            if (Instance != null)
            {
                if (Instance != this)
                {
                    Debug.LogWarning("More than one Instance of ActionsPool. Fix me!");
                    Destroy(gameObject);
                }
            }
            else
                Instance = this;

            if (amountToPool == 0)
                return;

            // Use reflection to find all types that inherit from BaseAction
            List<Type> actionTypes = FindDerivedTypes<Action_Base>();
            foreach (Type type in actionTypes)
            {
                for (int i = 0; i < amountToPool; i++)
                {
                    // Create an instance of the found type and add it to the list
                    CreateNewAction(type);
                }
            }
        }

        public static Action_Base GetAction(Type type, ActionType actionType, Unit unit)
        {
            // Try to get the action from the Unit's list of available actions first
            for (int i = 0; i < unit.UnitActionHandler.AvailableActions.Count; i++)
            {
                if (unit.UnitActionHandler.AvailableActions[i].GetType() == type)
                    return unit.UnitActionHandler.AvailableActions[i];
            }

            // Else, find an available action of the specified type from the pool
            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i].gameObject.activeSelf || actions[i].GetType() != type)
                    continue;

                Action_Base action = actions[i];
                SetupAction(action, actionType, unit);
                return action;
            }

            // If no available action of the specified type is found, create a new one
            Action_Base newAction = CreateNewAction(type);
            SetupAction(newAction, actionType, unit);
            return newAction;
        }

        static void SetupAction(Action_Base action, ActionType actionType, Unit unit)
        {
            action.Setup(unit, actionType);

            unit.UnitActionHandler.AvailableActions.Add(action);
            if (action is Action_BaseAttack)
                unit.UnitActionHandler.AvailableCombatActions.Add(action as Action_BaseAttack);
            else if (action is Action_BaseStance)
                unit.UnitActionHandler.AvailableStanceActions.Add(action as Action_BaseStance);

            actions.Remove(action);
            action.transform.SetParent(unit.UnitActionHandler.ActionsParent);
            action.gameObject.SetActive(true);
        }

        static Action_Base CreateNewAction(Type type)
        {
            Action_Base action = (Action_Base)new GameObject(type.Name).AddComponent(type);
            actions.Add(action);
            action.transform.SetParent(Instance.transform);
            action.gameObject.SetActive(false);
            return action;
        }

        List<Type> FindDerivedTypes<T>()
        {
            // Search for types derived from T in all assemblies
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(T).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                .ToList();
        }

        public static void ReturnToPool(Action_Base action)
        {
            // De-queue the action if necessary
            action.Unit.UnitActionHandler.RemoveActionFromQueue(action);

            action.OnReturnToPool();

            action.Unit.UnitActionHandler.AvailableActions.Remove(action);
            if (action is Action_BaseAttack)
                action.Unit.UnitActionHandler.AvailableCombatActions.Remove(action as Action_BaseAttack);
            else if (action is Action_BaseStance)
                action.Unit.UnitActionHandler.AvailableStanceActions.Remove(action as Action_BaseStance);

            action.transform.SetParent(Instance.transform);
            actions.Add(action);

            action.gameObject.SetActive(false);
        }
    }
}
