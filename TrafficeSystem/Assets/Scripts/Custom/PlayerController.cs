﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using RandomsUtilities;

[RequireComponent(typeof (CharacterController))]
public class PlayerController : MonoBehaviour {
    
    public static PlayerController Instance {get; private set;}
    
    // events
    public event EventHandler <float> OnHitCoin; 
    public event EventHandler <float> OnEnergyChange;

    private CharacterController characterController;
      
    [SerializeField] private float speed = 6.0f;
    [SerializeField] private float boostSpeed = 20f;
    [SerializeField] private float boostTime = 2f;

    [SerializeField] private List<Vector3> lines;
    [SerializeField] private int currentIdx = 1;
    [SerializeField] private float lerpDuration = 0.5f;

    public Animator _animator {get; private set;}

    [SerializeField] private float slideTime = 2f;
    [SerializeField] private float gravity = 9.8f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float jumpTime = 0.5f;
    [SerializeField] private float maxSpeed = 150f;
   
    [SerializeField] private Transform meshTransform;
    [SerializeField] private float rayCastDistance = 1.5f;
    [Space (10)] [SerializeField] private List <string> tagsToCompare;
    [SerializeField] private LayerMask groundLayer;
    [Space(10)][SerializeField] private LayerMask carLayerMask;  

    [Space(20)] [Header ("Control Buttons Reference!")]
    [SerializeField] private ButtonController leftBtn;  
    [SerializeField] private ButtonController rightBtn;
    
    private bool isJumping;
    private float vVelocity;
    private float SPEED ;
    private float _boostTime;
    private bool isDelayTime;
    private float touchDelayTime = 0.2f;
    private float _touchDelayTime;
    public bool isNitroEffectPlaying;
    private bool useBoost;
    
    [SerializeField] private float energyDecrementEach2Sec = 5f;
    [SerializeField] private float magnetEffectTime = 5f;
    private float playerEnergy;
    private float  _magnetEffectTime;
    private void Awake() {
        if (Instance) Destroy (this);
        _touchDelayTime = touchDelayTime;
        Instance = this;
        characterController = GetComponent<CharacterController> ();
        SPEED = speed;
        _boostTime = boostSpeed;
        _magnetEffectTime = magnetEffectTime;
        magnetTurnOffAction = new Utilities.ActionHandler ();
        magnetEffectTime = 0;
        transform.position = new Vector3(lines[currentIdx].x  , transform.position.y , transform.position.z);
        _animator = GetComponentInChildren <Animator> ();
        playerEnergy = 100;
    }

    private IEnumerator Start() {
        while (true) {
            yield return new WaitForSeconds(5f);
            speed += Time.deltaTime;
            playerEnergy -= energyDecrementEach2Sec;
            OnEnergyChange?.Invoke (this, playerEnergy);
        }
    }
    
    private void Update() => CharacterController ();  
    
    private void CharacterController () {
        speed = Mathf.Clamp (speed, 0f, maxSpeed);
        bool isGrounded = IsGrounded ();

        slideTime -= Time.deltaTime;
        
        if (slideTime > 0) {
            _animator.SetBool("Go", true);
        } else {
            _animator.SetBool("Go", false);
            if (isGrounded) _animator.SetBool("Idle" , true); 
        }
       
        float z = 1f;
        Vector3 move = transform.forward * z * speed * Time.deltaTime;
        vVelocity -= gravity * Time.deltaTime;
        move.y = vVelocity * Time.deltaTime;
        characterController.Move(move);
        
        // move player left and right
        if (Input.GetKey(KeyCode.LeftArrow)) MoveLeft();
        else if (Input.GetKey(KeyCode.RightArrow)) MoveRight();
        else _animator.SetFloat("Direction" , 0);
        
        if (isGrounded) {
            isJumping = false;
            _animator.SetBool("Jump" , false);
            vVelocity = 0;
            if (Input.GetKeyDown(KeyCode.Space)) Jump();
        } else isJumping = true;
        
        if (isJumping) {
            _animator.SetBool("Jump" , true);
            _animator.SetBool("Idle" , false);
            _animator.SetBool ("IsCollide" , false);
        }

        // if (Input.GetKeyDown(KeyCode.DownArrow)) MoveDown();

        GetTouchInput();
        SmoothlyLerpSpeed ();
        AddTouchDelay ();
        MagnetEffect ();
        if (Input.GetKey(KeyCode.LeftShift) || useBoost) NitroEffect ();
        if (Input.GetKeyUp(KeyCode.LeftShift)) { 
            DisableNitroEffect();
        }
    }
    
