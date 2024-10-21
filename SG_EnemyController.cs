using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SG_EnemyController : MonoBehaviour
{

    //Component references
    private SG_CameraController GameCamera;
    public Animator MobAnimator;
    private Rigidbody2D MobRigid;
    public AudioSource SFX_Damaged;
    public List<Collider2D> AttackColliders = new List<Collider2D>();

    //Particle object to spawn on death
    public GameObject Prefab_DeathPoof;

    //Dynamic variable trackers
    public int Health = 1;
    private Vector2 VelocityControl = new Vector2();
    private Vector2 ControlPaused = new Vector2();
    private Vector2 VelocityPaused = new Vector2();

    //Movement variables
    public float SpeedLimit = 5f;
    public float MoveTime = 3;
    public float PauseTime = 3;
    public float TurnChance = 1;
    public float MoveForce = 100f;
    public float JumpChance = 0f;
    public float JumpForce = 250f;
    public float DamagedCooldownTime = 3f;

    //Flags
    public bool FacingRight = false;
    private bool Invincible = false;
    private bool Jumping = false;
    private bool Jumped = false;

    private void Awake()
    {
        //Find the camera
        GameCamera = Camera.main.GetComponent<SG_CameraController>();

        //Grab rigidbody and freeze it until BeginLevel()
        MobRigid = GetComponent<Rigidbody2D>();
        MobRigid.isKinematic = true;

        //Subscribe to pausing events
        GameCamera.OnPaused += Paused;
        GameCamera.OnResumed += Resumed;
    }

    private void Start()
    {
        //Wait for curtains to finish opening
        GameCamera.OnCurtainsOpen += BeginLevel;
    }

    private void BeginLevel()
    {
        //Unfreeze rigidbody
        MobRigid.isKinematic = false;

        //Begin animating
        MobAnimator.SetBool("Initialized", true);

        //Curtains are open, unsubscribe
        GameCamera.OnCurtainsOpen -= BeginLevel;

        //Start movement loop
        StartCoroutine(MoveCycle());
    }

    private void LateUpdate()
    {
        //Mob is moving
        if (Mathf.Abs(VelocityControl.x) > 0 ||
            Mathf.Abs(VelocityControl.y) > 0)
        {
            //Clear lateral control if speed limit is reached
            if(MobRigid.velocity.x > SpeedLimit || MobRigid.velocity.x < -SpeedLimit)
            {
                VelocityControl.x = 0;
            }

            //Apply jump as needed
            if (Jumping)
            {
                //Clear flag
                Jumping = false;

                //Clear animation
                MobAnimator.SetBool("Jumping", false);
            }

            //Apply movement force
            MobRigid.AddForce(VelocityControl);

            //Clear control value
            VelocityControl = new Vector2();
        }
    }

    public void Paused()
    {
        //Stash movement current values
        ControlPaused = VelocityControl;
        VelocityPaused = MobRigid.velocity;

        //Freeze movement
        VelocityControl = new Vector2();
        MobRigid.velocity = new Vector2();
        MobRigid.isKinematic = true;
    }

    public void Resumed()
    {
        //Unfreeze
        MobRigid.isKinematic = false;

        //Restore movement values
        VelocityControl = ControlPaused;
        MobRigid.velocity = VelocityPaused;
    }
    
    IEnumerator MoveCycle()
    {
        //Start loop with first pause
        yield return new WaitForSeconds(PauseTime);

        //Reset flag
        Jumped = false;

        //Process movement frames
        float counter = 0;
        while (counter < MoveTime)
        {
            counter += Time.deltaTime;

            //Setup values for force vector
            float jumping = 0;
            float walking = MoveForce;

            //Roll dice for chance to jump on this frame
            float rollJump = Random.Range(0f, 1f);
            if (rollJump < JumpChance && !Jumped)
            {
                //Set flags
                Jumped = true;
                Jumping = true;

                //Set animation
                MobAnimator.SetBool("Jumping", true);

                //Update jump force value
                jumping = JumpForce;
            }

            //Flip direction as needed
            if (!FacingRight) walking *= -1f;

            //Set force amount to apply this frame
            VelocityControl = new Vector2(walking, jumping);

            yield return null;
        }

        //Second pause after moving
        yield return new WaitForSeconds(PauseTime);

        //Roll dice for chance to turn before restarting
        float rollTurn = Random.Range(0f, 1f);
        if (rollTurn < TurnChance)
        {
            //Flip character
            FacingRight = !FacingRight;
            MobAnimator.transform.Rotate(0, 180, 0);
        }

        //Restart loop
        StartCoroutine(MoveCycle());
    }

    public void TakeDamage(Vector3 atkPos)
    {
        if (Invincible) return;
        
        //Decrement health
        Health--;

        //Play SFX
        SFX_Damaged.Play();

        //Calculate hit force & direction
        Vector2 hitVelocity = new Vector2();
        hitVelocity.y = JumpForce;
        hitVelocity.x = JumpForce;
        Vector3 delta = atkPos - transform.position;
        if (delta.x > 0) hitVelocity.x *= -1;

        //Apply hit force
        MobRigid.freezeRotation = false;
        MobRigid.AddForce(hitVelocity);

        //Check for death
        if (Health <= 0)
        {
            //Clear any running loops
            StopAllCoroutines();

            //Play death animation
            MobAnimator.SetBool("Dead", true);            

            //Turn off attacks
            foreach (Collider2D collider in AttackColliders)
            {
                collider.enabled = false;
            }

            //Apply spin force
            float torque = Random.Range(-15f, 15f);
            MobRigid.AddTorque(torque);

            //Start death timer
            StartCoroutine(Death());
        }
        else
        {
            //prevent more damage until cooled
            Invincible = true;

            //Set damage animation
            MobAnimator.SetBool("Damage", true);

            //Start cooldown timer
            StartCoroutine(DamagedCooldown());
        }
    }

    IEnumerator DamagedCooldown()
    {
        //Pause damaged enemy until cooled
        yield return new WaitForSeconds(DamagedCooldownTime);

        //Reset flag
        Invincible = false;

        //Reset animation
        MobAnimator.SetBool("Damage", false);

        //Reset rigidbody
        MobRigid.freezeRotation = true;
        MobRigid.SetRotation(0);
    }

    IEnumerator Death()
    {
        //Pause dead enemy until cooled
        yield return new WaitForSeconds(DamagedCooldownTime);

        //Spawn a particle system object
        GameObject poof = Instantiate(Prefab_DeathPoof, transform.parent);
        poof.transform.position = transform.position;

        //Unsubscribe
        GameCamera.OnPaused -= Paused;
        GameCamera.OnResumed -= Resumed;

        //Get rid of the body
        Destroy(gameObject);
    }
}
