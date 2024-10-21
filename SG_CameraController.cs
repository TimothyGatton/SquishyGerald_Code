using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SG_CameraController : MonoBehaviour
{
    //Player to follow
    public Transform Character;

    //Wiggle room before moving to follow
    public float Margin = 5f;

    //Level bounds
    public Transform Limit_Left;
    public Transform Limit_Right;
    public Transform Limit_Up;
    public Transform Limit_Down;

    //Curtain references for start & end of level
    public Transform Curtains_Left;
    public Transform Curtains_Right;
    public Transform CurtainPosition_OpenLeft;
    public Transform CurtainPosition_ClosedLeft;
    public Transform CurtainPosition_OpenRight;
    public Transform CurtainPosition_ClosedRight;
    public float CurtainTime = 1.0f;

    //Movement variables
    private Vector3 m_CamVelocity = Vector3.zero;
    private float m_CameraSmoothing = 0.005f;

    //Camera events
    public delegate void CameraEvent();
    public CameraEvent OnCurtainsOpen;
    public CameraEvent OnCurtainsClosed;
    public CameraEvent OnPaused;
    public CameraEvent OnResumed;

    private void Start()
    {
        //Move curtains to closed position
        Curtains_Left.position = CurtainPosition_ClosedLeft.position;
        Curtains_Right.position = CurtainPosition_ClosedRight.position;

        //Initialize camera
        StartCoroutine(Initialize());
    }

    IEnumerator Initialize()
    {
        //Wait for second frame to start opening curtains
        yield return new WaitForEndOfFrame();

        //Open curtains
        StartCoroutine(MoveCurtains(true));
    }

    //Called by CharacterController class on level end
    public void SetCurtains_Closed()
    {
        StartCoroutine(MoveCurtains(false));
    }

    IEnumerator MoveCurtains(bool open)
    {
        //Movement positions refs for lerping
        Vector3 CurtainStart_Left;
        Vector3 CurtainStart_Right;
        Vector3 CurtainGoal_Left;
        Vector3 CurtainGoal_Right;

        //Set position locations based on direction
        if (open)
        {
            CurtainStart_Left = CurtainPosition_ClosedLeft.position;
            CurtainStart_Right = CurtainPosition_ClosedRight.position;
            CurtainGoal_Left = CurtainPosition_OpenLeft.position;
            CurtainGoal_Right = CurtainPosition_OpenRight.position;
        }
        else
        {
            CurtainStart_Left = CurtainPosition_OpenLeft.position;
            CurtainStart_Right = CurtainPosition_OpenRight.position;
            CurtainGoal_Left = CurtainPosition_ClosedLeft.position;
            CurtainGoal_Right = CurtainPosition_ClosedRight.position;
        }

        //Process curtain movement
        float counter = 0;
        while (counter < CurtainTime)
        {
            //Keep time
            counter += Time.deltaTime;

            //Lerp position
            float frac = counter / CurtainTime;
            Curtains_Left.position = Vector3.Lerp(CurtainStart_Left, CurtainGoal_Left, frac);
            Curtains_Right.position = Vector3.Lerp(CurtainStart_Right, CurtainGoal_Right, frac);

            yield return null;
        }


        //Broadcast completion
        if (open)
        {
            if (OnCurtainsOpen != null)
            {
                OnCurtainsOpen();
            }
        }
        else
        {
            //Let that sink in
            yield return new WaitForSeconds(CurtainTime);

            if (OnCurtainsClosed != null)
            {
                OnCurtainsClosed();
            }
        }
    }

    public void SetPause(bool paused)
    {
        if (paused)
        {
            if(OnPaused != null)
            {
                OnPaused();
            }
        }
        else
        {
            if (OnResumed != null)
            {
                OnResumed();
            }
        }
    }

    private void FixedUpdate()
    {
        //Find positions and directions for movement
        Vector3 camPos = transform.position;
        Vector3 targetPos = Character.position;
        targetPos.z = camPos.z;
        Vector2 camDir = (camPos - targetPos).normalized;

        //Determine if any bounds are reached
        bool leftLimit = camPos.x <= Limit_Left.position.x;
        bool rightLimit = camPos.x >= Limit_Right.position.x;
        bool upLimit = camPos.y >= Limit_Up.position.y;
        bool downLimit = camPos.y <= Limit_Down.position.y;

        //Limit movement to bounds
        if (leftLimit && camDir.x > 0 ||
            rightLimit && camDir.x < 0)
        {
            targetPos.x = camPos.x;
        }

        if (upLimit && camDir.y < 0 || 
            downLimit && camDir.y > 0)
        {
            targetPos.y = camPos.y;
        }

        //Apply movement
        transform.position = Vector3.SmoothDamp(targetPos, camPos, ref m_CamVelocity, m_CameraSmoothing);
    }
}
