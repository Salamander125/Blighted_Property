using UnityEngine;
using TMPro;

public class AbilityPreviewPanel : MonoBehaviour
{
    [Header("Textos de UI")]
    [SerializeField] private TextMeshProUGUI dañoText;
    [SerializeField] private TextMeshProUGUI manaText; 
    [SerializeField] private TextMeshProUGUI empujeText;
    [SerializeField] private TextMeshProUGUI estadoText;

    public void MostrarInfo(Ability skill, int dañoBasico)
    {
        gameObject.SetActive(true);

        if (skill != null)
        {
            dañoText.text = $"Daño: <color=#FF4444>{skill.damage}</color>";
            manaText.text =  $"Coste: <color=#44AAFF>{skill.manaCost} PM</color>";

            if (skill.moveOffset != Vector2Int.zero)
                empujeText.text = $"Empuje: X({skill.moveOffset.x}) Y({skill.moveOffset.y})";
            else
                empujeText.text = "Empuje: <color=#888888>Ninguno</color>";

            if (skill.aplicaEstado)
                estadoText.text = $"Aplica: <color=#FFDD44>{TraducirEstado(skill.tipoEstado)}</color> ({skill.duracionEstado} turnos)";
            else
                estadoText.text = "Aplica: <color=#888888>Ningún estado</color>";
        }
        else
        {
            // Si skill es null, significa que estamos usando el Ataque Básico normal
            dañoText.text = "Ataque Básico";
            manaText.text = $"Daño: <color=#FF4444>{dañoBasico}</color>  |  Coste: <color=#44AAFF>0 PM</color>";
            empujeText.text = "Empuje: <color=#888888>Ninguno</color>";
            estadoText.text = "Aplica: <color=#888888>Ningún estado</color>";
        }
    }

    public void Ocultar()
    {
        gameObject.SetActive(false);
    }

    private string TraducirEstado(State.StateType estado)
    {
        switch (estado)
        {
            // DoTs
            case State.StateType.Quemadura: return "Quemadura";
            case State.StateType.Veneno: return "Veneno";
            case State.StateType.Radiacion: return "Radiación"; 
            case State.StateType.Hemorragia: return "Hemorragia";

            // HoTs
            case State.StateType.Vitalidad: return "Vitalidad";
            case State.StateType.Lucidez: return "Lucidez";

            // Buffs
            case State.StateType.Prisa: return "Prisa";
            case State.StateType.Certeza: return "Certeza";
            case State.StateType.Coraza: return "Coraza";
            case State.StateType.Furia: return "Furia";
            case State.StateType.Espejismo: return "Espejismo";
            case State.StateType.Sifon: return "Sifón";
            case State.StateType.Baluarte: return "Baluarte";

            // Debuffs & CC
            case State.StateType.Fractura: return "Fractura";
            case State.StateType.Pesadez: return "Pesadez";
            case State.StateType.Ceguera: return "Ceguera";
            case State.StateType.Fragilidad: return "Fragilidad";
            case State.StateType.Fatiga: return "Fatiga";
            case State.StateType.Silencio: return "Silencio";
            case State.StateType.Sueno: return "Sueño";
            case State.StateType.Escarcha: return "Escarcha";
            case State.StateType.Cepo: return "Cepo";

            default: return estado.ToString();
        }
    }
}