using System.Collections.Generic;
using UnityEngine;

public enum ImagePosition { Izquierda, Derecha, Centro, Personalizada }
public enum PanelType { Panel1, Panel2, Panel3 }

[System.Serializable]
public struct Choice
{
    public string buttonText;
    public DialogueScript nextDialogue;
    public Sprite buttonBackground;

    [Header("Cambio de Escena (Opcional)")]
    public bool changeScene;
    public string sceneToLoad;
}

[System.Serializable]
public struct Line
{
    public string speaker;
    [TextArea(3, 5)]
    public string text;

    [Header("Transición de Texto (Fade In)")]
    public bool usarTransicionTexto; // <--- Check para activar/desactivar la transición suave
    public float duracionTransicion; // <--- Duración del desvanecimiento en segundos (Ej: 1.5)

    [Header("Personaje 1 (Principal)")]
    public bool showImage;
    public Sprite characterImage;
    public ImagePosition imagePosition;
    public Vector2 characterImagePosition;
    public Vector2 characterImageSize;

    [Header("Personaje 2 (Opcional)")]
    public bool showImage2;
    public Sprite characterImage2;
    public ImagePosition imagePosition2;
    public Vector2 characterImagePosition2;
    public Vector2 characterImageSize2;

    [Header("Personaje 3 (Opcional)")]
    public bool showImage3;
    public Sprite characterImage3;
    public ImagePosition imagePosition3;
    public Vector2 characterImagePosition3;
    public Vector2 characterImageSize3;

    [Header("Imagen UI de Pantalla / Ilustración")]
    public bool mostrarImagenUI; 
    public Sprite imagenUI;      

    [Header("Configuración del Panel de Diálogo")]
    public PanelType panelType;

    [Header("Configuración de Fondo (Opcional)")]
    public Sprite newBackground; 

    [Header("Efectos Especiales")]
    public bool triggerShake;
    public float shakeDuration;
    public float shakeIntensity;

    [Header("Efectos de Audio (Voz / Texto)")]
    public bool playSound;
    public AudioClip soundClip;

    [Header("Efectos de Audio Nuevos (SFX con Duración Fija)")]
    public bool playSFX;
    public AudioClip sfxClip;
    public float sfxDuration;

    [Header("Minijuegos / Carrusel de Paneles")]
    public bool dispararCarruselPaneles;
    public bool ocultarPanelDialogo;

    [Header("Decisiones (Opcional)")]
    public PanelType choicePanelType;
    public List<Choice> choices;
}

[CreateAssetMenu(fileName = "NewConversation", menuName = "Dialogues/Conversation")]
public class DialogueScript : ScriptableObject
{
    [Header("Configuración Global de la Conversación")]
    public PanelType defaultPanelType;

    [Header("Continuación Automática (Opcional)")]
    public DialogueScript nextConversation;

    [Header("Cambio de Escena al Terminar (Opcional)")]
    public bool changeSceneOnEnd; 
    public string sceneToLoadOnEnd; 

    [Space(10)]
    public List<Line> conversationLines;
}