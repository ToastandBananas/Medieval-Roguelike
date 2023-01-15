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
                Debug.LogWarning("More than one Instance of GameManager. Fix me!");
                Destroy(gameObject);
            }
        }
        else
            Instance = this;

        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Unit>();
    }

    public UnitManager PlayerManager() => playerManager;

    public Unit Player() => player;
}
