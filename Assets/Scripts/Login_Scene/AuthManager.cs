using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.SceneManagement;

[System.Serializable]
public class AuthRequest
{
    public string Username;
    public string PasswordHash; // Se envía en plano, el backend hace el SHA256
}

[System.Serializable]
public class AuthResponse
{
    public int id;
    public string username;
    public string createdAt;
}

public class AuthManager : MonoBehaviour
{
    private string apiBaseUrl = "http://blightedproperty.somee.com/api/auth";

    [Header("Paneles Principales")]
    public GameObject panelLogin;
    public GameObject panelRegistro;

    [Header("UI - Inicio de Sesión")]
    public TMP_InputField loginUserInp;
    public TMP_InputField loginPassInp;

    [Header("UI - Registro")]
    public TMP_InputField regUserInp;
    public TMP_InputField regPassInp;
    public TMP_InputField regConfirmPassInp;

    [Header("Feedback Visual")]
    public TextMeshProUGUI statusText; // Texto para mostrar errores o aciertos (ponlo visible para ambos paneles)

    private void Start()
    {
        // 1. Configuramos los límites y censura visual de los inputs
        loginUserInp.characterLimit = 30;
        regUserInp.characterLimit = 30;

        loginPassInp.inputType = TMP_InputField.InputType.Password;
        regPassInp.inputType = TMP_InputField.InputType.Password;
        regConfirmPassInp.inputType = TMP_InputField.InputType.Password;

        // 2. Estado inicial de la escena
        MostrarPanelLogin();
    }

    // ==========================================
    // CONTROL DE PANELES
    // ==========================================
    public void MostrarPanelLogin()
    {
        panelRegistro.SetActive(false);
        panelLogin.SetActive(true);
        statusText.text = "";
    }

    public void MostrarPanelRegistro()
    {
        panelLogin.SetActive(false);
        panelRegistro.SetActive(true);
        statusText.text = "";
    }

    // ==========================================
    // VALIDACIÓN Y ENVÍO - LOGIN
    // ==========================================
    public void IntentarLogin()
    {
        string user = loginUserInp.text.Trim();
        string pass = loginPassInp.text;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            MostrarError("Por favor, rellena todos los campos.");
            return;
        }

        statusText.color = Color.yellow;
        statusText.text = "Conectando...";
        StartCoroutine(RutinaAuth("/login", user, pass));
    }

    // ==========================================
    // VALIDACIÓN Y ENVÍO - REGISTRO
    // ==========================================
    public void IntentarRegistro()
    {
        string user = regUserInp.text.Trim();
        string pass = regPassInp.text;
        string confirmPass = regConfirmPassInp.text;

        // 1. Campos vacíos
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(confirmPass))
        {
            MostrarError("Por favor, rellena todos los campos.");
            return;
        }

        // 2. Coincidencia de contraseñas
        if (pass != confirmPass)
        {
            MostrarError("Las contraseñas no coinciden.");
            return;
        }

        // 3. Validación estricta de la contraseña usando Regex
        // Reglas: Mínimo 8 caracteres, al menos 1 mayúscula, al menos 1 número.
        string patronRegex = @"^(?=.*[A-Z])(?=.*\d).{8,}$";
        if (!Regex.IsMatch(pass, patronRegex))
        {
            MostrarError("La contraseña debe tener mínimo 8 caracteres, 1 mayúscula y 1 número.");
            return;
        }

        statusText.color = Color.yellow;
        statusText.text = "Creando cuenta...";
        StartCoroutine(RutinaAuth("/register", user, pass));
    }

    // ==========================================
    // CONEXIÓN CON LA API (Sirve para ambos)
    // ==========================================
    private IEnumerator RutinaAuth(string endpoint, string username, string password)
    {
        AuthRequest req = new AuthRequest
        {
            Username = username,
            PasswordHash = password
        };

        string jsonPayload = JsonUtility.ToJson(req);

        using (UnityWebRequest www = new UnityWebRequest(apiBaseUrl + endpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AuthResponse res = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);

                statusText.color = Color.green;
                statusText.text = (endpoint == "/login") ? "¡Acceso concedido!" : "¡Cuenta creada con éxito!";

                // DNI del usuario para el SaveManager
                PlayerPrefs.SetInt("UserId", res.id);
                PlayerPrefs.SetString("Username", res.username);
                PlayerPrefs.Save();

                yield return new WaitForSeconds(1f);

                // Transición a la escena principal
                SceneManager.LoadScene("first_scene");
            }
            else
            {
                // Manejo de errores amigable
                if (www.responseCode == 401)
                {
                    MostrarError("Usuario o contraseña incorrectos.");
                }
                else if (www.responseCode == 409 || www.downloadHandler.text.Contains("UNIQUE constraint"))
                {
                    MostrarError("Ese nombre de usuario ya está en uso.");
                }
                else
                {
                    MostrarError("Error de conexión con el servidor.");
                    Debug.LogError(www.error + " - " + www.downloadHandler.text);
                }
            }
        }
    }

    private void MostrarError(string mensaje)
    {
        statusText.color = Color.red;
        statusText.text = mensaje;
    }
}