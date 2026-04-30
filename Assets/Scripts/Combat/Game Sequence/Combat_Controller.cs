using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Combat_Controller : MonoBehaviour
{
    [Header("Barras de Estado")]
    [SerializeField] Image redBar;
    [SerializeField] Image grayBar;
    [SerializeField] float animationDuration = 1.0f;

    [Header("Textos")]
    [SerializeField] private TextMeshProUGUI currentLifeText;
    [SerializeField] private TextMeshProUGUI currentManaText;

    [Header("Economía")]
    [SerializeField] private TextMeshProUGUI currentGoldText;
    private int currentVisualGold = 0; // Para la animación del número
    public int totalGold = 0; // El valor real guardado

    // Usamos corrutinas separadas para que la vida y el maná no se pisen
    private Coroutine lifeAnimation;
    private Coroutine manaAnimation;

    void Start()
    {
        // Al inicio, las barras suelen estar llenas o en su estado actual. 
        redBar.fillAmount = 1;
        grayBar.fillAmount = 1;
    }

    // =============================
    // GESTIÓN DE VIDA
    // =============================
    public void UpdateLifeUI(int life, float targetFill)
    {
        currentLifeText.text = life.ToString();

        if (lifeAnimation != null) StopCoroutine(lifeAnimation);
        lifeAnimation = StartCoroutine(AnimateLifeBar(targetFill));
    }

    private IEnumerator AnimateLifeBar(float target)
    {
        float startFill = redBar.fillAmount;
        float timeElapsed = 0;

        while (timeElapsed < animationDuration)
        {
            timeElapsed += Time.deltaTime;
            redBar.fillAmount = Mathf.Lerp(startFill, target, timeElapsed / animationDuration);
            yield return null;
        }

        redBar.fillAmount = target;
    }

    // =============================
    // GESTIÓN DE MANÁ (Barra Gris)
    // =============================
    public void UpdateManaUI(int mana, float targetFill)
    {
        currentManaText.text = mana.ToString();

        if (manaAnimation != null) StopCoroutine(manaAnimation);
        manaAnimation = StartCoroutine(AnimateManaBar(targetFill));
    }

    private IEnumerator AnimateManaBar(float target)
    {
        float startFill = grayBar.fillAmount;
        float timeElapsed = 0;

        while (timeElapsed < animationDuration)
        {
            timeElapsed += Time.deltaTime;
            grayBar.fillAmount = Mathf.Lerp(startFill, target, timeElapsed / animationDuration);
            yield return null;
        }

        grayBar.fillAmount = target;
    }

    // =============================
    // GESTIÓN DE ORO
    // =============================
    public void UpdateGoldUI(int amountToAdd)
    {
        totalGold += amountToAdd;
        // Detenemos si ya hay una animación de oro y empezamos la nueva
        StopCoroutine("AnimateGoldText");
        StartCoroutine(AnimateGoldText(totalGold));
    }

    private IEnumerator AnimateGoldText(int targetGold)
    {
        float timeElapsed = 0;
        float duration = 0.5f; // Animación rápida de medio segundo
        int startGold = currentVisualGold;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            // Va calculando el número intermedio
            currentVisualGold = (int)Mathf.Lerp(startGold, targetGold, timeElapsed / duration);
            currentGoldText.text = currentVisualGold.ToString();
            yield return null;
        }

        currentVisualGold = targetGold;
        currentGoldText.text = currentVisualGold.ToString();
    }
}