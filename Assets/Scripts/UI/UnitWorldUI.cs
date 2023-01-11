using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class UnitWorldUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI healthText;
    [SerializeField] Image healthBarImage;

    Unit unit;

    void Start()
    {
        UpdateHealthText();
        UpdateHealthBar();
    }

    void LateUpdate()
    {
        SetPositionToUnit();
    }

    void SetPositionToUnit()
    {
        float positionOffset = 0.65f;
        transform.position = Camera.main.WorldToScreenPoint(unit.transform.position + (Vector3.up * positionOffset));
    }

    public void SetUnit(Unit unit)
    {
        if (unit == null && this.unit != null)
            this.unit.HealthSystem().OnHealthChanged -= HealthSystem_OnHealthChanged;
        else if (unit != null)
            unit.HealthSystem().OnHealthChanged += HealthSystem_OnHealthChanged;

        this.unit = unit;

        if (unit != null)
        {
            UpdateHealthBar();
            UpdateHealthText();
        }
    }

    public Unit Unit() => unit;

    void UpdateHealthText()
    {
        healthText.text = unit.HealthSystem().CurrentHealth().ToString() + " / " + unit.HealthSystem().MaxHealth().ToString();
    }

    void UpdateHealthBar()
    {
        healthBarImage.fillAmount = unit.HealthSystem().CurrentHealthNormalized();
    }

    void HealthSystem_OnHealthChanged(object sender, EventArgs e)
    {
        UpdateHealthText();
        UpdateHealthBar();
    }
}
