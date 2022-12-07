using UnityEngine;

[CreateAssetMenu(menuName = "aaaaAAAAAaaaaAAA????")]
public class Resource : ScriptableObject
{
    [Tooltip("Название")]
    public string materialName;
    [Tooltip("ID")]
    public int id;
    [Tooltip("Цвет ресурса")]
    public Color color;
    [Tooltip("Иконка")]
    public Texture2D icon;
    [Tooltip("Префаб руды")]
    public GameObject orePrefab;
    [Tooltip("Маска рудного следа")]
    public GameObject maskPrefab;
    [Tooltip("Префаб предмета")]
    public GameObject itemPrefab;
    [Tooltip("В сумку влезает")]
    public int maxInBag;
    [Tooltip("В одной жиле")]
    public int maxInVein;
    [Tooltip("В каком радиусе ресурс красит почву вокруг себя")]
    public int brushRadius;

    public void SpawnItem(Vector3 position) {
        Instantiate(itemPrefab, position, Quaternion.identity).name = id+"";
    }
}
