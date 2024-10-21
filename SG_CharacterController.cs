using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class SG_CharacterController : MonoBehaviour
{
    //Component References
    private SG_CameraController GameCamera;
    public CanvasGroup GameInterface;
    public CanvasGroup PauseMenu;
    public Animator CharacterAnimator;
    private Rigidbody2D CharacterRigid;
    public List<Image> HeartIcons = new List<Image>();
    public TextMeshProUGUI CoinReadout;

    //Colliders
    public Collider2D AttackCollision_Top;
    public Collider2D AttackCollision_Bottom;
    public Collider2D AttackCollision_Left;
    public Collider2D AttackCollision_Right;

    //SFX
    public AudioSource SFX_Jump;
    public AudioSource SFX_Squash;
    public AudioSource SFX_Stretch;
    public AudioSource SFX_Damaged;
    public AudioSource SFX_Coin;

    //Movement variables
    public float SpeedLimit = 5f;
    public float SpeedLimit_Stretching = 0.1f;
    public float Acceleration = 1.5f;
    public float SlideDrag = 0.01f;
    public float JumpForce = 5f;
    public float JumpWindowTime = 0.1667f;
    public float StretchAnimTime = 0.1667f;
    public float JumpAnimTime = 0.1667f;
    public float JumpHeight = 0.01f;
    public float DamagedCooldownTime = 3f;

    //Dynamic variable trackers
    public int Lives = 3;
    public int Health = 3;
    public int Coins = 0;
    private Vector2 VelocityInput = new Vector2(); //Current movement input from player
    private List<GameObject> LandedSurfaces = new List<GameObject>();

    //Flags
    private bool Initialized = false;
    private bool Frozen = false;
    private bool Landed = false;
    private bool FacingRight = true;
    private bool Moving = false;
    private bool Squashing = false;
    private bool Stretching = false;
    private bool JumpTriggered = false;
    private bool Invincible = false;
    private bool Paused = false;

    private void Awake()
    {
        //Find the camera
        GameCamera = Camera.main.GetComponent<SG_CameraController>();

        //Grab the rigidbody component ref, and freeze it until BeginLevel()
        CharacterRigid = GetComponent<Rigidbody2D>();
        CharacterRigid.isKinematic = true;

        //Hide UI
        GameInterface.alpha = 0;
        PauseMenu.alpha = 0;
    }

    private void Start()
    {
        //Wait for curtains to finish opening before beginning play
        GameCamera.OnCurtainsOpen += BeginLevel;

        //Reset level on ending
        GameCamera.OnCurtainsClosed += ReloadLevel;

        //Turn off attack collision
        AttackCollision_Top.gameObject.SetActive(false);
        AttackCollision_Left.gameObject.SetActive(false);
        AttackCollision_Right.gameObject.SetActive(false);
        AttackCollision_Bottom.gameObject.SetActive(false);

        //Initialize coin readout
        UpdateCoinDisplay();
    }
    
    public void BeginLevel()
    {
        //Set flag
        Initialized = true;

        //Start animation
        CharacterAnimator.SetBool("Initialized", true);

        //Unfreeze rigidbody
        CharacterRigid.isKinematic = false;

        //Clear init callback
        GameCamera.OnCurtainsOpen -= BeginLevel;

        //Show UI
        GameInterface.alpha = 1;
    }

    public void FinishLevel()
    {
        //Close curtains
        GameCamera.SetCurtains_Closed();

        //Unset flag
        Initialized = false;

        //Stop character
        VelocityInput = new Vector2();
    }

    public void ReloadLevel()
    {
        SceneManager.LoadScene("Level_1-1");
    }

    private void FixedUpdate()
    {
        //Apply movement input to rigidbody        
        if (Mathf.Abs(VelocityInput.x) > 0)
        {
            //Isolate horizontal input
            Vector2 horizVelocity = new Vector2();
            horizVelocity.x = VelocityInput.x;

            //Apply to rigidbody
            CharacterRigid.AddForce(horizVelocity);

            //Clear input if not in use
            if (!Moving && Landed)
            {
                VelocityInput.x = 0;
            }
        }
    }

    //Called by Player Input event in editor
    public void TogglePause()
    {
        SetPause(!Paused);
    }

    private void SetPause(bool isPaused)
    {
        //Set flag
        Paused = isPaused;
                
        if (Paused)
        {
            //Turn off time
            Time.timeScale = 0;

            //Enable UI
            PauseMenu.alpha = 1;
            PauseMenu.blocksRaycasts = true;
            PauseMenu.interactable = true;
        }
        else
        {
            //Turn time back on
            Time.timeScale = 1;

            //Disable UI
            PauseMenu.alpha = 0;
            PauseMenu.blocksRaycasts = false;
            PauseMenu.interactable = false;
        }

        //Inform camera
        GameCamera.SetPause(Paused);
    }

    //Called by Player Input event in editor
    public void Move(InputAction.CallbackContext context)
    {
        //Early exit if level has not started
        if (!Initialized) return;

        //Early exit if character is frozen
        if (Frozen) return;
        
        //Check if player has just stopped or started the input
        if (context.phase == InputActionPhase.Canceled)
        {
            Moving = false;

            //Start timer to listen for possible jump input
            if (!JumpTriggered && Squashing)
            {
                StartCoroutine(JumpWindow());
            }

            //Clear animation flags
            CharacterAnimator.SetBool("Squash", false);
            CharacterAnimator.SetBool("Stretch", false);
            Squashing = false;
            Stretching = false;

            //End movement input
            return;
        }
        else if (context.phase == InputActionPhase.Started)
        {
            //Start Moving
            Moving = true;
        }

        //Get the player's movement input amount
        Vector2 value = context.ReadValue<Vector2>();
        
        //Handle horizontal directional input
        if (Mathf.Abs(value.x) > 0f)
        {
            //Handle right
            if (value.x > 0)
            {
                //Flip character to face right if facing left
                if (!FacingRight)
                {
                    FacingRight = true;
                    CharacterAnimator.transform.Rotate(0, 180, 0);
                }
            }

            //Handle left
            if (value.x < 0)
            {
                //Flip character to face left if facing right
                if (FacingRight)
                {
                    FacingRight = false;
                    CharacterAnimator.transform.Rotate(0, 180, 0);
                }                
            }

            //Verify contact with ground
            if (Landed && !JumpTriggered)
            {
                //Calculate new speed
                float deltaSpeed = value.x * Acceleration;
                float newSpeed = Mathf.Clamp(VelocityInput.x + deltaSpeed, -SpeedLimit, SpeedLimit);

                //Apply current input to tracker
                VelocityInput.x = newSpeed;
            }
        }
        
        //Clear stretching flags if moving fast enough
        if (Mathf.Abs(VelocityInput.x) > SpeedLimit_Stretching)
        {
            CharacterAnimator.SetBool("Stretch", false);
            Stretching = false;
        }

        //Handle vertical direction input
        if (value.y > 0) //Up
        {
            if (Landed && !JumpTriggered)
            {
                //Set flags for stretch animation
                Stretching = true;
                CharacterAnimator.SetBool("Stretch", true);

                //Play SFX
                if (!SFX_Stretch.isPlaying) SFX_Stretch.Play();

                //Clear movement flag if vertical input is high enough
                if (value.y > 0.1f)
                {
                    Moving = false;
                }
            }
        }
        else if (value.y < 0) //Down
        {
            if (Landed && !JumpTriggered)
            {
                //Set flags for animation
                Squashing = true;
                CharacterAnimator.SetBool("Squash", true);

                //Play SFX
                SFX_Squash.Play();

                //Clear movement flag only if player isn't moving horizontally enough (allows for slide)
                if (Mathf.Abs(value.x) < 0.1f)
                {
                    Moving = false;
                }
            }
        }
        else
        {
            //Clear flags
            CharacterAnimator.SetBool("Squash", false);
            Stretching = false;

            //Start timer to listen for possible jump input
            if (!JumpTriggered && Squashing)
            {
                StartCoroutine(JumpWindow());
            }
        }
    }

    public void TakeDamage(Vector3 atkPos)
    {
        if (Invincible) return;        
        
        //Decrement health
        Health--;

        //Turn off all health icons above the current health value
        for (int i = Health; i < HeartIcons.Count; i++)
        {
            HeartIcons[i].enabled = false;
        }

        //Freeze character, make invincible
        Frozen = true;
        Invincible = true;

        //Play damage SFX
        SFX_Damaged.Play();

        //Check for game-over
        if (Health <= 0)
        {
            //Begin closing curtains
            GameCamera.SetCurtains_Closed();

            //Play death animation
            CharacterAnimator.SetBool("Dead", true);

            //Stop character
            CharacterRigid.isKinematic = true;
            CharacterRigid.velocity = new Vector2();
            VelocityInput = new Vector2();
        }
        else
        {
            //Play damage animation
            CharacterAnimator.SetBool("Damage", true);

            //Start reset timer
            StartCoroutine(DamagedCooldown());

            //Calculate hit force
            Vector2 hitVelocity = new Vector2();
            hitVelocity.y = JumpForce;
            hitVelocity.x = JumpForce;

            //Check for direction
            Vector3 delta = atkPos - transform.position;
            if (delta.x > 0) hitVelocity.x *= -1;

            //Apply force to character
            CharacterRigid.AddForce(hitVelocity);
        }
    }

    IEnumerator DamagedCooldown()
    {
        yield return new WaitForSeconds(DamagedCooldownTime * 0.25f);

        //Regain movement
        Frozen = false;

        yield return new WaitForSeconds(DamagedCooldownTime * 0.75f);

        //Become vincible
        Invincible = false;

        //End damaged animation
        CharacterAnimator.SetBool("Damage", false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Character landed on a solid surface
        if (collision.gameObject.tag == "Surface" ||
            collision.gameObject.tag == "MovingPlatform")
        {
            //Set flags
            Landed = true;
            CharacterAnimator.SetBool("Landed", true);

            //Track object
            LandedSurfaces.Add(collision.gameObject);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        //Character left surface
        if (collision.gameObject.tag == "Surface" || 
            collision.gameObject.tag == "MovingPlatform")
        {
            //Stop tracking object
            LandedSurfaces.Remove(collision.gameObject);

            //Check for additional surfaces
            if(LandedSurfaces.Count <= 0)
            {
                //Unset flags
                CharacterAnimator.SetBool("Landed", false);
                Landed = false;

                //Clear input
                VelocityInput = new Vector2();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Player's attack connects
        if(collision.tag == "MobDefense")
        {
            //Find enemy
            SG_EnemyController target = collision.GetComponentInParent<SG_EnemyController>();

            //Hurt enemy
            target.TakeDamage(transform.position);
        }
    
        //Enemy's attack connects
        if (collision.tag == "MobAttack")
        {
            //Hurt player
            TakeDamage(collision.transform.position);
        }
    
        //Player has reached the level end
        if (collision.tag == "Goal")
        {
            FinishLevel();
        }
    
        //Pick up coin
        if(collision.tag == "Coin")
        {
            //Add to count
            Coins++;

            //Update UI
            UpdateCoinDisplay();

            //Play SFX
            SFX_Coin.Play();

            //Remove coin
            Destroy(collision.gameObject);
        }
    }

    private void UpdateCoinDisplay()
    {
        //Set UI text to coin amount
        CoinReadout.text = Coins.ToString();
    }

    public void TriggerJump()
    {
        //Calculate jumping force
        Vector2 jumpVelocity = new Vector2();
        jumpVelocity.y = JumpForce;

        float boost = JumpForce * 0.1f;
        if (!FacingRight) boost *= -1;
        jumpVelocity.x = CharacterRigid.velocity.x + boost;

        //Apply force to character
        CharacterRigid.AddForce(jumpVelocity);

        //Set jump animation
        CharacterAnimator.SetBool("Jump", false);

        //Set flags
        Landed = false;
        Stretching = false;
        JumpTriggered = false;
    }

    //Move character location up at the end of the Jump animation
    public void TriggerTeleport()
    {
        //Turn rigidbody off while we do our business, keeps it from freaking out
        CharacterRigid.isKinematic = true;

        //Teleport the character
        Vector3 pos = transform.position;
        pos.y += JumpHeight;
        transform.position = pos;

        //Turn rigidbody back on, nothing to see here
        CharacterRigid.isKinematic = false;
    }

    public void TriggerStretchAttack()
    {
        SetCollision_Stretch(true);
    }

    public void TriggerSquashAttack()
    {
        SetCollision_Squash(true);
    }

    public void ClearAttacks()
    {
        SetCollision_Stretch(false);
        SetCollision_Squash(false);
    }

    private void SetCollision_Squash(bool active)
    {
        AttackCollision_Left.gameObject.SetActive(active);
        AttackCollision_Right.gameObject.SetActive(active);
        AttackCollision_Bottom.gameObject.SetActive(active);
    }

    private void SetCollision_Stretch(bool active)
    {
        AttackCollision_Top.gameObject.SetActive(active);
    }

    //Timer for activating jump move by stretching up immediately after squashing down
    IEnumerator JumpWindow()
    {
        //Used a while loop to catch the flag change during a frame
        float counter = 0;        
        while(counter < JumpWindowTime)
        {
            counter += Time.deltaTime;

            //Player has triggered a stretch
            if (Stretching)
            {
                JumpTriggered = true;
                SFX_Stretch.Stop();
            }

            yield return null;
        }

        Squashing = false;

        //Trigger the jump now if activated before the window expired
        if (JumpTriggered)
        {
            Stretching = false;
            
            CharacterAnimator.SetBool("Jump", true);
            CharacterAnimator.SetBool("Stretch", false);

            SFX_Jump.Play();
        }
    }

    //Handle exiting with preprocessors check for editor vs build
    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
