using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static Canvas Canvas { get; private set; }

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

        Canvas = FindObjectOfType<Canvas>();
    }

    void Start()
    {
        //QualitySettings.vSyncCount = 0;
        //Application.targetFrameRate = 120;
    }
}
