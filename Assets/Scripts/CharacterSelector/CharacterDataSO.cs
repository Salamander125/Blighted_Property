using UnityEngine;

[CreateAssetMenu(fileName = "NuevoPersonaje", menuName = "Sistema/Personaje")]
public class CharacterDataSO : ScriptableObject
{
    public string nombre;
    public GameObject prefab;   
}