using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Action Type", menuName = "Actions/Action Type")]
public class ActionType : ScriptableObject
{
    [SerializeField] string actionName; // The name used in game
    [SerializeField] string actionTypeName; // Store the Type's full name as a string (for example: MeleeAction Type is just MeleeAction)
    [SerializeField] Sprite actionIcon;

    public string ActionName => actionName;
    public string ActionTypeName => actionTypeName;
    public Sprite ActionIcon => actionIcon;

    // Convert the stored string back to a Type
    public Type GetActionType() => Type.GetType(actionTypeName);

    public BaseAction GetAction(Unit unit) => ActionSystem.GetAction(GetActionType(), unit); 
}
