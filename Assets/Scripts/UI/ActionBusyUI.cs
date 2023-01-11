using UnityEngine;

public class ActionBusyUI : MonoBehaviour
{
    void Start()
    {
        UnitActionSystem.Instance.OnBusyChanged += UnitActionSystem_OnBusyChanged;
        Hide();
    }

    void Show()
    {
        gameObject.SetActive(true);
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }

    void UnitActionSystem_OnBusyChanged(object sender, bool isBusy)
    {
        if (isBusy)
            Show();
        else
            Hide();
    }
}
