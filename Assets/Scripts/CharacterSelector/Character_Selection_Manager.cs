using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class Character_Selection_Manager : MonoBehaviour
{
    [Header("Base de Datos de Personajes")]
    public CharacterDataSO[] availableCharacters; 

    [Header("Botones de Personajes")]
    public Button[] characterButtons;
    public TextMeshProUGUI[] orderTexts; // Los números que aparecen arriba
    
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

        // 2. Confirmar selección (Espacio o Enter)
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            SelectCharacter(currentIndex);
            audioSource.PlayOneShot(confirmAudio);
        }

        // 3. Deshacer última selección (Opcional, con Escape o B)
        if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Escape))
        {
            DeshacerSeleccion();
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

    async void SelectCharacter(int index)
    {
        // Si ya lo elegimos, no hacemos nada (o lo quitamos)
        if (selectedIndices.Contains(index)) return;

        if (selectedIndices.Count < 3)
        {
            selectedIndices.Add(index);
            ActualizarVisualizacion();
            
            if (selectedIndices.Count == 3)
            {
                audioSource.PlayOneShot(SelectionAudio);
                await Task.Delay((int)(SelectionAudio.length * 1000));
                Debug.Log("¡Equipo completo! Preparando combate...");
                IniciarCombate();
            }
        }
    }

    void ActualizarVisualizacion()
    {
        for (int i = 0; i < characterButtons.Length; i++)
        {
            // Resaltar el botón que tenemos enfocado actualmente (escala o color)
            float scale = (i == currentIndex) ? 0.50f : 0.46f;
            characterButtons[i].transform.localScale = Vector3.one * scale;

            // Mostrar el número de orden si el personaje ha sido elegido
            int order = selectedIndices.IndexOf(i);
            if (order != -1)
            {
                orderTexts[i].text = (order + 1).ToString();
                orderTexts[i].gameObject.SetActive(true);
                // Opcional: Oscurecer el botón para indicar que ya está elegido
                characterButtons[i].interactable = false;
            }
            else
            {
                orderTexts[i].gameObject.SetActive(false);
                characterButtons[i].interactable = true;
            }
        }
    }

    void DeshacerSeleccion()
    {
        if (selectedIndices.Count > 0)
        {
            selectedIndices.RemoveAt(selectedIndices.Count - 1);
            ActualizarVisualizacion();
        }
    }

    void IniciarCombate()
    {
        // 1. Limpiamos la lista estática y la partida cargada por seguridad
        PlayerSelectionData.ChosenCharacters.Clear();
        PlayerSelectionData.PartidaCargada = null; // <--- SEGURO AÑADIDO

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