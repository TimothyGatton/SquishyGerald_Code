using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SG_Interactable : MonoBehaviour
{
    //Sprite components
    public SpriteRenderer ColorRenderer;
    public SpriteRenderer SwitchRenderer;
    public Sprite Img_On;
    public Sprite Img_Off;

    //Color options
    public Color OnColor;
    public Color OffColor;

    //State flag
    public bool CurrentState = false;

    //Platform to control
    public SG_MovingPlatform TargetPlatform;

    private void Start()
    {
        //Initialize
        SetState(CurrentState);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TriggerInteraction();
    }

    private void TriggerInteraction()
    {
        //Flip state
        SetState(!CurrentState);        

        //Stop or start movement
        if (CurrentState) TargetPlatform.ActivateMovement();
        else TargetPlatform.StopMovement();
    }

    public void SetState(bool state)
    {
        //Set flag
        CurrentState = state;

        //Set visuals
        if (CurrentState)
        {
            ColorRenderer.color = OnColor;
            SwitchRenderer.sprite = Img_On;
        }
        else
        {
            ColorRenderer.color = OffColor;
            SwitchRenderer.sprite = Img_Off;
        }
    }
}
