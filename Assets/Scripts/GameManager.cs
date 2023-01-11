using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    UnitManager playerManager;

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

    public UnitManager PlayerManager() => playerManager;
}
