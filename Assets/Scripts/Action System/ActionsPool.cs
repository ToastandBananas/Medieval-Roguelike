using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnitSystem;
using InventorySystem;

namespace ActionSystem
{
    public class ActionsPool : MonoBehaviour
    {
        public static ActionsPool Instance;

        static List<BaseAction> actions = new List<BaseAction>();

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
            List<Type> actionTypes = FindDerivedTypes<BaseAction>();
            foreach (Type type in actionTypes)
            {
                for (int i = 0; i < amountToPool; i++)
                {
                    // Create an instance of the found type and add it to the list
                    CreateNewAction(type);
                }
            }
        }

        public static BaseAction GetAction(Type type, Unit unit)
        {
            for (int i = 0; i < unit.unitActionHandler.AvailableActions.Count; i++)
            {
                if (unit.unitActionHandler.AvailableActions[i].GetType() == type)
                    return unit.unitActionHandler.AvailableActions[i];
            }

            // Find an available action of the specified type
            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i].GetType() != type)
                    continue;

                BaseAction action = actions[i];
                SetupAction(action, unit);
                return action;
            }

            // If no available action of the specified type is found, create a new one
            BaseAction newAction = CreateNewAction(type);
            SetupAction(newAction, unit);
            return newAction;
        }

        static void SetupAction(BaseAction action, Unit unit)
        {
            action.SetUnit(unit);

            unit.unitActionHandler.AvailableActions.Add(action);
            if (action is BaseAttackAction)
                unit.unitActionHandler.AvailableCombatActions.Add(action as BaseAttackAction);

            action.transform.SetParent(unit.ActionsParent);
            action.gameObject.SetActive(true);
            actions.Remove(action);
        }

        static BaseAction CreateNewAction(Type type)
        {
            BaseAction action = (BaseAction)new GameObject(type.Name).AddComponent(type);
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

        public static void ReturnToPool(BaseAction action)
        {
            // De-queue the action if necessary
            if (action.unit.unitActionHandler.queuedActions.Contains(action))
            {
                int actionIndex = action.unit.unitActionHandler.queuedActions.IndexOf(action);
                action.unit.unitActionHandler.queuedActions.RemoveAt(actionIndex);
                if (action.unit.unitActionHandler.queuedAPs.Count >= actionIndex + 1)
                    action.unit.unitActionHandler.queuedAPs.RemoveAt(actionIndex);
            }

            action.unit.unitActionHandler.AvailableActions.Remove(action);
            if (action.unit.unitActionHandler.AvailableCombatActions.Contains(action))
                action.unit.unitActionHandler.AvailableCombatActions.Remove(action as BaseAttackAction);

            action.transform.SetParent(Instance.transform);
            actions.Add(action);
            action.gameObject.SetActive(false);
        }
    }
}
