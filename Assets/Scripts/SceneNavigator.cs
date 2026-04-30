using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Home()
    {
        SceneManager.LoadScene("First_scene");
    }

    public void menulogin()
    {
        SceneManager.LoadScene("Login_scene");
    }
}
