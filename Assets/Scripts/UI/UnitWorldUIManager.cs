using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitWorldUIManager : MonoBehaviour
{
    public static UnitWorldUIManager Instance { get; private set; }

    [SerializeField] UnitWorldUI unitWorldUIPrefab;
    [SerializeField] bool showAll;

    GridPosition focusedGridPosition;

    int amountToPool;

    List<UnitWorldUI> unitWorldUIList = new List<UnitWorldUI>();

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one UnitWorldUIManager! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (showAll)
            amountToPool = UnitManager.Instance.UnitsList().Count;
        else
            amountToPool = 2;

        for (int i = 0; i < amountToPool; i++)
        {
            UnitWorldUI newUnitWorldUI = CreateNewUnitWorldUI();
            newUnitWorldUI.gameObject.SetActive(false);
        }

        if (showAll)
            ShowAllUnitWorldUI();

        UnitActionSystem.Instance.OnSelectedUnitChanged += UnitWorldUIManager_OnSelectedUnitChanged;

        if (UnitActionSystem.Instance.SelectedUnit() != null)
            ShowUnitWorldUI(UnitActionSystem.Instance.SelectedUnit());
    }

    void FixedUpdate()
    {
        if (showAll == false) // No need to show any additional Unit World UI if they're already all showing
        {
            GridPosition mouseGridPosition = LevelGrid.Instance.GetGridPosition(WorldMouse.GetPosition());
            if (LevelGrid.Instance.IsValidGridPosition(mouseGridPosition) && focusedGridPosition != mouseGridPosition) // If the mouse pointer isn't already hovering over this Grid Position
            {
                DisableAllUnitWorldUI();

                focusedGridPosition = mouseGridPosition;

                Unit unit = LevelGrid.Instance.GetUnitAtGridPosition(focusedGridPosition);
                if (unit == UnitActionSystem.Instance.SelectedUnit())
                    return;

                if (unit != null)
                    ShowUnitWorldUI(unit);
            }
        }
    }

    public UnitWorldUI GetUnitWorldUIFromPool()
    {
        for (int i = 0; i < unitWorldUIList.Count; i++)
        {
            if (unitWorldUIList[i].gameObject.activeSelf == false)
                return unitWorldUIList[i];
        }

        return CreateNewUnitWorldUI();
    }

    UnitWorldUI CreateNewUnitWorldUI()
    {
        UnitWorldUI newUnitWorldUI = Instantiate(unitWorldUIPrefab, transform).GetComponent<UnitWorldUI>();
        unitWorldUIList.Add(newUnitWorldUI);
        return newUnitWorldUI;
    }

    void ShowAllUnitWorldUI()
    {
        DisableAllUnitWorldUI();
        for (int i = 0; i < UnitManager.Instance.UnitsList().Count; i++)
        {
            ShowUnitWorldUI(UnitManager.Instance.UnitsList()[i]);
        }
    }

    void ShowUnitWorldUI(Unit unit)
    {
        for (int i = 0; i < unitWorldUIList.Count; i++)
        {
            if (unitWorldUIList[i].Unit() == unit)
                return;
        }

        UnitWorldUI unitWorldUI = GetUnitWorldUIFromPool();
        unitWorldUI.SetUnit(unit);
        unitWorldUI.gameObject.SetActive(true);
    }

    void DisableAllUnitWorldUI()
    {
        for (int i = 0; i < unitWorldUIList.Count; i++)
        {
            if (UnitActionSystem.Instance.SelectedUnit() != unitWorldUIList[i].Unit())
                DisableUnitWorldUI(unitWorldUIList[i]);
        }
    }

    void DisableUnitWorldUI(UnitWorldUI unitWorldUI)
    {
        if (unitWorldUI.gameObject.activeSelf)
        {
            unitWorldUI.SetUnit(null);
            unitWorldUI.gameObject.SetActive(false);
        }
    }

    void DisableUnitWorldUI(Unit unit)
    {
        if (unit == null)
            return;

        for (int i = 0; i < unitWorldUIList.Count; i++)
        {
            if (unitWorldUIList[i].Unit() == unit)
            {
                DisableUnitWorldUI(unitWorldUIList[i]);
                break;
            }
        }
    }

    public void SetShowAll(bool showAll)
    {
        this.showAll = showAll;
        if (showAll)
            ShowAllUnitWorldUI();
        else
            DisableAllUnitWorldUI();
    }

    public void ToggleShowAll()
    {
        showAll = !showAll;
        if (showAll)
            ShowAllUnitWorldUI();
        else
            DisableAllUnitWorldUI();
    }

    void UnitWorldUIManager_OnSelectedUnitChanged(object sender, EventArgs e)
    {
        if (showAll == false)
        {
            DisableUnitWorldUI(UnitActionSystem.Instance.PreviousSelectedUnit());

            Unit selectedUnit = UnitActionSystem.Instance.SelectedUnit();
            if (selectedUnit != null)
                ShowUnitWorldUI(selectedUnit);
        }
    }
}
