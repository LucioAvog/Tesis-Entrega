using System.Collections.Generic;
using UnityEngine;

public enum ImagePosition { Izquierda, Derecha, Centro }
public enum PanelType { Panel1, Panel2 }

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

    [Header("Configuración de Imagen de Personaje")]
    public bool showImage;
    public Sprite characterImage;
    public ImagePosition imagePosition;
    
    [Tooltip("¡CLAVE! Deja esto en (X: 0, Y: 0) para usar el tamaño por defecto que configuraste en el Editor. Solo escribe números aquí si quieres cambiar el tamaño del personaje en esta línea específica.")]
    public Vector2 characterImageSize;

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
    [Tooltip("Tiempo exacto en segundos que sonará este efecto antes de apagarse.")]
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