using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

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
