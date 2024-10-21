using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SG_MovingPlatform : MonoBehaviour
{
    //Movement variables
    public float MoveTime = 3f;
    public float PauseTime = 1f;
    private float Counter = 0;

    //Movement locations
    public Transform PositionA;
    public Transform PositionB;
    private Vector3 nextPosition;
    private Vector3 prevPosition;

    //Flags
    public bool StartMoving = false;
    private bool Moving = false;
    private bool Forward = true;

    //Activation buttons
    public List<SG_Interactable> Buttons = new List<SG_Interactable>();

    public void Start()
    {
        //Set initial position
        prevPosition = transform.position;

        //Start loop if auto-start
        if (StartMoving)
        {
            ActivateMovement();
        }
    }

    public void ActivateMovement()
    {
        //Set flag
        Moving = true;

        //Find destination
        if(Forward)
        {
            nextPosition = PositionB.position;
        }
        else
        {
            nextPosition = PositionA.position;
        }

        //Set button states
        foreach (SG_Interactable button in Buttons)
        {
            button.SetState(true);
        }

        //Start movement loop
        StartCoroutine(MovePlatform());
    }

    private void FlipDirection()
    {
        //Switch flag
        Forward = !Forward;

        //Swap end locations
        if (Forward)
        {
            nextPosition = PositionB.position;
        }
        else
        {
            nextPosition = PositionA.position;
        }

        //Set the start location as current
        prevPosition = transform.position;
    }

    public void StopMovement()
    {
        //Halt everything
        if (Moving)
        {
            StopAllCoroutines();
            Moving = false;
        }

        //Set button states
        foreach (SG_Interactable button in Buttons)
        {
            button.SetState(false);
        }
    }

    IEnumerator MovePlatform()
    {
        //Process movement for this frame
        while(Counter < MoveTime)
        {
            //Lerp between locations
            float frac = Counter / MoveTime;
            transform.position = Vector3.Lerp(prevPosition, nextPosition, frac);

            //Step forward 
            Counter += Time.deltaTime;
            yield return null;
        }

        //Reset time
        Counter = 0;

        //Reverse
        FlipDirection();

        //Set button states
        foreach (SG_Interactable button in Buttons)
        {
            button.SetState(false);
        }
    }
}
