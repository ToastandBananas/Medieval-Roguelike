using UnitSystem;
using UnityEngine;

namespace InventorySystem
{
    public abstract class Item_HeldEquipment : Item_Equipment
    {
        [SerializeField] StanceStatModifier_ScriptableObject[] stanceStatModifiers;

        [Header("Equipped Mesh")]
        [SerializeField] Mesh[] meshes;
        [SerializeField] Material[] meshRendererMaterials;

        [Header("Transform Info")]
        [SerializeField] Vector3 idlePosition_Left;
        [SerializeField] Vector3 idlePosition_Right;
        [SerializeField] Vector3 idleRotation_Left;
        [SerializeField] Vector3 idleRotation_Right;

        public Mesh[] Meshes => meshes;
        public Material[] MeshRendererMaterials => meshRendererMaterials;

        public Vector3 IdlePosition_LeftHand => idlePosition_Left;
        public Vector3 IdlePosition_RightHand => idlePosition_Right;
        public Vector3 IdleRotation_LeftHand => idleRotation_Left;
        public Vector3 IdleRotation_RightHand => idleRotation_Right;

        public StanceStatModifier_ScriptableObject GetStanceStatModifier(HeldItemStance heldItemStance)
        {
            for (int i = 0; i < stanceStatModifiers.Length; i++)
            {
                if (stanceStatModifiers[i].HeldItemStance == heldItemStance)
                    return stanceStatModifiers[i];
            }
            return null;
        }

        public StanceStatModifier_ScriptableObject[] StanceStatModifiers => stanceStatModifiers;
    }
}
