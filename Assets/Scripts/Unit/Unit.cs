using Pathfinding;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Unit Info")]
    [SerializeField] float shoulderHeight = 0.25f;
    [SerializeField] Transform leftHeldItemParent, rightHeldItemParent;
    [SerializeField] MeshRenderer[] meshRenderers;
    MeshRenderer leftHeldItemMeshRenderer, rightHeldItemMeshRenderer;
    MeshRenderer[] bowMeshRenderers;
    LineRenderer bowLineRenderer;

    public bool meshesHidden { get; private set; }
    
    public HeldItem leftHeldItem { get; private set; }
    public HeldItem rightHeldItem { get; private set; }

    public bool isMyTurn { get; private set; }
    public bool hasStartedTurn { get; private set; }

    public GridPosition gridPosition { get; private set; }

    public SingleNodeBlocker singleNodeBlocker { get; private set; }

    public Alliance alliance { get; private set; }
    public HealthSystem health { get; private set; }
    public Hearing hearing { get; private set; }
    public StateController stateController { get; private set; }
    public Stats stats { get; private set; }
    public UnitActionHandler unitActionHandler { get; private set; }
    public UnitAnimator unitAnimator { get; private set; }
    public Vision vision { get; private set; }

    void Awake()
    {
        // Center the Unit's position on whatever tile they're on
        CenterPosition();
        
        singleNodeBlocker = GetComponent<SingleNodeBlocker>();
        alliance = GetComponent<Alliance>();
        health = GetComponent<HealthSystem>();
        hearing = GetComponentInChildren<Hearing>();
        stateController = GetComponent<StateController>();
        stats = GetComponent<Stats>();
        unitActionHandler = GetComponent<UnitActionHandler>();
        unitAnimator = GetComponentInChildren<UnitAnimator>();
        vision = GetComponentInChildren<Vision>();

        SetLeftHeldItem();
        SetRightHeldItem();
    }

    void Start()
    {
        if (leftHeldItem != null)
        {
            if (leftHeldItem.itemData.item.itemType == ItemType.RangedWeapon) // The item is a Bow
            {
                bowMeshRenderers = leftHeldItem.GetComponentsInChildren<MeshRenderer>();
                bowLineRenderer = leftHeldItem.GetComponentInChildren<LineRenderer>();
            }
            else
                leftHeldItemMeshRenderer = leftHeldItem.GetComponentInChildren<MeshRenderer>();
        }

        if (rightHeldItem != null)
            rightHeldItemMeshRenderer = rightHeldItem.GetComponentInChildren<MeshRenderer>();

        singleNodeBlocker.manager = LevelGrid.Instance.GetBlockManager();
        LevelGrid.Instance.AddSingleNodeBlockerToList(singleNodeBlocker, LevelGrid.Instance.GetUnitSingleNodeBlockerList());

        if (IsNPC())
        {
            BlockCurrentPosition();
            HideMeshRenderers();
        }

        gridPosition = LevelGrid.GetGridPosition(transform.position);
        LevelGrid.Instance.AddUnitAtGridPosition(gridPosition, this);
    }

    // Used for debugging
    /*void Update()
    {
        if (isMyTurn && unitActionHandler.isPerformingAction == false)
            unitActionHandler.TakeTurn();
    }*/

    public bool TryBlockRangedAttack(Unit attackingUnit)
    {
        if (ShieldEquipped())
        {
            // If the attacker is in front of this Unit (greater chance to block)
            if (unitActionHandler.GetAction<TurnAction>().AttackerInFrontOfUnit(attackingUnit))
            {
                float random = Random.Range(1f, 100f);
                if (random <= stats.ShieldBlockChance(GetShield(), false))
                    return true;
            }
            // If the attacker is beside this Unit (less of a chance to block)
            else if (unitActionHandler.GetAction<TurnAction>().AttackerBesideUnit(attackingUnit))
            {
                float random = Random.Range(1f, 100f);
                if (random <= stats.ShieldBlockChance(GetShield(), true))
                    return true;
            }
        }
        return false;
    }

    public bool TryBlockMeleeAttack(Unit attackingUnit, out HeldItem itemBlockedLastAttackWith)
    {
        float random;
        TurnAction targetUnitTurnAction = unitActionHandler.GetAction<TurnAction>();
        if (targetUnitTurnAction.AttackerInFrontOfUnit(attackingUnit))
        {
            if (ShieldEquipped())
            {
                // Try blocking with shield
                random = Random.Range(1f, 100f);
                if (random <= stats.ShieldBlockChance(GetShield(), false))
                {
                    itemBlockedLastAttackWith = GetShield();
                    return true;
                }

                // Still have a chance to block with weapon
                if (MeleeWeaponEquipped())
                {
                    random = Random.Range(1f, 100f);
                    if (random <= stats.WeaponBlockChance(GetPrimaryMeleeWeapon(), false, true))
                    {
                        itemBlockedLastAttackWith = GetPrimaryMeleeWeapon();
                        return true;
                    }
                }
            }
            else if (MeleeWeaponEquipped())
            {
                if (IsDualWielding())
                {
                    // Try blocking with right weapon
                    random = Random.Range(1f, 100f);
                    if (random <= stats.WeaponBlockChance(GetPrimaryMeleeWeapon(), false, false) * GameManager.dualWieldPrimaryEfficiency)
                    {
                        itemBlockedLastAttackWith = GetPrimaryMeleeWeapon();
                        return true;
                    }

                    // Try blocking with left weapon
                    random = Random.Range(1f, 100f);
                    if (random <= stats.WeaponBlockChance(GetLeftMeleeWeapon(), false, false) * GameManager.dualWieldSecondaryEfficiency)
                    {
                        itemBlockedLastAttackWith = GetLeftMeleeWeapon();
                        return true;
                    }
                }
                else
                {
                    // Try blocking with only weapon
                    random = Random.Range(1f, 100f);
                    if (random <= stats.WeaponBlockChance(GetPrimaryMeleeWeapon(), false, false))
                    {
                        itemBlockedLastAttackWith = GetPrimaryMeleeWeapon();
                        return true;
                    }
                }
            }
        }
        else if (targetUnitTurnAction.AttackerBesideUnit(attackingUnit))
        {
            if (ShieldEquipped())
            {
                // Try blocking with shield
                random = Random.Range(1f, 100f);
                if (random <= stats.ShieldBlockChance(GetShield(), true))
                {
                    itemBlockedLastAttackWith = GetShield();
                    return true;
                }

                // Still have a chance to block with weapon
                if (MeleeWeaponEquipped())
                {
                    random = Random.Range(1f, 100f);
                    if (random <= stats.WeaponBlockChance(GetPrimaryMeleeWeapon(), true, true))
                    {
                        itemBlockedLastAttackWith = GetPrimaryMeleeWeapon();
                        return true;
                    }
                }
            }
            else if (MeleeWeaponEquipped())
            {
                if (IsDualWielding())
                {
                    // Try blocking with right weapon
                    random = Random.Range(1f, 100f);
                    if (random <= stats.WeaponBlockChance(GetPrimaryMeleeWeapon(), true, false) * GameManager.dualWieldPrimaryEfficiency)
                    {
                        itemBlockedLastAttackWith = GetPrimaryMeleeWeapon();
                        return true;
                    }

                    // Try blocking with left weapon
                    random = Random.Range(1f, 100f);
                    if (random <= stats.WeaponBlockChance(GetLeftMeleeWeapon(), true, false) * GameManager.dualWieldSecondaryEfficiency)
                    {
                        itemBlockedLastAttackWith = GetLeftMeleeWeapon();
                        return true;
                    }
                }
                else
                {
                    // Try blocking with only weapon
                    random = Random.Range(1f, 100f);
                    if (random <= stats.WeaponBlockChance(GetPrimaryMeleeWeapon(), true, false))
                    {
                        itemBlockedLastAttackWith = GetPrimaryMeleeWeapon();
                        return true;
                    }
                }
            }
        }

        itemBlockedLastAttackWith = null;
        return false;
    }

    public void UpdateGridPosition()
    {
        GridPosition newGridPosition = LevelGrid.GetGridPosition(transform.position);
        if (newGridPosition != gridPosition)
        {
            // Unit changed Grid Position
            GridPosition oldGridPosition = gridPosition;
            gridPosition = newGridPosition;
            LevelGrid.Instance.UnitMovedGridPosition(this, oldGridPosition, newGridPosition);
        }
    }

    public void ShowMeshRenderers()
    {
        if (meshesHidden == false)
            return;

        meshesHidden = false;

        if (meshRenderers != null)
        {
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                meshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        if (leftHeldItemMeshRenderer != null)
            leftHeldItemMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        if (rightHeldItemMeshRenderer != null)
            rightHeldItemMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        if (bowMeshRenderers != null)
        {
            for (int i = 0; i < bowMeshRenderers.Length; i++)
            {
                bowMeshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            if (bowLineRenderer != null)
                bowLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    public void HideMeshRenderers()
    {
        if (meshesHidden)
            return;

        meshesHidden = true;

        if (meshRenderers != null)
        {
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                meshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }

        if (leftHeldItemMeshRenderer != null)
            leftHeldItemMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        if (rightHeldItemMeshRenderer != null)
            rightHeldItemMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        if (bowMeshRenderers != null)
        {
            for (int i = 0; i < bowMeshRenderers.Length; i++)
            {
                bowMeshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }

            if (bowLineRenderer != null)
                bowLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }

    public bool IsCompletelySurrounded(float range)
    {
        List<GridPosition> surroundingGridPositions = LevelGrid.Instance.GetSurroundingGridPositions(gridPosition, range);
        for (int i = 0; i < surroundingGridPositions.Count; i++)
        {
            if (LevelGrid.Instance.GridPositionObstructed(surroundingGridPositions[i]) == false)
                return false;
        }
        return true;
    }

    public void SetLeftHeldItem()
    {
        if (leftHeldItemParent.childCount > 0)
        {
            leftHeldItem = leftHeldItemParent.GetChild(0).GetComponent<HeldItem>();
            unitAnimator.SetLeftHeldItemAnim(leftHeldItem.GetComponent<Animator>());
        }
    }

    public void SetRightHeldItem()
    {
        if (rightHeldItemParent.childCount > 0)
        {
            rightHeldItem = rightHeldItemParent.GetChild(0).GetComponent<HeldItem>();
            unitAnimator.SetRightHeldItemAnim(rightHeldItem.GetComponent<Animator>());
        }
    }

    public bool IsDualWielding() => leftHeldItem != null && rightHeldItem != null && leftHeldItem.itemData.item.IsMeleeWeapon() && rightHeldItem.itemData.item.IsMeleeWeapon();

    public bool MeleeWeaponEquipped() => (leftHeldItem != null && leftHeldItem.itemData.item.IsMeleeWeapon()) || (rightHeldItem != null && rightHeldItem.itemData.item.IsMeleeWeapon());

    public bool RangedWeaponEquipped() => leftHeldItem != null && leftHeldItem.itemData.item.IsRangedWeapon();

    public bool ShieldEquipped() => (leftHeldItem != null && leftHeldItem.itemData.item.IsShield()) || (rightHeldItem != null && rightHeldItem.itemData.item.IsShield());

    public bool IsUnarmed() => leftHeldItem == null && rightHeldItem == null;

    public void SwitchWeapon()
    {
        if (RangedWeaponEquipped())
        {
            leftHeldItem.enabled = false;
            leftHeldItem = null;
        }
    }

    public HeldMeleeWeapon GetPrimaryMeleeWeapon()
    {
        if (rightHeldItem != null && rightHeldItem.itemData.item.IsMeleeWeapon())
            return rightHeldItem as HeldMeleeWeapon;
        else if (leftHeldItem != null && leftHeldItem.itemData.item.IsMeleeWeapon())
            return leftHeldItem as HeldMeleeWeapon;
        return null;
    }

    public HeldRangedWeapon GetRangedWeapon() => leftHeldItem == null ? null : leftHeldItem as HeldRangedWeapon;

    public HeldMeleeWeapon GetLeftMeleeWeapon() => leftHeldItem == null ? null : leftHeldItem as HeldMeleeWeapon;

    public HeldMeleeWeapon GetRightMeleeWeapon() => rightHeldItem == null ? null : rightHeldItem as HeldMeleeWeapon;

    public HeldShield GetShield()
    {
        if (leftHeldItem != null && leftHeldItem.itemData.item.IsShield())
            return leftHeldItem as HeldShield;
        else if (rightHeldItem != null && rightHeldItem.itemData.item.IsShield())
            return rightHeldItem as HeldShield;
        return null;
    }

    public float GetAttackRange(bool accountForHeight)
    {
        if (RangedWeaponEquipped())
            return GetRangedWeapon().MaxRange(gridPosition, unitActionHandler.targetEnemyUnit.gridPosition, accountForHeight);
        else if (MeleeWeaponEquipped())
            return GetPrimaryMeleeWeapon().MaxRange(gridPosition, unitActionHandler.targetEnemyUnit.gridPosition, accountForHeight);
        else
            return unitActionHandler.GetAction<MeleeAction>().UnarmedAttackRange(unitActionHandler.targetEnemyUnit.gridPosition, accountForHeight);
    }

    public void BlockCurrentPosition() => singleNodeBlocker.BlockAtCurrentPosition();

    public void UnblockCurrentPosition() => singleNodeBlocker.Unblock();

    public void BlockAtPosition(Vector3 position) => singleNodeBlocker.BlockAt(position);

    public bool IsNPC() => gameObject.CompareTag("Player") == false;

    public bool IsPlayer() => gameObject.CompareTag("Player");

    public bool IsVisibleOnScreen() => meshRenderers[0].isVisible && UnitManager.Instance.player.vision.IsVisible(this);

    public void SetIsMyTurn(bool isMyTurn) 
    {
        this.isMyTurn = isMyTurn;
        if (IsPlayer())
            GridSystemVisual.UpdateGridVisual();
    }

    public void SetHasStartedTurn(bool hasStartedTurn) => this.hasStartedTurn = hasStartedTurn;

    public Vector3 WorldPosition() => LevelGrid.GetWorldPosition(gridPosition);

    public float ShoulderHeight() => shoulderHeight;

    public void CenterPosition() => transform.position = LevelGrid.GetGridPosition(transform.position).WorldPosition();
}
