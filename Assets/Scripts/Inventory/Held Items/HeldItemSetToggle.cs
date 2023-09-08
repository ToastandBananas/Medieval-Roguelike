using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HeldItemSetToggle : MonoBehaviour, IPointerDownHandler
{
    [Header("Sprite Info")]
    [SerializeField] Image image;
    [SerializeField] Sprite inactiveSetSprite;
    [SerializeField] Sprite activeSetSprite;

    [Header("Held Item Set Info")]
    [SerializeField] HeldItemSetToggle otherHeldItemSetToggle;
    [SerializeField] HeldItemSet heldItemSetNumber;

    CharacterEquipment characterEquipment;

    void Start()
    {
        characterEquipment = UnitManager.Instance.player.CharacterEquipment();
        if (characterEquipment.currentHeldItemSet == heldItemSetNumber)
            image.sprite = activeSetSprite;
        else
            image.sprite = inactiveSetSprite;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (characterEquipment.currentHeldItemSet != heldItemSetNumber)
        {
            otherHeldItemSetToggle.SetSprite(inactiveSetSprite);
            SetSprite(activeSetSprite);
            characterEquipment.SetCurrentHeldItemSet(heldItemSetNumber);
            characterEquipment.ToggleHeldItemSet();
        }
    }

    public void SetSprite(Sprite sprite) => image.sprite = sprite;
}
