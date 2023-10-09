using System.Collections.Generic;
using UnityEngine;

namespace UnitSystem
{
    public enum Faction
    {
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
        [SerializeField] List<Faction> alliedFactions = new List<Faction>();
        [SerializeField] List<Faction> enemyFactions = new List<Faction>();

        public bool IsAlly(Unit unitToCheck)
        {
            Faction unitsFaction = unitToCheck.alliance.CurrentFaction();
            if (unitsFaction == currentFaction || alliedFactions.Contains(unitsFaction) || unitToCheck.alliance.alliedFactions.Contains(currentFaction))
                return true;
            return false;
        }

        public bool IsEnemy(Unit unitToCheck)
        {
            Faction unitsFaction = unitToCheck.alliance.CurrentFaction();
            if (enemyFactions.Contains(unitsFaction) || unitToCheck.alliance.enemyFactions.Contains(currentFaction))
                return true;
            return false;
        }

        public bool IsNeutral(Unit unitToCheck)
        {
            if (IsAlly(unitToCheck) == false && IsEnemy(unitToCheck) == false)
                return true;
            return false;
        }

        public void AddEnemy(Unit newEnemy)
        {
            Faction enemyFaction = newEnemy.alliance.CurrentFaction();
            RemoveAlly(newEnemy);
            if (enemyFactions.Contains(enemyFaction) == false)
                enemyFactions.Add(enemyFaction);
        }

        public void RemoveEnemy(Unit enemy)
        {
            Faction enemyFaction = enemy.alliance.CurrentFaction();
            if (enemyFactions.Contains(enemyFaction))
                enemyFactions.Remove(enemyFaction);
        }

        public void AddAlly(Unit newAlly)
        {
            Faction allyFaction = newAlly.alliance.CurrentFaction();
            RemoveEnemy(newAlly);
            if (alliedFactions.Contains(allyFaction) == false)
                alliedFactions.Add(allyFaction);
        }

        public void RemoveAlly(Unit ally)
        {
            Faction allyFaction = ally.alliance.CurrentFaction();
            if (alliedFactions.Contains(allyFaction))
                alliedFactions.Remove(allyFaction);
        }

        public bool IsInPlayerFaction() => currentFaction == Faction.Player;

        public Faction CurrentFaction() => currentFaction;

        public List<Faction> AlliedFactions() => alliedFactions;

        public List<Faction> EnemyFactions() => enemyFactions;
    }
}
