using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [HideInInspector] public List<Unit> npcs = new List<Unit>();
    [HideInInspector] public int npcsFinishedTakingTurnCount;

    Unit activeUnit;

    GameManager gm;

    #region Singleton
    public static TurnManager Instance;

    void Awake()
    {
        if (Instance != null)
        {
            if (Instance != this)
            {
                Debug.LogWarning("More than one Instance of TurnManager. Fix me!");
                Destroy(gameObject);
            }
        }
        else
            Instance = this;
    }
    #endregion

    void Start()
    {
        gm = GameManager.Instance;

        activeUnit = GameObject.FindGameObjectWithTag("Player").GetComponent<Unit>();
    }

    public IEnumerator FinishTurn(Unit unit)
    {
        yield return null;
    }

    public Unit ActiveUnit() => activeUnit;

    public bool IsPlayerTurn() => activeUnit == gm.Player();
}
