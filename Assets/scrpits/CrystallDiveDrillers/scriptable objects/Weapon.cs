using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ШТУКИ/БАБАХА")]
public class Weapon : ScriptableObject
{
    public string weaponname;
    public Sprite icon;
    public float firerate;
    public int dmg;
    public int ammo;
    public int maxammo;
    public float reloadtime;
    public int sound;
    public int reloadsound;
    public float recoil;
    public bool haveRotor;
    public bool haveScope;
    public bool haveProjectile;
    public GameObject projectile;
    public bool isRailgun;
    public int railStreakCount = 5;
    public bool isChain;
    public int chaincount = 5;
    public float projectilespeed;
    public float projectileLifeTime=30f;
    public int firespershoot;
    public GameObject firesplash;
    public GameObject bulletmark;
}
