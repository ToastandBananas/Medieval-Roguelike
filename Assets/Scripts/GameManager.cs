using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    UnitManager playerManager;
    Unit player;

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

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Unit>();
    }

    public UnitManager PlayerManager() => playerManager;

    public Unit Player() => player;
}
