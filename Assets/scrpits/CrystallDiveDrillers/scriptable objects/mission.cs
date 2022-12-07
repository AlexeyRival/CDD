using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ШТУКИ/тип миссии")]
public class mission : ScriptableObject
{
    public string missiontypename;
    public int bonus;
    public Sprite icon;
    public missionCategory category;
    public enum missionCategory { 
        Обычная,
        Элитная,
        Колониальная,
        Лунарная,
        Стелларная,
        Сверхстелларная,
        Пустотная
    }
}
