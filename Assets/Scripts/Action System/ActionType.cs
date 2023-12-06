using System;
using UnitSystem.ActionSystem.Actions;
using UnityEngine;

namespace UnitSystem.ActionSystem
{
    [CreateAssetMenu(fileName = "New Action Type", menuName = "Actions/Action Type")]
    public class ActionType : ScriptableObject
    {
        [SerializeField] string actionName; // The name used in game
        [SerializeField] string actionTypeName; // Store the Type's full name as a string (for example: MeleeAction Type is just MeleeAction)
        [SerializeField] Sprite actionIcon;
        [SerializeField] Sprite cancelActionIcon;

        public string ActionName => actionName;
        public string ActionTypeName => actionTypeName;
        public Sprite ActionIcon => actionIcon;
        public Sprite CancelActionIcon => cancelActionIcon;

        readonly string actionTypeNamespace = "UnitSystem.ActionSystem.Action.";

        // Convert the stored string back to a Type
        public Type GetActionType() => Type.GetType(actionTypeNamespace + actionTypeName);

        public BaseAction GetAction(Unit unit) => ActionsPool.GetAction(GetActionType(), this, unit);
    }
}
