using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

//This script is for the fading instructions that appear when the game starts
//it displays 3 different text blocks that give the player context and game instructions
//Made with reference to Project A where I had fading text at the start of the game
//Additional resources used: Unity Manual "Coroutines"


public class IntroPanelSequence : MonoBehaviour
{
    //Serialized lets me tweak/edit the values of the private variables below  in the Unity Editor

    //List of all of the text objects that will appear
    [SerializeField] private List<TMP_Text> textLines = new List<TMP_Text>();
    //Canvas group that controls the overall visibility
    [SerializeField] private CanvasGroup panelGroup;
    //Duration for each text line to fade in/out
    [SerializeField] private float textFadeDuration = 1f;
    //How long each line of text is displayed for
    [SerializeField] private float textDisplayDuration = 2f;
    //How long the panel takes to fade 
    [SerializeField] private float panelFadeOutDuration = 1.5f;


    private void Awake()
    {
        //Making sure the CanvasGroup reference is there
        if (panelGroup == null)
            panelGroup = GetComponent<CanvasGroup>();

        //Makes all of the text invisible at the start
        foreach (var line in textLines)
            line.alpha = 0f;
    }

    private void Start()
    {
        //start the sequence once the scene loads
        StartCoroutine(RunIntroSequence());
    }


    //This is what runs the entire sequence of fading in/out of each text line
    //After that, it then fades the entire panel
    private IEnumerator RunIntroSequence()
    {
        panelGroup.alpha = 1f; //makes the panel visible when the sequence starts with an alpha of 1

        //looping through each text element
        foreach (var line in textLines)
        {
            // Fade in
            yield return StartCoroutine(FadeText(line, 0f, 1f, textFadeDuration));
            yield return new WaitForSeconds(textDisplayDuration); //keeps it visible for the set duration

            // Fade out
            yield return StartCoroutine(FadeText(line, 1f, 0f, textFadeDuration));
        }

        // Fade out the whole panel at the end once all of the text has been shown
        float elapsed = 0f; //this starts a timer that measures how much time has passed in a loop
        while (elapsed < panelFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            panelGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / panelFadeOutDuration); //using math lerp to make the fade smooth not instant
            yield return null;
        }

        //disables the whole panel once finished
        gameObject.SetActive(false);
    }

    //this section allows for the smooth fading of the TMP text obejcts 
    //targets its alpha from start to end over the set duration
    private IEnumerator FadeText(TMP_Text text, float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            //ex of lerp below: starts at 0 alpha, moves to an alpha of 1 over a given duration
            text.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        //ensures the final alpha value is set to the target value once the loop finished
        text.alpha = end;
    }
}
