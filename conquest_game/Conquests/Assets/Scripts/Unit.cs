using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Unit
{
    public int ID = -1;
    public string name 
    {
        get {return "Unit #" + ID.ToString();}
    }
    public int attack;
    public int defence;
    public int initiative;
    int health;
    public int maxHealth; 
    int cooldown;
    int cooldownCount;
    public bool dead;
    public bool attacking;
    public Owner owner;
    public Area area;

    public Unit(UnitSpawnInfo info, MapInfo map)
    {
        ID = info.ID;
        attack = info.attack;
        defence = info.defence;
        initiative = info.initiative;
        maxHealth = info.maxHealth;
        cooldown = info.cooldown;
        area = map.areas[info.strAreaID];
        owner = map.owners[info.strOwner];
        Reset();
    }

    public bool CheckAttack()
    {
        if (cooldownCount == 0)
        {
            return true;
        }
        else
        {
            cooldownCount--;
            return false;
        }
    }
    public void Reset()
    {
        health = maxHealth;
        cooldownCount = 0;
        dead = false;
        attacking = false;
    }

    public int Attack()
    {
        cooldownCount = cooldown;
        return attack;
    }

    public void TakeDamage(int d)
    {
        health -= d;
        dead = (health <= 0);
    }

    public Unit SelectTarget(List<Unit> candidates)
    {
        List<Unit> sorted = candidates.OrderBy(o => o.health).ToList();
        return sorted[0];
    }

    public void Die()
    {
        owner.RemoveUnit(this);
        area.RemoveUnit(this);
    }

    public void Move(Area destination)
    {
        area.RemoveUnit(this);
        destination.AddUnit(this);
        area = destination;
    }
    
}

public class UnitSpawnInfo
{
    public int ID = -1;
    public string name = "Unit Default";
    public int attack = 3;
    public int defence = 2;
    public int initiative = 1;
    public int maxHealth = 10; 
    public int cooldown = 3;
    public string strOwner;
    public string strAreaID;

    public UnitSpawnInfo()
    {

    }

    public UnitSpawnInfo(Area area)
    {
        strAreaID = area.ID;
        strOwner = area.owner.name;
    }
}
