using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectBehaviorTrigger: BaseEntity
{
    [Header("Control & Movement")]
    public PlayerControls playerInput;

    public GameManager gm;
    //[HideInInspector] public MovementManager mm;
    //public PlayerInput pI;
    //string inputMethod;
    public Rigidbody2D rb;
    public int moveSpeed = 10;
    public float jumpForce = 400f;
    public string Name;
    public string type;
    protected Vector2 _move;
    protected Vector2 _vxy;
    protected Vector2 _velocity;
    protected float _angVel;
    public bool isPossessed = false;
    protected GameObject LaramiePossessionRange;
    protected bool _InRange;
    protected GameObject _ObjectInRange = null;
    public GameObject Ghost = null;
    protected ObjectBehaviorTrigger _ghostCode;
    public LayerMask whatIsGround;
    public Transform groundCheck;
    public bool isGrounded = true;
    public bool facingRight;
    //bool _currentlySpeaking = false;
    public float possessMassMod = 2;
    public PhysicsMaterial2D frictionless;
    public float spin = 1;
    public float freeSpin = 2;
    public bool canMove = true;
    public bool canFlip = true;
    public bool controlsDisabled = false; //Used to make it so the player can do anything during a cut scene
    [Header("Jumping ground check stuff")] 
    private bool naturallyPortrait = false;
    private CircleCollider2D cirlceCollider;
    private BoxCollider2D boxCollider;
    private float halfWidth;
    private float halfHeight;
    
    [Header("Time until platforms and object interact again")]
    public float platformDropTime = 0.2f;

    [Header("Clamp")]
    //Values used for clamping
    public float maxX = 0f;
    public float minX = 0f;
    public float maxY = 0f;
    public float minY = 0f;

    [Header("Health & Damage")] 
    public bool canTakeDamage = true; //can this object currently take damage
    public int health = 100; //HP
    //public List<HeartHitPoint> hearts = new List<HeartHitPoint>();
    public List<HeartHitPoint> hearts;
    private int activeHeartIndex;
    //public int heartNum = 3;
    public float physDamageThreshold; //How big of a collision will damage the player
    public int physDamageAmount = 0; //Amount to damage player\
    public GameObject hurtFX;
    public GameObject dieFX;
    public Transform respawnPoint;
    private int initialHealth = 100;
    private int initialHeartCount = 3;   

    protected int _playerLayer;
    protected int _platformLayer;
    protected Animator _animator;
    protected Vector3 _rollGroundCheck;
    [HideInInspector]
    public GameObject manager;
    protected float _baseMass;
    public float invincibilityTime = 3f;
    protected float invincibleEndTime = 0;
    protected bool currentlyInvincible = false;
    protected Animator cam;

    [Header("Materials")]
    //materials
    public Material matWhite;
    protected Material matDefault;
    public Material matRange;

    public SpriteRenderer spriteRend;
    
    [Header("Audio")]
    //audio
    protected AudioSource objectaud;
    public GameObject damageAudio;
    protected AudioSource dmgaud;
    public AudioClip damage;
    public AudioClip destroy;
    public bool hasplayed;
    protected AudioSource ghostaud;
    public AudioClip possess;
    public AudioClip depossess;
    public AudioClip switchpossess;

    [Header("Misc")]
    public GameObject theGlow;
    protected MenuManager menMan;
    private bool possessionPaused = false;
    //public PossessUIManager possUI;
    public GameObject soundIndicator;

    [Header("Debug Config")]
    public bool activateOnDebug = false;
    public Vector2 debugGoalPosition;

    private void Awake()
    {
        playerInput = new PlayerControls();
        playerInput.Controls.Movement.performed += ctx => _move = ctx.ReadValue<Vector2>();
        playerInput.Controls.Movement.canceled += ctx => _move = Vector2.zero;
        playerInput.Controls.Possess.performed += ctx => PossessPressed();
        //playerInput.Controls.Depossess.performed += ctx => DepossessPressed();
        playerInput.Controls.Jump.performed += ctx => JumpPressed();
        playerInput.Controls.Jump.canceled += ctx => JumpReleased();
        playerInput.Controls.Pause.performed += ctx => PausePressed();
        playerInput.Controls.SpecialAction.performed += ctx => AbilityPressed();
        playerInput.Controls.DebugSkip.performed += ctx => TriggerDebug();
        //inputMethod = playerInput.currentControlScheme;

        menMan = GameObject.FindWithTag("Menu").GetComponent<MenuManager>();
        
        ExtendedAwake();
    }

    // Start is called before the first frame update
    void Start()
    {

        gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        //mm = GameObject.Find("MovementManager").GetComponent<MovementManager>();
        rb = GetComponent<Rigidbody2D>();
        _ghostCode = Ghost.GetComponent<ObjectBehaviorTrigger>();
        //grab the universal damage audiosource

        if (damageAudio)
        {
            if (!damageAudio.GetComponent<AudioSource>())
            {
                damageAudio.AddComponent<AudioSource>();
            }
            dmgaud = damageAudio.GetComponent<AudioSource>();
        }
        ghostaud = Ghost.GetComponent<AudioSource>();
        hasplayed = true;

        //make the cursor invisible while playing
        Cursor.visible = false;
        //Try to set audio source
        objectaud = gameObject.GetComponent<AudioSource>();
        //Set the sprite render
        spriteRend = GetComponent<SpriteRenderer>();
        if (spriteRend == null)
        {
            spriteRend = GetComponentInChildren<SpriteRenderer>();
        }
        //Set default material
        matDefault = spriteRend.material;
        //Load white mat
        matWhite = Resources.Load<Material>("WhiteFlash");
        //Load in range tint material
        matRange = Resources.Load<Material>("Tint");
        //Set game manager
        manager = GameObject.FindGameObjectWithTag("GameController");

        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Animator>();

        _baseMass = rb.mass;

        theGlow.SetActive(false);
        
        //Will make it so object can take damage by default because so some reason this isn't
        //being set in the variable definition
        canTakeDamage = true;

        // determine the platform's specified layer
        _platformLayer = LayerMask.NameToLayer("Platform");
        _playerLayer = LayerMask.NameToLayer("Player");

        _animator = GetComponent<Animator>();
        
        //Used for setting the side ground check positions
        cirlceCollider = GetComponent<CircleCollider2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        CheckOriginalOrientation();
        
        //Debug.LogError("Animator component missing from this gameobject");
        if(type == "object")
        {
            if (physDamageAmount != 0)
            {
                _animator.SetBool("HighHealth", true);
            }
        }

        if (this.gameObject.tag == "Player")
        {
            //_playerLayer = this.gameObject.layer;
            if (Name == "Laramie")
            {
                //this.gameObject.GetComponent<BoxCollider2D>().enabled = false;
            }
            PossessObject();
            gm.ChangeDisplayedButtons(0);
            gm.controllerText[1].text = "Possess";
            //Gets the Possession Range child
            LaramiePossessionRange = transform.Find("PossessionRange").gameObject;
        }

        //Sets the index of the last heart in hearts list
        activeHeartIndex = hearts.Count - 1;
        health = hearts.Count * 2;
        initialHealth = health;
        initialHeartCount = hearts.Count;

        SetClamps();
        
        ExtendedStart();
    }

    /*private void AssembleHearts()
    {
        for (int i = 0; i < heartNum; i++)
        {
            hearts.Add(new HeartHitPoint());
        }
    }*/
    
    private void SetClamps()
    {
        maxX = gm.maxX;
        minX = gm.minX;
        maxY = gm.maxY;
        minY = gm.minY;
    }

    private void CheckOriginalOrientation()
    {
        halfWidth = spriteRend.size.x / 2;
        halfHeight = spriteRend.size.y / 2;
        
        if (cirlceCollider)
        {
            if (halfHeight > cirlceCollider.radius / 2) 
            {
                halfHeight = cirlceCollider.radius / 2;
            }

            if (halfWidth > cirlceCollider.radius / 2)
            {
                halfWidth = cirlceCollider.radius / 2;
            }
        }else if (boxCollider)
        {
            if (halfHeight > boxCollider.size.y / 4)
            {
                halfHeight = boxCollider.size.y / 4;
            }

            if (halfWidth > boxCollider.size.x / 4)
            {
                halfWidth = boxCollider.size.x / 4;
            }
        }

        naturallyPortrait = halfHeight > halfWidth;
    }

    private void OnEnable()
    {
        playerInput.Controls.Enable();
    }

    private void OnDisable()
    {
        playerInput.Controls.Disable();
    }

    public void AbilityPressed()
    {
        if (isPossessed && !controlsDisabled)
        {
            ActivateAbility();
        }
    }

    public void JumpPressed()
    {
        if (isPossessed && canMove && !controlsDisabled)
        {
            if (name != "Laramie")
            {
                if (isGrounded)
                {
                    //Debug.Log("Jumping");
                    DoJump();
                }
                /*if (_vxy.y > 0)
                {
                    _vxy.y = 0f;
                }*/
                //Debug.Log("Jump");
            }
        }
    }

    public void JumpReleased()
    {
        if (isPossessed && canMove && !controlsDisabled)
        {
            if (name != "Laramie")
            {
                
                if (_vxy.y > 0)
                {
                    _vxy.y = 0f;
                }
                //Debug.Log("Jump");
            }
        }
    }

    public void PossessPressed()
    {
        if (!controlsDisabled)
        {
            if (gm.PossessPressedLast && (type == "object" || type == "prototype") && isPossessed)
            {
                //Debug.Log("returning");
                if (!possessionPaused)
                {
                    RestoreForm();
                    //StartCoroutine(PossessionPause(false));
                }
            }
            else if (type == "Ghost" && !gm.PossessPressedLast && isPossessed)
            {
                if (_InRange && _ObjectInRange.GetComponentInParent<ObjectBehaviorTrigger>().Name != "Person" &&
                    !possessionPaused)
                {
                    //Debug.Log(Name + " Switch");
                    SwitchPossession();
                    StartCoroutine(PossessionPause(true));
                }
            }
        }
    }

    /*public void DepossessPressed()
    {
        if (isPossessed)
        {
            RestoreForm();
        }
    }*/


    public void PausePressed()
    {
        if (isPossessed)
        {
            menMan.PausePressed();
        }
    }

    // Update is called once per frame
    void Update()
    {
        ExtendedUpdate();

        if (isPossessed && canMove && !controlsDisabled)
        {
            //_move = mm.move;
            //_isGrounded = Physics2D.Linecast(transform.position, groundCheck.position, whatIsGround);

            //_vxy.x = Input.GetAxisRaw("Horizontal");
            _vxy.x = _move.x;

            _velocity = rb.velocity;

            if (Name == "Laramie" || Name == "Flying Object")
            {
                //_vxy.y = Input.GetAxisRaw("Vertical");
                _vxy.y = _move.y;
                _velocity = Vector2.Lerp(_velocity, _vxy * moveSpeed, 0.1f);
                rb.velocity = _velocity;

                if (gm.PossessPressedLast && Name == "Laramie")
                {
                    gm.PossessPressedLast = false;
                }

                if (Name == "Laramie")
                {
                    if (_move.x == 0 && _move.y == 0)
                    {
                        _animator.speed = .65f;
                    }
                    else
                    {
                        _animator.speed = 1f;
                    }
                }
                
            }
            else
            {
                if (gm.PossessPressedLast && type == "object")
                {
                    gm.PossessPressedLast = true;
                }
                
                // Make groundcheck independent of rotation.
                _rollGroundCheck = gameObject.transform.position + (groundCheck.localPosition  * gameObject.transform.localScale.x);

                //_vxy.y = 0;
                _vxy.y = rb.velocity.y;
                isGrounded = Physics2D.Linecast(transform.position, _rollGroundCheck, whatIsGround);
                if (!isGrounded)
                {
                    //This big if statement makes it so you can jump on the very edge of an object
                    bool useWidth = false;
                    float objectRot = this.transform.localEulerAngles.z;
                    if ((45f <= objectRot && objectRot < 135f) || (225f <= objectRot && objectRot < 315f))
                    {
                        
                        useWidth = false;
                        
                    }else if ((315f <= objectRot || objectRot < 45f) || (135f <= objectRot && objectRot < 255f))
                    {
                        
                        useWidth = true;
                    }

                    //This one goes out the width to the left and the right of the object
                    if ((Physics2D.Linecast(
                             new Vector3(transform.position.x + halfWidth, transform.position.y, transform.position.z),
                             new Vector3(_rollGroundCheck.x + halfWidth, _rollGroundCheck.y, _rollGroundCheck.z), whatIsGround) ||
                         Physics2D.Linecast(
                             new Vector3(transform.position.x - halfWidth, transform.position.y, transform.position.z),
                             new Vector3(_rollGroundCheck.x - halfWidth, _rollGroundCheck.y, _rollGroundCheck.z), whatIsGround) &&
                         useWidth)
                    )
                    {
                        isGrounded = true;
                    }
                    //This one goes out the height to the left and right of the object
                    if (Physics2D.Linecast(
                            new Vector3(transform.position.x   + halfHeight, transform.position.y, transform.position.z),
                            new Vector3(_rollGroundCheck.x  + halfHeight, _rollGroundCheck.y, _rollGroundCheck.z), whatIsGround) ||
                        Physics2D.Linecast(
                            new Vector3(transform.position.x  - halfHeight, transform.position.y, transform.position.z),
                            new Vector3(_rollGroundCheck.x  - halfHeight, _rollGroundCheck.y, _rollGroundCheck.z), whatIsGround) &&
                        !useWidth)
                    {
                        isGrounded = true;
                    }
                }

                _vxy.x *= moveSpeed;
                _velocity = Vector2.Lerp(_velocity, _vxy, 0.1f);
                rb.velocity = _velocity;

                if (isGrounded)
                {
                    _angVel = Input.GetAxis("Horizontal") * -100 * spin;
                    //_angVel = _move.x * -100 * spin;
                } else
                {
                    _angVel = Input.GetAxis("Horizontal") * -100 * freeSpin;
                    //_angVel = _move.x * -100 * freeSpin;
                }

                rb.angularVelocity = _angVel;

                //If the player is pressing all the way down
                //_move.y
                if (_move.y <= -.9)
                {
                    PlatformDrop();
                }
            }

            transform.position = new Vector2(Mathf.Clamp(transform.position.x, minX, maxX),
            (Mathf.Clamp(transform.position.y, minY, maxY)));


        }
        if (currentlyInvincible && Time.time >= invincibleEndTime)
        {
            currentlyInvincible = false;
        }
    }

    void LateUpdate()
    {
        //If you can flip and the time scale is greater than 0 (game is unpaused)
        if (canFlip && Time.timeScale > 0f)
        {
            // get the current scale
            Vector3 localScale = transform.localScale;

            if (_vxy.x > 0) // moving right so face right
            {
                facingRight = true;
            }
            else if (_vxy.x < 0)
            { // moving left so face left
                facingRight = false;
            }

            // check to see if scale x is right for the player
            // if not, multiple by -1 which is an easy way to flip a sprite
            if (((facingRight) && spriteRend.flipX) || ((!facingRight) && !spriteRend.flipX))
            {
                spriteRend.flipX = !spriteRend.flipX;
                if (Name == "Laramie")
                {
                    LaramiePossessionRange.transform.localPosition = new Vector3(
                        LaramiePossessionRange.transform.localPosition.x * -1, LaramiePossessionRange.transform.localPosition.y,
                        LaramiePossessionRange.transform.localPosition.z);
                    boxCollider.offset = new Vector2(boxCollider.offset.x * -1, boxCollider.offset.y);
                }
            }

            // update the scale
            transform.localScale = localScale;
        }
        
    }

    void DoJump()
    {
        //make the player jump

        // reset current vertical motion to 0 prior to jump
        _vxy.y = 0f;
        rb.velocity = Vector2.zero;
        // add a force in the up direction
        rb.AddForce(new Vector2(0, jumpForce*rb.mass*rb.gravityScale));
        hasplayed = false;
    }


    public void RestoreForm()
    {
        //This function will deposses an object and restore Laramie to his normal form
        StartCoroutine(PossessionPause(false));
        //Debug.Log(Name + " here");
        this.gameObject.tag = "Object";
        isPossessed = false;
        _InRange = false;
        //This function will make Laramie reappear in the scene and start setting him back up
        ObjectBehaviorTrigger ghostObjectBehaviorTrigger = Ghost.GetComponentInParent<ObjectBehaviorTrigger>();
        ghostObjectBehaviorTrigger.PossessObject();
        ghostObjectBehaviorTrigger.controlsDisabled = controlsDisabled;
        if (ghostObjectBehaviorTrigger.controlsDisabled)
        {
            ghostObjectBehaviorTrigger.TriggerFade(true);
        }
        //Ghost.GetComponentInParent<ObjectBehaviorTrigger>().PossessObject();

        if (_ObjectInRange != null)
        {
            //This makes it so the object you just depossesd will start glowing again to show that it's still range to be possessed
            _ObjectInRange.GetComponentInParent<ObjectBehaviorTrigger>().OutOfRangeGlow();
        }
        _ObjectInRange = null;
        theGlow.GetComponent<SpriteRenderer>().color = Color.blue;
        theGlow.SetActive(false);
        if (this.gameObject != Ghost)
        {
            ghostaud.PlayOneShot(depossess);
            //Move Laramie tot he old objects position
            Vector2 newGhostPosition = new Vector2(this.transform.position.x, this.transform.position.y + 1);
            Ghost.transform.position = newGhostPosition;
            controlsDisabled = false;
            Ghost.GetComponent<Animator>().SetTrigger("PossessOut");
            
        }
        if (type == "object")
        {
            //sets the formorly possessed object back to normal
            _animator.SetBool("Possessed", false);
            gameObject.layer = 0;
            if (Name == "Power Strip")
            {
                gameObject.layer = 19;
            }
            rb.mass = _baseMass;
            rb.sharedMaterial = null;
        }
        
        ExtendedRestore();
    }

    void SwitchPossession()
    {
        //This function will allow to actually posses an object when in Laramie's form or switch between near by objects
        //when currently possessing something

        if(_ObjectInRange != null)
        {
            //Debug.Log(Name + " here");
            this.gameObject.tag = "Object";
            isPossessed = false;
            _InRange = false;
            _ObjectInRange.GetComponentInParent<ObjectBehaviorTrigger>().PossessObject();
            _ObjectInRange = null;
            theGlow.GetComponent<SpriteRenderer>().color = Color.blue;
            if(Name == "Laramie")
            {
                //set Laramie's currently in range object to null and will prep him to possess an object
                OutOfRange();
                this.gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(0, 0);
                ghostaud.PlayOneShot(possess);
                _animator.SetTrigger("PossessIn");
                this.gameObject.GetComponentInChildren<ObjectPossessionTrigger>().DisablePossessionRange();

            }
            else if (type == "object")
            {
                //This will set the previously possessed object back to normal
                _animator.SetBool("Possessed", false);
                gameObject.layer = 0;
                rb.mass = _baseMass;
                rb.sharedMaterial = null;
            }
            theGlow.SetActive(false);
            //_ghostCode.possUI.EnablePosessionUI(false);
        }
        else
        {
            //Debug.Log("Out of Range");
        }
    }

    public void PossessObject()
    {
        //This function will handle all of the switching that is required to possess an object and to restore Laramie back to
        //his normal form

        this.gameObject.tag = "Player";
        isPossessed = true;
        if (Name == "Laramie")
        {
            spriteRend.enabled = true;
            this.gameObject.GetComponentInChildren<ObjectPossessionTrigger>().EnablePossessionRange();
            theGlow.SetActive(false);
            //_ghostCode.possUI.EnablePosessionUI(false);
            ResetMaterial();
            changeButtonUI(0);
            gm.SetUpLinearRumble(false);
            
        }
        else if (type == "object")
        {
            ghostaud.PlayOneShot(switchpossess);
            _animator.SetBool("Possessed", true);
            theGlow.SetActive(false);
            //_ghostCode.possUI.EnablePosessionUI(false);
            ResetMaterial();
            gameObject.layer = 10;
            rb.mass = _baseMass * possessMassMod;
            rb.sharedMaterial = frictionless;
            //this function will make it so when possessing an object the people dialogue will no longer show up
            FindObjectOfType<DialogueManager>().EndDialogue();
            if (Name == "Flying Object")
            {
                changeButtonUI(0);
            }
            else
            {
                changeButtonUI(1);
            }

            gm.SetUpLinearRumble();
        }
        else
        {
            theGlow.GetComponent<SpriteRenderer>().color = Color.green;
            theGlow.SetActive(true);
        }
        
        gm.SetUpHearts(hearts);
        gm.ChangeObjectUI(Name);

        //mm.currentPlayerObject = this;
        ExtendedPossess();

    }

    public void InRange(GameObject possessableObject){
        //Determines the closest object to Laramie. 

        if (!gm.PossessPressedLast)
        {
            if (_ObjectInRange == null)
            {
                //Debug.Log(possessableObject.GetComponentInParent<ObjectBehaviorTrigger>().Name + " Entered Range");
                _InRange = true;
                _ObjectInRange = possessableObject;
                possessableObject.GetComponentInParent<ObjectBehaviorTrigger>().InRangeGlow();
            }
            else
            {

                float currentObjectDistance = Vector2.Distance(this.gameObject.transform.position,
                    _ObjectInRange.transform.parent.position);
                float newObjectDistance = Vector2.Distance(this.gameObject.transform.position,
                    possessableObject.transform.parent.position);

                if (newObjectDistance < currentObjectDistance)
                {
                    _ObjectInRange.GetComponentInParent<ObjectBehaviorTrigger>().OutOfRangeGlow();
                    _InRange = true;
                    _ObjectInRange = possessableObject;
                    possessableObject.GetComponentInParent<ObjectBehaviorTrigger>().InRangeGlow();
                }

            }
        }

    }

    public void InRangeGlow()
    {
        //turns on an object's glow

        if (Name != "Laramie" && Name != "Person" && !gm.PossessPressedLast)
        {
            theGlow.SetActive(true);
            spriteRend.material = matRange;
            /*
            if (_ghostCode != null)
            {
                _ghostCode.possUI.EnablePosessionUI(true);
            }
            */
        }
    }

    public void OutOfRange()
    {
        //Clears an objects in range values
        //Debug.Log(Name + " Exited Range");
        _InRange = false;
        _ObjectInRange = null;
    }

    public void OutOfRangeGlow()
    {
        theGlow.SetActive(false);
        ResetMaterial();
        /*
        if (_ghostCode != null)
        {
            //_ghostCode.possUI.EnablePosessionUI(false);
        }
        */
    }

    /*private void OnDestroy()
    {
        gm.PossessPressedLast = false;
        
        //StopCoroutine(PossessionPause(false));
        
    }*/

    void OnCollisionEnter2D(Collision2D col)
    {
        //Play collision sound when it makes contact.
        if (hasplayed == false && col.relativeVelocity.y > 0)
        {
            objectaud.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
            objectaud.Play();
            hasplayed = true;
            if (soundIndicator != null)
            {
                Instantiate(soundIndicator, transform);
            }
        }
        
        //Play damage sound if the object is damaged.
        /*if (dmgaud && col.relativeVelocity.magnitude > physDamageThreshold)
        {
            dmgaud.PlayOneShot(damage);
        }*/
        //If the object can take damage
        if (physDamageAmount != 0)
        {
            PillowProperty pillow = col.gameObject.GetComponent<PillowProperty>();
            //If the velocity magnitude passes the damage threshold
            if (col.relativeVelocity.magnitude > physDamageThreshold && pillow == null)
            {
                
                DamagePlayer();
            }
            else if (pillow)
            {
                //Half the velocity when colliding with a pillow
                rb.velocity /= 2;
            }
        }
    }

    //This function restores the objects health and can respawn it
    public void ResetObject()
    {
        //Make Laramie leave
        RestoreForm();
        //Reset the health
        health = initialHealth;
        _animator.SetBool("HighHealth", true);
        _animator.SetBool("MidHealth", false);
        _animator.SetBool("LowHealth", false);
        for (int i = 0; i < hearts.Count; ++i)
        {
            hearts[i].currentState = HeartHitPoint.HeartStates.Whole;
        }
        activeHeartIndex = hearts.Count - 1;
        //Respawn the object if you want
        if (respawnPoint)
        {
            transform.position = respawnPoint.position;
            rb.velocity = Vector2.zero;
        }
    }

    public void DamagePlayer()
    {
        //Debug.Log("PHYSDMGAMT: " + physDamageAmount);
        //Debug.Log("Invincible?: " + currentlyInvincible);
        //Debug.Log("CanTakeDMG: " + canTakeDamage);
        if (physDamageAmount != 0 && !currentlyInvincible && canTakeDamage)
        {
            //Damage the player
            health -= 1;
            
            hearts[activeHeartIndex].ProgressState();
            if (hearts[activeHeartIndex].currentState == HeartHitPoint.HeartStates.Dead)
            {
                activeHeartIndex--;
            }
            
            //If the sprite has multiple states and the object is not dead
            if (health > 0)
            {
                if (dmgaud != null && damage != null)
                {
                    dmgaud.PlayOneShot(damage);
                    if (isPossessed)
                    {
                        gm.SetUpTapRumble();
                    }
                }
                if (hurtFX != null)
                {
                    Instantiate(hurtFX, gameObject.transform.position, new Quaternion());
                }
                //Switch states depending on health
                if (health <= hearts.Count && health > 2)
                {
                    _animator.SetBool("MidHealth", true);
                    _animator.SetBool("HighHealth", false);
                    _animator.SetBool("LowHealth", false);
                }
                else if (health <= 2)
                {
                    _animator.SetBool("LowHealth", true);
                    _animator.SetBool("HighHealth", false);
                    _animator.SetBool("MidHealth", false);
                }
                invincibleEndTime = Time.time + invincibilityTime;
                currentlyInvincible = true;
                if (isPossessed)
                {
                    gm.DamageHeart(hearts);
                }
            }
            else if (health <= 0)
            {
                NoHealthAction();
            }
            if (matWhite)
            {
                spriteRend.material = matWhite;
                Invoke("ResetMaterial", 0.1f);
                StartCoroutine(InvincibleFlash());
                //Camera.current.GetComponent<Animator>().SetTrigger("Shake");
                if (cam && isPossessed)
                {
                    cam.SetTrigger("Shake");
                }
            }
            //Instantiate a sound detection object if there is an audio clip to play
            if (dmgaud != null && dmgaud.clip != null)
            {
                if (soundIndicator)
                {
                    Instantiate(soundIndicator, transform);
                }
            }
        }
    }

    public virtual void NoHealthAction()
    {
        DestroyObject();
    }

    public void DestroyObject()
    {
        if (dmgaud)
        {
            dmgaud.PlayOneShot(destroy);
        }
        if (dieFX != null)
        {
            Instantiate(dieFX, gameObject.transform.position, new Quaternion());
        }

        if (manager.GetComponent<GameManager>().targetObjects.Contains(gameObject))
        {
            if (manager.GetComponent<GameManager>().targetObjects.Count > 1)
            {
                manager.GetComponent<GameManager>().targetObjects.Remove(gameObject);
            }
            else
            {
                manager.GetComponent<GameManager>().loseGame();
            }
        }
        if (isPossessed)
        {
            RestoreForm();
            gm.PossessPressedLast = false;
        }

        Destroy(gameObject);
    }

    IEnumerator InvincibleFlash()
    {
        //Debug.Log("Invincible flash");
        while(currentlyInvincible && Time.time < invincibleEndTime)
        {
            //Debug.Log("Flashing");
            spriteRend.material = matWhite;
            yield return new WaitForSeconds(.2f);
            ResetMaterial();
            yield return new WaitForSeconds(.2f);
        }
    }

    IEnumerator PossessionPause(bool state)
    {
        possessionPaused = true;
        yield return new WaitForSeconds(.1f);
        gm.PossessPressedLast = state;
        possessionPaused = false;
    }

    /// <summary>
    /// Function to allow player to drop through the platform
    /// </summary>
    void PlatformDrop()
    {
        // Disable collision between player layer and platform
        Physics2D.IgnoreLayerCollision(_playerLayer, _platformLayer, true);
        // Restore it after specified amount of time
        Invoke("RestorePlatformCols", platformDropTime);
    }

    /// <summary>
    /// Function to restore the collision between player and platform
    /// </summary>
    void RestorePlatformCols()
    {
        Physics2D.IgnoreLayerCollision(_playerLayer, _platformLayer, false);
    }

    void ResetMaterial()
    {
        spriteRend.material = matDefault;
    }

    public void HideLaramie()
    {
        spriteRend.enabled = false;
    }

    public void ApplyForce(int forceX, int forceY=0, bool useMass=true)
    {
        if (this.gameObject != Ghost)
        {
            //Debug.Log(Name + " Receieving force");
            float objectMass = rb.mass;
            float objectGravity = 1;
            if (useMass)
            {
                //Debug.Log("using mass");
                objectMass = rb.mass;
                objectGravity = rb.gravityScale;

            }
            //_vxy.x += forceToApply;
            rb.AddForce(new Vector2(forceX * objectMass * objectGravity, forceY * objectMass * objectGravity));
        }
    }

    public void changeButtonUI(int buttonInt)
    {
        gm.ChangeDisplayedButtons(buttonInt);
    }

    public void PlayNetPhaseAnim()
    {
        _animator.SetTrigger("HitNet");
    }

    /*
     * Empty function that derived classes can use. Will run every update() call
     */
    public virtual void ExtendedUpdate()
    {

    }

    public virtual void ExtendedStart()
    {

    }

    protected virtual void ExtendedAwake()
    {
        
    }

    public virtual void ExtendedPossess()
    {
        
    }

    public virtual void ExtendedRestore()
    {
        
    }

    public virtual void ActivateAbility()
    {

    }

    //Used solely for telling the GM to trigger a debug skip
    public void TriggerDebug()
    {
        if (gm != null && gameObject.tag == "Player")
        {
            gm.ProcessDebugButton();
        }
    }

    //This function is used to activate debug behavior to make the object move to
    //it's specified debug position to progress the level
    public override void DebugComplete()
    {
        if (activateOnDebug)
        {
            ActivateAbility();
        }
        else
        {
            transform.position = debugGoalPosition;
        }
    }

    public Vector2 GetMove()
    {
        return _move;
    }

    public void TriggerFade(bool fadeOut)
    {
        if (Name == "Laramie")
        {
            if (fadeOut)
            {
                
                _animator.SetBool("FadeOut", true);
            }else
            {
                _animator.SetBool("FadeOut", false);
                _animator.SetTrigger("FadeIn");
                spriteRend.enabled = true;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Name == "Firework")
        {
            BoxCollider2D cerealCollider = GetComponent<BoxCollider2D>();
            //float halfWidth = (float)Math.Round(spriteRend.size.x / 2, 1);
            //float halfHeight = (float)Math.Round(spriteRend.size.y / 2, 1);
            bool useWidth = false;
            float objectRot = Mathf.Abs(this.transform.localEulerAngles.z);
            Debug.Log(objectRot);
            if ((45f <= objectRot && objectRot < 135f) || (225f <= objectRot && objectRot < 315f))
            {
                        
                useWidth = false;
                        
            }else if ((315f <= objectRot || objectRot < 45f) || (135f <= objectRot && objectRot < 255f))
            {

                useWidth = true;
            }
            Debug.Log(useWidth);
            Gizmos.color = Color.green;
            if (useWidth)
            {
                Gizmos.DrawLine(
                    new Vector3(transform.position.x + halfWidth, transform.position.y, transform.position.z),
                    new Vector3(_rollGroundCheck.x + halfWidth, _rollGroundCheck.y, _rollGroundCheck.z));
                Gizmos.DrawLine(
                    new Vector3(transform.position.x - halfWidth, transform.position.y, transform.position.z),
                    new Vector3(_rollGroundCheck.x - halfWidth, _rollGroundCheck.y, _rollGroundCheck.z));
            }
            else
            {
                Gizmos.DrawLine(
                    new Vector3(transform.position.x + halfHeight, transform.position.y, transform.position.z),
                    new Vector3(_rollGroundCheck.x + halfHeight, _rollGroundCheck.y, _rollGroundCheck.z));
                Gizmos.DrawLine(
                    new Vector3(transform.position.x - halfHeight, transform.position.y, transform.position.z),
                    new Vector3(_rollGroundCheck.x - halfHeight, _rollGroundCheck.y, _rollGroundCheck.z));
            }
        }
    }
}
