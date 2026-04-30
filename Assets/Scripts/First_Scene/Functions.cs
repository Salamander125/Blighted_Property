using UnityEngine;
using UnityEngine.SceneManagement;

public class Functions : MonoBehaviour
{
   public void ButtonInicio()
   {
        SceneManager.LoadScene("Character_selector");
   }

    public void ButtonLoad()
    {
        SceneManager.LoadScene("LoadMatch_Scene");
    }
}
