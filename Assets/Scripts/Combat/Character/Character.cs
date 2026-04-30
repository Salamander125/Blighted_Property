using UnityEngine;

public class Character : Entity
{ 

    public Combat_Controller combatController;
    protected override void Start()
    {
        base.Start();
        // Inicializamos la UI con los valores actuales al empezar
        UpdateUI();
    }

    // Sobrescribimos usando el nuevo sistema de críticos
    public override bool TakeDamage(int incomingDamage, bool isCritical = false)
    {
        // Guardamos si el golpe acertó o no
        bool hit = base.TakeDamage(incomingDamage, isCritical);

        // Actualizamos las barras de vida
        UpdateUI();

        // Devolvemos la respuesta para que la habilidad la lea
        return hit;
    }

    public override void ConsumMana(int manaAmount)
    {
        base.ConsumMana(manaAmount);
        UpdateUI();
    }

    // Actualiza la UI si nos curamos
    public override void HealCurrentLife(int amount)
    {
        base.HealCurrentLife(amount);
        UpdateUI();
    }

    // Actualiza la UI si recuperamos maná
    public override void RestoreCurrentMana(int amount)
    {
        base.RestoreCurrentMana(amount);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (combat != null)
        {
            // CÁLCULO DEL FILL: 
            // IMPORTANTE: Usamos MaxLife en lugar de life para que no se rompa 
            // si compramos aumentos de vida en la tienda.
            float lifeFill = (float)CurrentLife / MaxLife;
            combat.UpdateLifeUI(CurrentLife, lifeFill);

            float manaFill = (float)CurrentMana / MaxMana;
            combat.UpdateManaUI(CurrentMana, manaFill);
        }
        else
        {
            Debug.LogWarning($"{name}: ¡El Combat Controller no está asignado en el Inspector!");
        }
    }
}