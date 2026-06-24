using UnityEngine;
using System.Collections;

public class MenuInicioNegro : MonoBehaviour
{
    public GameObject pantallaDeCarga;

    void Start()
    {
        Debug.Log("Script iniciado, esperando 7 segundos...");
        StartCoroutine(EsperaYApaga());
    }

    IEnumerator EsperaYApaga()
    {
        yield return new WaitForSeconds(5f);

        if (pantallaDeCarga != null)
        {
            pantallaDeCarga.SetActive(false); 
            Debug.Log("Pantalla de carga desactivada.");
        }
    }
}