    private bool IsGrounded () {
        if (Physics.Raycast (transform.position , Vector3.down , out RaycastHit hitInfo , rayCastDistance , groundLayer)){   
            #if UNITY_EDITOR
             Debug.DrawLine (transform.position , hitInfo.point , Color.black);// !debug
            #endif
            return true;
        }
        return false;
    }
    
    private void AddTouchDelay () {
        _touchDelayTime -= Time.deltaTime;
        isDelayTime = _touchDelayTime > 0;
        if (_touchDelayTime <= 0) _touchDelayTime = 0;
    }

    private void MoveLeft() {
        if (CanMoveLeft()) {
            currentIdx -= 1;
            _touchDelayTime = touchDelayTime;  
            // _animator.SetFloat("Direction" , -1);
            _animator.SetBool("Idle" , false);
            transform.DOMoveX(lines[currentIdx].x , lerpDuration).SetEase(Ease.Linear).OnComplete(() => {
                _animator.SetBool("Idle" , true);
            });
            // rotate in y direction
            meshTransform.DORotate(new Vector3(0, -45, 0), lerpDuration ).OnComplete(() => {
                meshTransform.DORotate(new Vector3(0, 0, 0), lerpDuration );
            });
        }
    }

    private void MoveRight() {
        
        if (CanMoverRight ()) {
            currentIdx += 1;
            _touchDelayTime = touchDelayTime;  
            // _animator.SetFloat("Direction" , 1);
            _animator.SetBool("Idle" , false);
            transform.DOMoveX(lines[currentIdx].x , lerpDuration).SetEase(Ease.Linear).OnComplete(() => {
                // _animator.SetBool("Idle" , true);
            });  
            // rotate in y direction
            meshTransform.DORotate(new Vector3(0, 55, 0), lerpDuration ).OnComplete(() => {
                meshTransform.DORotate(new Vector3(0, 0, 0), lerpDuration);
            });
        }
    }
    
    private void Jump() {
        if (isJumping) return;
        isJumping = true;
        _animator.SetBool("Idle" , false);
        _animator.SetBool("Jump" , true);
        transform.DOMoveY(jumpHeight, jumpTime);
        // vVelocity = jumpSpeed;
    }

    private void MoveDown () {
        if (!isJumping) return;
        isJumping = false;
        DOTween.KillAll (this.transform);
        _animator.SetBool ("Idle" , true);
        _animator.SetBool ("Jump" , false);
        vVelocity = 0;
    }
    
    private void NitroEffect () {
      if (!useBoost) return;  
      if (_boostTime <= 0) {
        _boostTime  = boostTime;
        DisableNitroEffect();
        return;
      }
      
      if (!isNitroEffectPlaying) speed += boostSpeed;
      isNitroEffectPlaying = true;
      ParticleSpawnManager.Instance.InstantiateParticle (
       ParticleSpawnManager.ParticleType.NitroEffect,
       transform.position,
       transform
      );
      
      _boostTime -= Time.deltaTime;
    }
    
    private void DisableNitroEffect () {
        useBoost = false;
        speed -= boostSpeed;
        isNitroEffectPlaying = false;
        ParticleSpawnManager.Instance.DiableParticle (ParticleSpawnManager.ParticleType.NitroEffect);
    }
    [SerializeField] private float minSwipeDistance=50f;// In -> px
    [SerializeField] private float maxSwipeTime=0.5f;// Max Time Requried to move 
    private float _swipeTime;// Total Swipe Time
    
    // Swipe Time
    private float _swipeEndTime;// Time at Swipe End
    private float _swipeStartTime;// Time at Swipe Start
    private float _swipeLength;// Lenght of Swipe
    // Swipe Pos
    private Vector2 _startSwipePos;// Swipe Start pos
    private Vector2 _endSwipePos;// End pos
    
