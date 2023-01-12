using UnityEngine;

public class UnitActionSystem : MonoBehaviour
{
    private static UnitActionSystem Instance;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There's more than one UnitActionSystem! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void MoveUnit(Unit unit, GridPosition gridPosition)
    {

    }
}
