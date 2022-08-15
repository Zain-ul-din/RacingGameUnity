﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using RandomsUtilities;


[RequireComponent(typeof (CharacterController))]
public class PlayerController : MonoBehaviour {

    private CharacterController characterController;
    [SerializeField] private float speed = 6.0f;
    [SerializeField] private Camera _camera;
    [SerializeField] private List<Vector3> lines;
    [SerializeField] private float cameraOffSet = 20f;
    [SerializeField] private int currentIdx = 1;
    [SerializeField] private float lerpDuration = 0.5f;

    [SerializeField] private Animator _animator;

    [SerializeField] private float slideTime = 2f;
    [SerializeField] private float gravity = 9.8f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float jumpTime = 0.5f;
   
    [SerializeField] private Transform meshTransform;
    [SerializeField] private float rayCastDistance = 1.5f;
    [Space (10)] [SerializeField] private List <string> tagsToCompare;
    [SerializeField] private LayerMask groundLayer;

    private bool isJumping;
    private float vVelocity;
    private Collider characterCollider;
    private float SPEED ;

    private void Awake() {
        characterController = GetComponent<CharacterController>();
        characterCollider = GetComponent <CharacterController> ();
        SPEED = speed;
        transform.position = new Vector3(lines[currentIdx].x  , transform.position.y , transform.position.z);
    }

    private IEnumerator Start() {
        while (true) {
            yield return new WaitForSeconds(5f);
            speed += Time.deltaTime;
        }
    }
    
    private void Update() {
        CharacterController (); 
    }
    
    private void CharacterController () {
        bool isGrounded = IsGrounded ();

        slideTime -= Time.deltaTime;
        
        if (slideTime > 0) {
            _animator.SetBool("Go", true);
        } else {
            _animator.SetBool("Go", false);
            
            if (isGrounded)
                _animator.SetBool("Idle" , true); 
        }
       
        float z = 1f;
        Vector3 move = transform.forward * z * speed * Time.deltaTime;
        vVelocity -= gravity * Time.deltaTime;
        move.y = vVelocity * Time.deltaTime;
        characterController.Move(move);
        
        // move player left and right
        if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveLeft();
        else if (Input.GetKeyDown(KeyCode.RightArrow)) MoveRight();
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
        }

        GetTouchInput();
        SmoothlyLerpSpeed ();
    }
    
    private bool IsGrounded () {
        if (Physics.Raycast (transform.position , Vector3.down , out RaycastHit hitInfo , rayCastDistance , groundLayer)){   
            Debug.DrawLine (transform.position , hitInfo.point , Color.black);// !debug
            return true;
        }
        return false;
    }

    private void MoveLeft() {
        if (CanMoveLeft()) {
            currentIdx -= 1;
            // _animator.SetFloat("Direction" , -1);

            _animator.SetBool("Idle" , false);
            transform.DOMoveX(lines[currentIdx].x , lerpDuration).SetEase(Ease.InOutSine).OnComplete(() => {
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
            // _animator.SetFloat("Direction" , 1);
            _animator.SetBool("Idle" , false);
                
            transform.DOMoveX(lines[currentIdx].x , lerpDuration).SetEase(Ease.InOutSine).OnComplete(() => {
                _animator.SetBool("Idle" , true);
            });
               
            // rotate in y direction
            meshTransform.DORotate(new Vector3(0, 45, 0), lerpDuration ).OnComplete(() => {
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
        
        if(yDistance>xDistance)
        {
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
       HandleCarCollision (other);
    }
 
    private int coins = 0;
    private void OnTriggerEnter (Collider other) {
        Debug.Log ("Collide");
        if (tagsToCompare.Contains (other.gameObject.tag)) {
           coins += 1;
           GameManager.Instance.gameState.coins += 1;
           SoundManager.Instance.PlaySound (SoundManager.SoundClip.CoinSound);
           other.gameObject.SetActive (false);
        }
    }

   private void LateUpdate() =>
       _camera.gameObject.transform.position = new Vector3(_camera.transform.position.x , _camera.transform.position.y, transform.position.z - cameraOffSet);

    // Helper Functions
    private bool CanMoverRight () => currentIdx < lines.Count - 1;
    private bool CanMoveLeft () => currentIdx > 0;   

    private void HandleCarCollision (Collision other) {
        var otherTransform = other.gameObject.transform;
        
        if (!Utilities.IsPlayerBehindGameObject (transform , otherTransform)) return; // player should be behind car
        
        _animator.SetBool ("IsCollide" , true);
        otherTransform.DOMoveZ (otherTransform.position.z + 60f + UnityEngine.Random.Range (5 , 20)  , 1f)
        .SetEase (Ease.OutSine)
        .OnComplete(()=>{
          _animator.SetBool ("IsCollide" , false);
        }); // OutFlash
        
        // change random line
        int lOrR = UnityEngine.Random.Range (0,2);
        Debug.Log (lOrR);
        if (lOrR == 0) {
           if (CanMoveLeft()) MoveLeft ();
           else MoveRight ();
        }
        else {
           if (CanMoverRight()) MoveRight ();
           else MoveLeft();
        }
        
        // decrease speed
        speed /= 3;
    }

    private void SmoothlyLerpSpeed () {
      float smoothTime = 0.01f;
      float yVelocity = 0.0f;
      speed = Mathf.SmoothDamp(speed, SPEED, ref yVelocity, smoothTime); 
    }
}



