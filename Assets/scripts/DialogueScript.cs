using System.Collections.Generic;
using UnityEngine;

public enum PanelType
{
    Panel1,
    Panel2,
    Panel3,
    Panel4
}

public enum ImagePosition
{
    Izquierda,
    Centro,
    Derecha,
    Personalizada
}

[System.Serializable]
public class Choice
{
    [TextArea(2, 5)]
    public string buttonText;
    public Sprite buttonBackground;
    public DialogueScript nextDialogue;

    [Header("Mecánica de Vidas")]
    public bool restarVida = false;
    public int cantidadVidaARestar = 1;
    public DialogueScript dialogoAlMorirSiPierde;

    [Header("Reinicio / Continuación")]
    public bool reiniciarEnLineaEspecifica = false;
    public int lineaDestino = 0;

    [Header("Condicionales de Banderas")]
    public string flagRequerida;
    public DialogueScript dialogoSiTieneFlag;

    [Header("Guardar Bandera")]
    public string guardarFlag;

    [Header("Cambio de Escena Directo")]
    public bool changeScene = false;
    public string sceneToLoad;
}

[System.Serializable]
public class Line
{
    public string speaker;
    [TextArea(3, 10)]
    public string text;

    [Header("Paneles de Diálogo")]
    public PanelType panelType = PanelType.Panel1;
    public PanelType choicePanelType = PanelType.Panel1;
    public bool ocultarPanelDialogo = false;

    [Header("Control de Interacción Global")]
    public bool bloquearOtrosBotones = false;

    [Header("Animación de Texto")]
    public bool usarTransicionTexto = false;
    public float duracionTransicion = 1f;

    [Header("Imagen Destacada / Ilustración en Pantalla")]
    public bool mostrarImagenPantalla = false;
    public Sprite imagenPantalla;
    public Vector2 posicionImagenPantalla = Vector2.zero;
    public Vector2 tamanoImagenPantalla = new Vector2(500, 500);

    [Header("Imágenes del Personaje (Hasta 3)")]
    public bool showImage = false;
    public Sprite characterImage;
    public ImagePosition imagePosition = ImagePosition.Izquierda;
    public Vector2 characterImagePosition;
    public Vector2 characterImageSize;

    public bool showImage2 = false;
    public Sprite characterImage2;
    public ImagePosition imagePosition2 = ImagePosition.Izquierda;
    public Vector2 characterImagePosition2;
    public Vector2 characterImageSize2;

    public bool showImage3 = false;
    public Sprite characterImage3;
    public ImagePosition imagePosition3 = ImagePosition.Izquierda;
    public Vector2 characterImagePosition3;
    public Vector2 characterImageSize3;

    [Header("Fondo")]
    public Sprite newBackground;

    [Header("Audio y Sonidos")]
    public bool playSound = false;
    public AudioClip soundClip;

    public bool playSFX = false;
    public AudioClip sfxClip;
    public float sfxDuration = 0f;

    [Header("Efectos Visuales")]
    public bool triggerShake = false;
    public float shakeDuration = 0.5f;
    public float shakeIntensity = 10f;

    [Header("Opciones de Elección")]
    public List<Choice> choices;

    [Header("Recompensa de Inventario")]
    public bool agregarItemAlInventario = false;
    [Tooltip("Arrastra aquí el ItemData que quieres añadir al inventario al mostrar esta línea.")]
    public ItemData itemAAgregar;

    [Header("Aparición / Desaparición Automática de Objetos")]
    [Tooltip("Arrastra aquí un objeto de la escena para ACTIVARLO automáticamente al salir esta línea de diálogo.")]
    public GameObject objetoAActivar;
    [Tooltip("Arrastra aquí un objeto de la escena para DESACTIVARLO automáticamente al salir esta línea de diálogo.")]
    public GameObject objetoADesactivar;

    [Header("Puentes de Interfaz")]
    public bool dispararCarruselPaneles = false;
}

[CreateAssetMenu(fileName = "NewDialogueScript", menuName = "Dialogue/DialogueScript")]
public class DialogueScript : ScriptableObject
{
    public List<Line> conversationLines;

    [Header("Siguiente Conversación Directa")]
    public DialogueScript nextConversation;
    public string flagRequeridaParaSiguiente;
    public DialogueScript conversationSiTieneFlag;

    [Header("Cambio de Escena al Finalizar")]
    public bool changeSceneOnEnd = false;
    public string sceneToLoadOnEnd;

    [Header("Punto de Guardado")]
    [Tooltip("Flag que se guarda al terminar este diálogo.")]
    public string flagAlFinalizar;

    [Header("Diálogo al perder la última vida en esta conversación")]
    public DialogueScript dialogoAlPerderUltimaVida;

}