    private void GetTouchInput() {
       
       #if UNITY_EDITOR || UNITY_EDITOR_WIN  // if unity editor
        if (Input.touchCount>0)
        {
            Touch touch = Input.GetTouch(0); // Take 1st Touch by User
            if(touch.phase==TouchPhase.Began)// Touch starts 
            {
                _swipeStartTime = Time.time;
                _startSwipePos = touch.position;
                // Get Time & Position of touch
            }
            else if(touch.phase==TouchPhase.Ended)// ?? Touch ended
            {
                _swipeEndTime = Time.time;
                _endSwipePos = touch.position;
                // Get Time & pos where Touch ended

                _swipeTime = _swipeEndTime - _swipeStartTime;//Check how long User Swipe
                _swipeLength = (_endSwipePos - _startSwipePos).magnitude;// Check Lenght
                
                if(_swipeTime >= maxSwipeTime && _swipeLength > minSwipeDistance)// Time & Distance 
                {
                    SwipeControl();
                }
            }
        }
        #elif UNITY_ANDROID || UNITY_IPHONE
          if (leftBtn.isPressing || rightBtn.isPressing){
             if (leftBtn.isPressing) MoveLeft();
             if (rightBtn.isPressing) MoveRight();
          }
        #endif
        else {
            _animator.SetFloat("Direction" , 0);
        }
    }// getinput <-
    
    
    void SwipeControl() {
       
        Vector2 Distance = _endSwipePos - _startSwipePos;// get pos of Touch 
        float xDistance = Mathf.Abs(Distance.x);
        float yDistance = Mathf.Abs(Distance.y);
        // Abs return's Positive value

        // -> for x Movement
        if(xDistance>yDistance) {
            if(Distance.x>0)// Swipe Right
            {
                MoveRight();
            }
            if(Distance.x<0)// Swipe Left
            {
                MoveLeft();
            }       
        }

        // -> for Y Movement
        if(yDistance>xDistance) {
            if (characterController.isGrounded) {
                _animator.SetBool("Jump" , false);
                vVelocity = 0;
                if(Distance.y>0) // Swipe up
                    Jump();
            }
            if(Distance.y<0)// Swipe Down
            {
                // do noting
            }
        }
    }// <- Swipe control

    private void OnCollisionEnter (Collision other) {
        if (other.gameObject.tag == "TrafficCar") {
            if(useBoost)
             DestoryCarOnCollision (other);
            else
             HandleCarCollision (other);
        }
    }
    
    private int coins = 0;
    private void OnTriggerEnter (Collider other) {
        if (tagsToCompare.Contains (other.gameObject.tag)) {

           if (other.gameObject.tag == "Coin"){
            coins += 1;
            GameManager.Instance.gameState.coins += 1;
            OnHitCoin?.Invoke (this , coins);
            SoundManager.Instance.PlaySound (SoundManager.SoundClip.CoinSound);
            ParticleSpawnManager.Instance.InstantiateParticle (
             ParticleSpawnManager.ParticleType.CoinHitEffect,
             transform.position + transform.forward * 0.5f,
             transform
            );
            other.gameObject.SetActive (false);
           } else if (other.gameObject.tag == "Booster") {
             useBoost = true;
             _boostTime = boostTime;
             ParticleSpawnManager.Instance.InstantiateParticle (
                ParticleSpawnManager.ParticleType.BoostPickUpEffect,
                transform.position + transform.forward * 0.5f,
                transform
             );
             other.gameObject.SetActive(false);
            //  StressReceiver.Instance.InduceStress (boostTime);
           }
           else if (other.gameObject.tag == "Energy") {
            playerEnergy = 100;
            OnEnergyChange?.Invoke (this, playerEnergy);
            ParticleSpawnManager.Instance.InstantiateParticle (
                ParticleSpawnManager.ParticleType.EnergyPickUpEffect,
                transform.position + transform.forward * 0.5f,
                transform
            );
            other.gameObject.SetActive (false);
           }
           else if (other.gameObject.tag == "Magnet") {
            other.gameObject.SetActive (false);
            magnetEffectTime = _magnetEffectTime;
            ParticleSpawnManager.Instance.InstantiateParticle (
             ParticleSpawnManager.ParticleType.MagnetEffect,
             transform.position + Vector3.up * 0.5f + Vector3.forward * 0.3f,
             transform
            );
            magnetTurnOffAction.canInvokeAction = true;
           }
        }
    }

