using System;

public enum BuffType
{
    MaxLife, MaxMana, Attack, Speed, Defense, CriticalChance, Evasion,
    HealCurrentLife, RestoreCurrentMana,
    BonusLifeRegenPerFloor, BonusManaRegenPerFloor
}

public enum CardRarity { None, Normal, Especial, Epica, Mitica }

[Serializable]
public class CardModel
{
    public int id;
    public CardRarity rarity;
    public string description;
    public BuffType buffType;
    public float amount;
    public bool appliesState;
    public State.StateType stateToApply;
    public int stateDuration;
    public int stateIntensity;
}

// Clase envoltorio obligatoria para que JsonUtility pueda leer la lista de la API
[Serializable]
public class CardListWrapper
{
    public CardModel[] cards;
}