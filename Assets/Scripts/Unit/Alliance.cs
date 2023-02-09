using UnityEngine;

public enum Faction { 
    Player = 0,
    Military = 10,
    Bandits = 20,
    Undead = 30,
    Goblins = 40,
    PredatorAnimal = 50,
    PreyAnimal = 51
}

public class Alliance : MonoBehaviour
{
    [SerializeField] Faction currentFaction;
    [SerializeField] Faction[] alliedFactions;
    [SerializeField] Faction[] enemyFactions;

    public bool IsAlly(Unit unitToCheck)
    {
        Faction unitsFaction = unitToCheck.alliance.CurrentFaction();
        if (unitsFaction == currentFaction)
            return true;

        for (int i = 0; i < alliedFactions.Length; i++)
        {
            if (unitsFaction == alliedFactions[i])
                return true;
        }

        return false;
    }

    public bool IsEnemy(Unit unitToCheck)
    {
        Faction unitsFaction = unitToCheck.alliance.CurrentFaction();
        for (int i = 0; i < enemyFactions.Length; i++)
        {
            if (unitsFaction == enemyFactions[i])
                return true;
        }

        for (int i = 0; i < unitToCheck.alliance.enemyFactions.Length; i++)
        {
            if (currentFaction == unitToCheck.alliance.enemyFactions[i])
                return true;
        }

        return false;
    }

    public bool IsNeutral(Unit unitToCheck)
    {
        if (IsAlly(unitToCheck) == false && IsEnemy(unitToCheck) == false)
            return true;
        return false;
    }

    public bool IsPlayer() => currentFaction == Faction.Player;

    public Faction CurrentFaction() => currentFaction; 

    public Faction[] GetAlliedFactions() => alliedFactions; 

    public Faction[] GetEnemyFactions() => enemyFactions; 
}
