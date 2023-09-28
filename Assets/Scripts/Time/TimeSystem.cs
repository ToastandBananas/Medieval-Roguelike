using UnityEngine;

public class TimeSystem : MonoBehaviour
{
    public static int currentHour = 8;
    public static int currentMinute;
    public static int currentSecond;

    /// <summary>When calculating a Unit's MaxAP in Stats.cs, their BaseAP is multiplied by this number before calculating additional AP from a Unit's Speed. This is also how much time passes when each full "Turn" is complete.</summary>
    public const int defaultTimeTickInSeconds = 3;

    #region Singleton
    public static TimeSystem Instance;

    void Awake()
    {
        if (Instance != null)
        {
            if (Instance != this)
            {
                Debug.LogWarning("More than one Instance of TimeSystem. Fix me!");
                Destroy(gameObject);
            }
        }
        else
            Instance = this;
    }
    #endregion

    public static void IncreaseTime()
    {
        IncreaseCurrentSecond(defaultTimeTickInSeconds);
        // LogTime();
    }

    static void IncreaseCurrentSecond(int seconds)
    {
        currentSecond += seconds;
        if (currentSecond >= 60)
        {
            int minuteProgress = Mathf.FloorToInt(currentSecond / 60);
            IncreaseCurrentMinute(minuteProgress);
            currentSecond = currentSecond % 60;
        }
    }

    static void IncreaseCurrentMinute(int minutes)
    {
        currentMinute += minutes;
        if (currentMinute >= 60)
        {
            int hourProgress = Mathf.FloorToInt(currentMinute / 60);
            IncreaseCurrentHour(hourProgress);
            currentMinute = currentMinute % 60;
        }
    }

    static void IncreaseCurrentHour(int hours)
    {
        currentHour += hours;
        if (currentHour >= 24)
        {
            int dayProgress = Mathf.FloorToInt(currentHour / 24);
            IncreaseCurrentDay(dayProgress);
            currentHour = currentHour % 24;
        }
    }

    static void IncreaseCurrentDay(int days)
    {
        Debug.Log("It's a new day!");
    }

    public static Vector3 GetCurrentTime()
    {
        return new Vector3(currentHour, currentMinute, currentSecond);
    }

    public static int GetTotalSeconds(Vector3Int timeAmount)
    {
        return (timeAmount.x * 60 * 60) + (timeAmount.y * 60) + timeAmount.z;
    }

    public static void LogTime()
    {
        Debug.Log("Time: " + currentHour + ":" + currentMinute + ":" + currentSecond);
    }
}
