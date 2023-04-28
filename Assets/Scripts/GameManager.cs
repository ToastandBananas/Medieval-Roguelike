using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static float dualWieldPrimaryEfficiency = 0.8f;
    public static float dualWieldSecondaryEfficiency = 0.6f;

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
    }

    void Start()
    {
        //QualitySettings.vSyncCount = 0;
        //Application.targetFrameRate = 120;
    }
}
