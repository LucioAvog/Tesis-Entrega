using System.Collections; 
using UnityEngine;
using UnityEngine.SceneManagement;

public class CambioEscena : MonoBehaviour
{
    public void CargarEscena(string nombreEscena)
    {
        StartCoroutine(CambiarEscenaConTransicion(nombreEscena));
    }

    private IEnumerator CambiarEscenaConTransicion(string nombreEscena)
    {
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.FadeOut();
        }

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene(nombreEscena);
    }
}