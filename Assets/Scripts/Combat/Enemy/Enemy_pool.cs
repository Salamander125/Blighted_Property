using UnityEngine;

[System.Serializable]
public class Enemy_pool
{
    public string enemyName;
    public GameObject prefab;
    [Tooltip("¿Cuántas copias de este enemigo metemos en la bolsa del nivel?")]
    public int copiesInDeck = 1;
}