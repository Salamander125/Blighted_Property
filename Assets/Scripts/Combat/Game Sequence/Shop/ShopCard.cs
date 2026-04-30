using UnityEngine;
using TMPro;

public class ShopCard : MonoBehaviour
{
    [HideInInspector] public CardModel data; 
    [HideInInspector] public int cost;
    public bool isSelected = false;

    [Header("Referencias Únicas")]
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;
    public SpriteRenderer backgroundRenderer;

    public void SetupCard(CardModel newData, Sprite backgroundSprite)
    {
        data = newData;

        if (backgroundRenderer) backgroundRenderer.sprite = backgroundSprite;
        if (descriptionText) descriptionText.text = data.description;

        switch (data.rarity)
        {
            case CardRarity.Normal: cost = 60; break;
            case CardRarity.Especial: cost = 80; break;
            case CardRarity.Epica: cost = 120; break;
            case CardRarity.Mitica: cost = 150; break;
        }

        if (costText) costText.text = cost + " G";
    }

    public void ToggleSelection()
    {
        isSelected = !isSelected;
        backgroundRenderer.color = isSelected ? new Color(0.7f, 1f, 0.7f) : Color.white;
    }
}   