    // Helper Functions
    private bool CanMoverRight () => currentIdx < lines.Count - 1 && !isDelayTime;
    private bool CanMoveLeft () => currentIdx > 0 && !isDelayTime;   

    private void HandleCarCollision (Collision other) {
        var otherTransform = other.gameObject.transform;
        
        if (otherTransform.position.z < transform.position.z - 5f && !isJumping)  {
            otherTransform.gameObject.SetActive(false);
            return;
        }
        
        // Draw RayCast to check player is behind vehicle
        if (Physics.Raycast (transform.position, transform.forward, out RaycastHit hitInfo, 1.5f,  carLayerMask)){
              
          SoundManager.Instance.PlaySound (SoundManager.SoundClip.CrashSound);
          StressReceiver.Instance.InduceStress(0.5f);
          _animator.SetBool ("IsCollide" , true);
          otherTransform.DOMoveZ (otherTransform.position.z + 40f + UnityEngine.Random.Range (5 , 10)  , 1f)
          .SetEase (Ease.OutSine)
          .OnComplete(()=>{
            _animator.SetBool ("IsCollide" , false);
          }); // OutFlash
          
          ParticleSpawnManager.Instance.InstantiateParticle (
            ParticleSpawnManager.ParticleType.HitEffect,
            transform.position,
            transform
          );

          // change random line
          int lOrR = UnityEngine.Random.Range (0,2);
         
          if (lOrR == 0) {
             if (CanMoveLeft()) 
               MoveLeft ();
             else 
               MoveRight ();
          }
          else {
             if (CanMoverRight()) 
               MoveRight ();
             else 
               MoveLeft();
          }
          
          speed /= 2;
        }
    }
    
    private void DestoryCarOnCollision (Collision other) {
        var otherTransform = other.gameObject.transform;
        var spawnPos = new Vector3 (otherTransform.position.x, otherTransform.position.y + 2f, transform.position.z + 1f);
        ParticleSpawnManager.Instance.InstantiateParticle (ParticleSpawnManager.ParticleType.DestoryEffect, spawnPos, transform);
        StressReceiver.Instance.InduceStress(0.5f);
        SoundManager.Instance.PlaySound (SoundManager.SoundClip.DestorySound);
        otherTransform.DOMoveY (30, 0.3f);        
    }

    private void SmoothlyLerpSpeed () {
      float smoothTime = 0.2f;
      float yVelocity = 0.0f;
      speed = Mathf.SmoothDamp(speed, SPEED, ref yVelocity, smoothTime); 
    }
    
    private static float SmoothDampBtwValues (float a , float b , float smoothTime) {
      float yVelocity = 2f;
      float result = Mathf.SmoothDamp (a , b , ref yVelocity , smoothTime);
      return result;
    }
    
    internal Utilities.ActionHandler magnetTurnOffAction;

    private void MagnetEffect () {
        if (magnetEffectTime <= 0) return;
        
        magnetEffectTime -= Time.deltaTime;
        if (magnetEffectTime < 1f) {
            magnetTurnOffAction.PlayOneShot (()=>{
                ParticleSpawnManager.Instance.DiableParticle (ParticleSpawnManager.ParticleType.MagnetEffect);
            });
            magnetTurnOffAction.canInvokeAction = false;
        }

        TrafficPooling.Instance.IfAnyOfpowerUps (transform, (Transform player, Transform powerUp)=> {
            float megnetFieldRange = player.position.z + 25f;
            float powerUpPosZ = powerUp.position.z;
            return powerUpPosZ <= megnetFieldRange && player.position.z < powerUpPosZ + 5f;
        },(Transform powerUp)=>{
            // Move PowerUp Towards Player
            powerUp.DOMove (this.transform.position, 0.3f);
        });
    }

    public float BoostTime () => _boostTime;
}




