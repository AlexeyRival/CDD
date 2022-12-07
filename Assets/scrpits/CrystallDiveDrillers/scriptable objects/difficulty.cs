using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ШТУКИ/сложность")]
public class difficulty : ScriptableObject
{
    public string difname;
    public int bonus;
    public float BugsAgression = 1f;
    public float SwarmmeterMultiplier = 1f;
    public float TimerMultiplier = 1f;
    public float OpressorSpawnChance;
    public Sprite icon;
}   