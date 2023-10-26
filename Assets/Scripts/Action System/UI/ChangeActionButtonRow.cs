using UnityEngine;
using UnityEngine.UI;

public class ChangeActionButtonRow : MonoBehaviour
{
    [SerializeField] Button upButton;
    [SerializeField] Button downButton;
    [SerializeField] RectTransform rowParentRectTransform;
    [SerializeField] RectTransform rowRectTransform;

    readonly int rowAdjustAmount = 64;
    readonly int padding;

    public void ActivateButtons()
    {
        upButton.interactable = true;
        upButton.transform.GetChild(0).gameObject.SetActive(true);

        downButton.interactable = true;
        downButton.transform.GetChild(0).gameObject.SetActive(true);
    }

    public void DeactivateButtons()
    {
        upButton.interactable = false;
        upButton.transform.GetChild(0).gameObject.SetActive(false);

        downButton.interactable = false;
        downButton.transform.GetChild(0).gameObject.SetActive(false);
    }

    public void IncreaseRow()
    {
        if (rowRectTransform.offsetMax.y > 0)
            rowRectTransform.offsetMax = new Vector2(0, rowRectTransform.offsetMax.y - rowAdjustAmount);
    }

    public void DecreaseRow()
    {
        if (rowRectTransform.offsetMax.y < (rowAdjustAmount * (3 - (rowParentRectTransform.sizeDelta.y - padding) / rowAdjustAmount)))
            rowRectTransform.offsetMax = new Vector2(0, rowRectTransform.offsetMax.y + rowAdjustAmount);
    }
}
