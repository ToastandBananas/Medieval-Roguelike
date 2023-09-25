using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActionSystem : MonoBehaviour
{
    public static ActionSystem Instance;

    static List<BaseAction> actions = new List<BaseAction>();

    readonly int amountToPool = 1;

    void Awake()
    {
        if (Instance != null)
        {
            if (Instance != this)
            {
                Debug.LogWarning("More than one Instance of ActionSystem. Fix me!");
                Destroy(gameObject);
            }
        }
        else
            Instance = this;

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
        // Find an available action of the specified type
        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i].gameObject.activeSelf == false && actions[i].GetType() == type)
            {
                actions[i].SetUnit(unit);
                return actions[i];
            }
        }

        // If no available action of the specified type is found, create a new one
        BaseAction newAction = CreateNewAction(type);
        newAction.SetUnit(unit);
        return newAction;
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
        // action.SetUnit(null);
        action.gameObject.SetActive(false);
    }
}
