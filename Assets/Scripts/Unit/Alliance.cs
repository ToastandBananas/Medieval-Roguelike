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

    public bool IsAlly(Faction factionToCheckAgainst)
    {
        if (factionToCheckAgainst == currentFaction)
            return true;

        for (int i = 0; i < alliedFactions.Length; i++)
        {
            if (factionToCheckAgainst == alliedFactions[i])
                return true;
        }

        return false;
    }

    public bool IsEnemy(Faction factionToCheckAgainst)
    {
        for (int i = 0; i < enemyFactions.Length; i++)
        {
            if (factionToCheckAgainst == enemyFactions[i])
                return true;
        }

        return false;
    }

    public bool IsNeutral(Faction factionToCheckAgainst)
    {
        if (IsAlly(factionToCheckAgainst) == false && IsEnemy(factionToCheckAgainst) == false)
            return true;
        return false;
    }

    public bool IsPlayer() => currentFaction == Faction.Player;

    public Faction CurrentFaction() => currentFaction; 

    public Faction[] GetAlliedFactions() => alliedFactions; 

    public Faction[] GetEnemyFactions() => enemyFactions; 
}
