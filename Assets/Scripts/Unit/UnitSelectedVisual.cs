using UnityEngine;
using System;

public class UnitSelectedVisual : MonoBehaviour
{
    [SerializeField] Unit unit;

    MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void Start()
    {
        UnitActionSystem.Instance.OnSelectedUnitChanged += UnitActionSystem_OnSelectedUnitChanged;
        UnitActionSystem.Instance.OnUnitDeselected += UnitActionSystem_OnUnitDeselected;
        UnitActionSystem.Instance.OnActiveAIUnitChanged += UnitActionSystem_OnActiveAIUnitChanged;

        UpdateVisual();
    }

    void UnitActionSystem_OnSelectedUnitChanged(object sender, EventArgs e) => UpdateVisual();

    void UnitActionSystem_OnUnitDeselected(object sender, EventArgs e) => UpdateVisual();

    void UnitActionSystem_OnActiveAIUnitChanged(object sender, EventArgs e) => UpdateVisual();

    void UpdateVisual()
    {
        if (UnitActionSystem.Instance.SelectedUnit() == unit)
            meshRenderer.enabled = true;
        else
            meshRenderer.enabled = false;
    }

    void OnDestroy()
    {
        UnitActionSystem.Instance.OnSelectedUnitChanged -= UnitActionSystem_OnSelectedUnitChanged;
        UnitActionSystem.Instance.OnActiveAIUnitChanged -= UnitActionSystem_OnActiveAIUnitChanged;
    }
}
