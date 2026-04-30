using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager_Combat : MonoBehaviour
{
    public static UIManager_Combat instance;

    [Header("Botones Únicos")]
    public Button attackBtn;
    public Button abilityBtn;

    public GameObject panelbuttons;
    public AbilityPreviewPanel abilityPreviewPanel;
    public AbilityMenuUI abilityMenuUI;

    public Combat_Controller[] slots; // Aquí están tus 3 barras de vida/mana

    [Header("Economía Visual")]
    [SerializeField] private TextMeshProUGUI currentGoldText;
    private int visualGold = 0;

    private Character_Controller activeCharacter;

    private void Awake() => instance = this;

    private void Start()
    {
        attackBtn.onClick.AddListener(OnAttackClicked);
        abilityBtn.onClick.AddListener(OnAbilityClicked);
    }

    public void AsignarSlotAPersonaje(GameObject personaje, int indiceSlot)
    {
        if (indiceSlot < 0 || indiceSlot >= slots.Length) return;

        // Buscamos los scripts que necesitan el Combat_Controller
        var character = personaje.GetComponent<Character>();
        var controller = personaje.GetComponent<Character_Controller>();
        var entity = personaje.GetComponent<Entity>();

        // Les entregamos su slot correspondiente
        if (character != null) character.combatController = slots[indiceSlot];
        if (controller != null) controller.combat = slots[indiceSlot];
        if (entity != null) entity.combat = slots[indiceSlot];

        Debug.Log($"<color=green>Slot {indiceSlot + 1} asignado a {personaje.name}</color>");
    }

    public void SetActiveCharacter(Character_Controller character)
    {
        activeCharacter = character;
        UpdateButtonsState(character != null);
    }

    public void UpdateButtonsState(bool interactable)
    {
        attackBtn.interactable = interactable;
        abilityBtn.interactable = interactable;
    }

    private void OnAttackClicked() => activeCharacter?.ExecuteAttack();
    private void OnAbilityClicked() => activeCharacter?.ExecuteAbilityMenu();

    public void UpdateGoldUI(int targetTotalGold)
    {
        if (currentGoldText == null) return;
        StopCoroutine("AnimateGold");
        StartCoroutine("AnimateGold", targetTotalGold);
    }

    private IEnumerator AnimateGold(int target)
    {
        float duration = 0.8f;
        float elapsed = 0;
        int startValue = visualGold;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            visualGold = (int)Mathf.Lerp(startValue, target, elapsed / duration);
            currentGoldText.text = visualGold.ToString();
            yield return null;
        }
        visualGold = target;
        currentGoldText.text = visualGold.ToString();
    }
}