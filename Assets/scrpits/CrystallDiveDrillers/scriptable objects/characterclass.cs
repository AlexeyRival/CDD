using UnityEngine;

[CreateAssetMenu(menuName = "ДВОРФ?!")]
public class characterclass : ScriptableObject
{
    public string classname;
    public Sprite icon;
    public int id;
    public float speed;
    public AudioClip greetings;
}
