/*
    A class that contains buttons that are displayed during drawing
*/
using System;
using MixedReality.Toolkit.UX;
using PicassoAR.Utils;
using UnityEngine;
using UnityEngine.Events;

public class GameControlButtons : MonoBehaviour
{
    private PressableButton relocalizeImageButton;
    private PressableButton endDrawingeButton;
    private PressableButton fixImagePositionButton;
    public GameObject drawingButtons;
    public GameObject fixImageButton;
    public GameObject gameControlButtons;

    public UnityEvent OnRelocalizeImage;
    public UnityEvent OnFixImagePosition;

    void OnEnable()
    {   
        try 
        {
            gameControlButtons = GameObject.Find("GameControlButtons");
            drawingButtons = GameObject.Find("DrawingButtons");
            fixImageButton = GameObject.Find("FixImageButton");
            relocalizeImageButton = Helpers.GetChildComponentByName<PressableButton>(drawingButtons, "RelocalizeImageButton");
            endDrawingeButton = Helpers.GetChildComponentByName<PressableButton>(drawingButtons, "EndDrawingButton");
            fixImagePositionButton = Helpers.GetChildComponentByName<PressableButton>(fixImageButton, "FixImagePositionButton");

            drawingButtons.SetActive(false);
            fixImageButton.SetActive(false);
            relocalizeImageButton.enabled = false;
            endDrawingeButton.enabled = false;
            fixImagePositionButton.enabled = false;

        } catch (Exception e)
        {
            Debug.Log("Error in enabling GameControlButtons: " + e.Message);
        }

    }

    public void OnRelocalizImageButtonClicked()
    {
        drawingButtons.SetActive(false);
        relocalizeImageButton.enabled = false;
        endDrawingeButton.enabled = false;
        OnRelocalizeImage.Invoke();
    }

    public void OnEndDrawingButtonClicked() => Application.Quit(); // temporary 

    public void OnFixImagePositionButtonClicked()
    {
        drawingButtons.SetActive(true);
        fixImagePositionButton.enabled = false;
        relocalizeImageButton.enabled = true;
        endDrawingeButton.enabled = true;
        OnFixImagePosition.Invoke();
    }
    
}