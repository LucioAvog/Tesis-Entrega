using System.Collections; 
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void Jugar()
    {
        StartCoroutine(JugarConTransicion());
    }

    public void Salir()
    {
        StartCoroutine(SalirConTransicion());
    }

    private IEnumerator JugarConTransicion()
    {
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.FadeOut();
        }

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private IEnumerator SalirConTransicion()
    {
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.FadeOut();
        }

        yield return new WaitForSeconds(1f);

        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}