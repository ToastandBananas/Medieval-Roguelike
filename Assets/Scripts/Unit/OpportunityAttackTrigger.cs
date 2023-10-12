using UnitSystem;
using UnityEngine;

public class OpportunityAttackTrigger : MonoBehaviour
{
    Unit myUnit;

    void Awake()
    {
        myUnit = GetComponentInParent<Unit>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Unit") || other.CompareTag("Player"))
        {
            if (other.transform == transform.parent)
                return;

            other.gameObject.GetComponent<Unit>().unitsWhoCouldOpportunityAttackMe.Add(myUnit);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Unit") || other.CompareTag("Player"))
            other.gameObject.GetComponent<Unit>().unitsWhoCouldOpportunityAttackMe.Remove(myUnit);
    }
}
