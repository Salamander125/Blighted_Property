using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections; // Necesario para las Corrutinas

public class Character_Selection_Manager : MonoBehaviour
{
    [Header("Base de Datos de Personajes")]
    public CharacterDataSO[] availableCharacters;

    [Header("Botones de Personajes")]
    public Button[] characterButtons;
    public TextMeshProUGUI[] orderTexts; // Los números que aparecen arriba

    [Header("Interfaz de Confirmación")]
    public Button confirmButton; // <--- Asigna tu nuevo botón de "Empezar Combate" aquí

    [Header("Ajustes de Selección")]
    private int currentIndex = 0;
    private List<int> selectedIndices = new List<int>(); // Guarda el orden: [mago, guerrero, etc]

    [Header("Audios")]
    public AudioClip confirmAudio;
    public AudioClip navigateAudio;
    public AudioClip SelectionAudio;
    public AudioSource audioSource;

    private void Start()
    {
        // Configuramos el botón de confirmar al inicio
        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            // Asignamos la función al botón por código (así no tienes que hacerlo en el Inspector)
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }

        ActualizarVisualizacion();
    }

    private void Update()
    {
        // 1. Navegación horizontal
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveSelection(-1);
            audioSource.PlayOneShot(navigateAudio);
        }
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveSelection(1);
            audioSource.PlayOneShot(navigateAudio);
        }

        // 2. Seleccionar o Deseleccionar (Espacio o Enter)
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            ToggleCharacterSelection(currentIndex);
        }
    }

    void MoveSelection(int direction)
    {
        currentIndex += direction;
        // Clamp para que no se salga del array
        if (currentIndex < 0) currentIndex = characterButtons.Length - 1;
        if (currentIndex >= characterButtons.Length) currentIndex = 0;

        ActualizarVisualizacion();
    }

    // Nueva función que hace de interruptor
    void ToggleCharacterSelection(int index)
    {
        // Si YA está seleccionado, lo quitamos (Deseleccionar)
        if (selectedIndices.Contains(index))
        {
            selectedIndices.Remove(index);
            // Opcional: audioSource.PlayOneShot(sonidoDeCancelar);
        }
        // Si NO está seleccionado y aún nos caben personajes, lo añadimos
        else if (selectedIndices.Count < 3)
        {
            selectedIndices.Add(index);
            audioSource.PlayOneShot(confirmAudio);
        }

        ActualizarVisualizacion();
    }

    void ActualizarVisualizacion()
    {
        for (int i = 0; i < characterButtons.Length; i++)
        {
            // Resaltar el botón que tenemos enfocado actualmente
            float scale = (i == currentIndex) ? 0.50f : 0.46f;
            characterButtons[i].transform.localScale = Vector3.one * scale;

            // Mostrar el número de orden si el personaje ha sido elegido
            int order = selectedIndices.IndexOf(i);
            if (order != -1)
            {
                orderTexts[i].text = (order + 1).ToString();
                orderTexts[i].gameObject.SetActive(true);
                // NOTA: Eliminado el interactable = false para permitir deseleccionar
            }
            else
            {
                orderTexts[i].gameObject.SetActive(false);
            }
        }

        // Habilitar o deshabilitar el botón de continuar según la cantidad elegida
        if (confirmButton != null)
        {
            confirmButton.interactable = (selectedIndices.Count == 3);
        }
    }

    // Esta función la llama el botón de Confirmar
    public void OnConfirmButtonClicked()
    {
        if (selectedIndices.Count == 3)
        {
            StartCoroutine(TransitionToCombatRoutine());
        }
    }

    // Corrutina segura para WebGL
    IEnumerator TransitionToCombatRoutine()
    {
        // Desactivamos el botón para evitar dobles clics
        if (confirmButton != null) confirmButton.interactable = false;

        if (SelectionAudio != null)
        {
            audioSource.PlayOneShot(SelectionAudio);
            yield return new WaitForSeconds(SelectionAudio.length);
        }

        Debug.Log("¡Equipo completo! Preparando combate...");
        IniciarCombate();
    }

    void IniciarCombate()
    {
        // 1. Limpiamos la lista estática y la partida cargada por seguridad
        PlayerSelectionData.ChosenCharacters.Clear();
        PlayerSelectionData.PartidaCargada = null;

        // 2. Traducimos los índices (0, 1, 2...) a los ScriptableObjects reales
        foreach (int index in selectedIndices)
        {
            CharacterDataSO data = availableCharacters[index];
            PlayerSelectionData.ChosenCharacters.Add(data);
        }

        // 3. Cargamos la escena de combate
        SceneManager.LoadScene("Combat_scene");
    }
}