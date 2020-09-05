using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class BattleSimulator
{
    public static BattleResult SimulateBattle(BattleInfo info)
    {
        
        List<Unit> attackAlive = new List<Unit>(info.attackers);
        List<Unit> defendAlive = new List<Unit>(info.defenders);

        foreach (Unit unit in attackAlive) 
        {
            unit.attacking = true;
        }

        //Initial battle phase
        Debug.Log("INITAL BATTLE PHASE");
        int maxInit = attackAlive.Concat(defendAlive).Max(x => x.initiative);
        for (int i = 0; i < maxInit; i++)
        {
            List<Unit> qualifyingAttackers = attackAlive.Where(o => o.initiative == i).ToList();
            foreach (Unit attacker in qualifyingAttackers)
            {
                Unit target = attacker.SelectTarget(defendAlive);
                int dmg = attacker.Attack();
                target.TakeDamage(dmg);
                Debug.Log(attacker.name + " attacking " + target.name + " for " + dmg.ToString());
                if (target.dead)
                {
                    defendAlive.Remove(target);
                    Debug.Log(target.name + " is dead.");
                }
            }
            List<Unit> qualifyingDefenders = defendAlive.Where(o => o.initiative == i).ToList();
            foreach (Unit defender in qualifyingDefenders)
            {
                Unit target = defender.SelectTarget(attackAlive);
                int dmg = defender.Attack();
                target.TakeDamage(dmg);
                Debug.Log(defender.name + " attacking " + target.name + " for " + dmg.ToString());
                if (target.dead)
                {
                    attackAlive.Remove(target);
                    Debug.Log(target.name + " is dead.");
                }
            }
        }

        Debug.Log("MAIN BATTLE PHASE");
        //Main battle phase
        Queue<Unit> unitQueue = new Queue<Unit>();
        while (attackAlive.Count > 0 && defendAlive.Count > 0)
        {
            //If we've done a full cycle of attacks, reset to a random order
            if (unitQueue.Count == 0) {
                List<Unit> allAlive = attackAlive.Concat(defendAlive).ToList(); 
                Shuffle(allAlive);
                unitQueue = new Queue<Unit>(allAlive);
            }

            //Handle one attack
            Unit attacker = unitQueue.Dequeue();
            if (attacker.CheckAttack())
            {
                List<Unit> opponents = attacker.attacking ? defendAlive : attackAlive;
                int dmg = attacker.Attack();
                Unit target = attacker.SelectTarget(opponents);
                target.TakeDamage(dmg);
                Debug.Log(attacker.name + " attacking " + target.name + " for " + dmg.ToString());
                if (target.dead) 
                {
                    opponents.Remove(target);
                    Debug.Log(target.name + " is dead.");
                }
            }
        }

        Debug.Log("BATTLE COMPLETE.");
        Debug.Log("Remaining attackers: " + attackAlive.Count.ToString());
        Debug.Log("Remaining defenders: " + defendAlive.Count.ToString());

        Owner victor = defendAlive.Count > 0 ? info.area.owner : info.attackers[0].owner;

        return new BattleResult(info.attackers.Concat(info.defenders).ToList(), victor);
    }

    private static System.Random rng = new System.Random();  

    public static void Shuffle<T>(IList<T> list)  
    {  
        int n = list.Count;  
        while (n > 1) {  
            n--;  
            int k = rng.Next(n + 1);  
            T value = list[k];  
            list[k] = list[n];  
            list[n] = value;  
        }  
    }
}

public class BattleInfo
{
    public Area area;
    public List<Unit> attackers;
    public List<Unit> defenders;

    public BattleInfo(List<Unit> _attackers, List<Unit> _defenders, Area _area)
    {
        attackers = _attackers;
        defenders = _defenders;
        area = _area;
    }

}

public class BattleResult
{
    public List<Unit> units;
    public Owner victor;

    public BattleResult(List<Unit> _units, Owner _victor)
    {
        units = _units;
        victor = _victor;
    }
}




