﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RandomsUtilities;
using UnityEngine.UI;
using DG.Tweening;

namespace Randoms {
    public sealed class RandomsPlayerController : MonoBehaviour {
        
        #region Standard

        private CharacterController _cc;
        private Animator _animator;
        
        private void Awake() {
            if (Instance) Destroy(this);
            Instance = this;
            _cc = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
            InitMoveForward();
            InitChangeLines();
            InitActions();
        }

        private void Start() {
            StartMoveForward();
            StartMobInput();
        }
        
        private void Update() {
            MoveForward();
            ChangeLines();
            ManagePhysics();
            if (Input.GetKeyDown(KeyCode.Space)) Jump();
        }
        
        #endregion
        
        #region MoveForward
        
        [Space(10)][Header("Player Movement")]
        [SerializeField] private float speed = 50f;
        [SerializeField] private float walkSpeed = 30f;
        [SerializeField] private float slideTime = 5f;
        private float _currentSpeed;
        
        /// <summary>
        /// Move Forward Initial SetUp on Awake
        /// </summary>
        private void InitMoveForward() {
            _currentSpeed = walkSpeed;
        }
        
        /// <summary>
        ///  Move Forward Wait for instances 
        /// </summary>
        private void StartMoveForward () {
            RandomsAnimationManager.Instance.PlayAnimation(RandomsAnimationManager.AnimType.Slide);
            StartCoroutine(AddIdleDelay(slideTime));
        }
        
        /// <summary>
        /// Moves Player Forward
        /// </summary>
        private void MoveForward() {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, speed, Time.deltaTime * speed);
            var moveDir = transform.forward * speed * Time.deltaTime;
            _cc.Move(moveDir);
        }

        #endregion

        #region ChangeLines

        [Space(10)] [Header("Change Lines")] [SerializeField]
        private List<Vector3> lines;
        [Tooltip("Input Delay in Second")][Range(0,1f)][SerializeField] 
        private float inputDelay = 0.2f;
        [SerializeField] private int initialLine = 0;
        [SerializeField] private float lineChangeSpeed = 3f;

        private float _inputDelay;
        private Vector3 _currentTargetedLine;
        private int _currentLineIdx;
        private bool _canMoveLR;
        private Transform mesh;
        private bool _overRideQuaternion;

        /// <summary>
        /// Jump Initial SetUp on Awake
        /// </summary>
        private void InitChangeLines() { 
          Utilities.QuickSort(0, lines.Count - 1, ref lines, (lhs, rhs) => lhs.x < rhs.x  );
          _currentLineIdx = initialLine;
          _inputDelay = inputDelay;
          _canMoveLR = true;
          mesh = transform.GetChild(0);
          _overRideQuaternion = false;
        }

        private void ChangeLines () {
            int flag;
            
            #if UNITY_EDITOR
             if (Input.GetKey(KeyCode.LeftArrow)) { flag = -1; }
             else if (Input.GetKey(KeyCode.RightArrow)) { flag = 1; }
             else flag = 0;
            #else
             if (leftBtn.isPressing) { flag = -1; }
             else if (rightBtn.isPressing) { flag = 1; }
             else flag = 0;
            #endif
            
            if (flag == 0 || _currentLineIdx == 0 || _currentLineIdx == (lines.Count - 1)) Idle();
            MoveX(flag);
            MoveTowardsCurrentLine();
        }

        /// <summary>
        /// -1 left & 1 right
        /// </summary>
        /// <param name="flag"></param>
        public void MoveX (int flag) {
            if (!_canMoveLR) return;
            if (inputDelay >= 0) inputDelay -= Time.deltaTime;
            if (inputDelay > 0 || flag == 0) return;
            inputDelay = _inputDelay;

            if (flag == 1) MoveRight(flag);
            else if (flag == -1) MoveLeft(flag);
            
        } 
        
        // Helper Functions
        private void MoveLeft(int flag) {
            _currentLineIdx = Mathf.Clamp((_currentLineIdx - 1), 0, lines.Count - 1);
            SetCurrentLine(lines[_currentLineIdx]);
            if (_currentLineIdx == 0) { Idle(); }
            else {
                mesh.DORotate(new Vector3(0, -45, 0), 0.3f);
                // .OnComplete(() => {
                //     if (flag == 0) mesh.DORotate(new Vector3(0, 0, 0), 1f);
                // });   
            }
        }
        
        private void MoveRight(int flag) {
            _currentLineIdx = Mathf.Clamp((_currentLineIdx + 1), 0, lines.Count - 1);
            SetCurrentLine(lines[_currentLineIdx]);
            if (_currentLineIdx == (lines.Count - 1)) { Idle(); }
            else {
                mesh.DORotate(new Vector3(0, 45, 0), 0.3f);
                // .OnComplete(() => {
                //     if (flag == 0) mesh.DORotate(new Vector3(0, 0, 0), 1f);
                // });
            }
        }

        private void Idle() {
            if (_overRideQuaternion) return;
            mesh.rotation = Quaternion.Lerp(mesh.rotation, Quaternion.Euler(0,0,0), Time.deltaTime * 8f);
            // mesh.DORotate(new Vector3(0, 0, 0), 1f);
        }
        
        private void SetCurrentLine (Vector3 line) {
            inputDelay = _inputDelay;
            _currentTargetedLine = line;
        }

        private void MoveTowardsCurrentLine() {
            var targetPos = Utilities.VectorX(transform, _currentTargetedLine.x);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * lineChangeSpeed);
        }

        
        
        #endregion

        #region Physics & Gravity
        
        [Space(10)][Header("Player Physics")]
        [SerializeField] private LayerMask groundLayer;
        [Range(0,-Int32.MaxValue)][Tooltip("Set this value on run time ")][SerializeField] private float groundOffSetFromPlayerPos = -1.13f;
        [Range(1,9.81f)][SerializeField] private float gravity = 9.81f;
        [Range(1,10f)][SerializeField] private float downGravityMultiplier = 1f;
        private bool _isGrounded;
        private float _verticalVelocity;
        private bool _useGravity = true;
        
        /// <summary>
        /// Character Controller Physics handler (isGrounded | Gravity | etc...)
        /// </summary>
        private void ManagePhysics () {
            var isGrounded = Physics.OverlapSphere(transform.position + Utilities.VectorY(Vector3.zero, groundOffSetFromPlayerPos), 0.1f, groundLayer);
            _isGrounded = isGrounded.Length > 0;

            if (_isGrounded) {
                _verticalVelocity = 0f;
                _animator.SetBool("Jump", false);
            }
            else _animator.SetBool("Jump", true);
            
            if (!_isGrounded && _useGravity) {
                // Apply Gravity downward
                _verticalVelocity -= gravity * downGravityMultiplier * Time.deltaTime;
                var moveDir = Utilities.VectorY(Vector3.zero, _verticalVelocity * Time.deltaTime);
                _cc.Move(moveDir);
            }
        }
        
        #endregion

        #region Jump
        
        [Space(10)][Header("Player Jump")]
        [Range(0,100)][SerializeField] private float jumpForce = 10f;
        [SerializeField] private float jumpTime = 0.3f;
        
        /// <summary>
        /// Moves Player Upward
        /// </summary>
        private void Jump() {
            if (_isGrounded) {
                // Jump using Physics 
                // var moveDir = Vector3.up * jumpForce ;
                // _cc.Move(moveDir);
                //_animator.SetBool("Jump", true);
                // Jump using Tween
                transform.DOMoveY(jumpForce, jumpTime);
            }
        }
        
        #endregion

        #region APIS
        
        public static RandomsPlayerController Instance { get; private set; }
        public float Speed() => _currentSpeed;
        public float SetSpeed(float newSpeed) => _currentSpeed = Mathf.Abs(newSpeed);
        public float SetMaxSpeed(float newMaxSpeed) => speed = newMaxSpeed;
     
        public void ChangeRandomLine () {
            var flag = UnityEngine.Random.Range(0, 2);
            if (flag == 0) {
                if (_currentLineIdx > 0) MoveLeft(0);
                else MoveRight(0);
            }
            else {
                if (_currentLineIdx < (lines.Count - 1)) MoveRight(0);
                else MoveLeft(0);
            }
        }

        public void SetGravity(bool state) => _useGravity = state;
        public void CanChangeLines (bool state) => _canMoveLR = state;
        public IEnumerator AddIdleDelay (float time) {
            yield return new WaitForSeconds(time);
            RandomsAnimationManager.Instance.PlayAnimation(RandomsAnimationManager.AnimType.Idle);
        }

        public void OverRideQuaternion(bool state) => _overRideQuaternion = state; 
        
        #endregion

        #region Collsions & Actions
        
        [Space (10)][Header("Player Actions")]
        [SerializeField] private CollideAction collideAction;
        [SerializeField] private BoostAction boostAction;
        [SerializeField] private InvisibleAction invisibleAction;
        [SerializeField] private MagnetAction magnetAction;
        [SerializeField] private FlyAction flyAction;
        
        [Header("Collision Actions")]
        [SerializeField] private float collidePosFromPlayerPos = 1.13f;
        [SerializeField] private LayerMask otherLayerMask;
        private enum ActionTags {
            TrafficCar,
            Booster,
            Shield,
            Magnet,
            Fly,
            Coin,
            Energy
        }
        
        private void InitActions() {
            if (!collideAction) gameObject.AddComponent<CollideAction>();
        }


        private void FixedUpdate() {
            var colliders = Physics.OverlapSphere(transform.position + Utilities.VectorZ(Vector3.zero, collidePosFromPlayerPos), 0.1f, otherLayerMask);
            if (colliders.Length == 0) return;
            var other = colliders[0].gameObject;
            if (other.gameObject.tag == ActionTags.TrafficCar.ToString()) {
                collideAction.TriggerAction(other.transform);
            }
        }

        // private void OnCollisionEnter (Collision other) {
        //     var colliders = Physics.OverlapSphere(transform.position + Utilities.VectorZ(Vector3.zero, collidePosFromPlayerPos), 0.1f, otherLayerMask);
        //     if (colliders.Length == 0) return;
        //     
        //     if (other.gameObject.tag == ActionTags.TrafficCar.ToString()) {
        //         collideAction.TriggerAction(other.transform);
        //     }
        // }
        
        private void OnTriggerEnter(Collider other) {
            if (other.gameObject.tag == ActionTags.Booster.ToString()) {
                boostAction.TriggerAction(other.transform);
            }
            else if (other.gameObject.tag == ActionTags.Shield.ToString()) {
                invisibleAction.TriggerAction(other.transform);
            }
            else if (other.gameObject.tag == ActionTags.Magnet.ToString()) {
                magnetAction.TriggerAction(other.transform);
            }
            else if (other.gameObject.tag == ActionTags.Fly.ToString()) {
                flyAction.TriggerAction(other.transform);
            }
            else if (other.gameObject.tag == ActionTags.Coin.ToString()) {
                PickCoin(other.transform);
            }
            else if (other.gameObject.tag == ActionTags.Energy.ToString()) {
                PickEnergy(other.transform);
            }
        }

        #region PickUp
        
        // TODOS:  Make this better
        private int coins = 0;
        public event EventHandler<float> OnHitCoin;
        
        private void PickCoin (Transform other) {
            SoundManager.Instance.PlaySound(SoundManager.SoundClip.CoinSound);
            ParticleSpawnManager.Instance.InstantiateParticle(
                ParticleSpawnManager.ParticleType.CoinHitEffect,
                transform.position + transform.forward * 0.5f,
                transform
            );
            other.gameObject.SetActive(false);
            coins += 1;
            GameManager.Instance.gameState.coins += 1;
            OnHitCoin?.Invoke(this, coins);
        }
        
        private void PickEnergy(Transform other) {
            ParticleSpawnManager.Instance.InstantiateParticle(
                ParticleSpawnManager.ParticleType.EnergyPickUpEffect,
                transform.position + transform.forward * 0.5f,
                transform
            );
            other.gameObject.SetActive(false);
        }
        
        #endregion
        
        #endregion

        #region MobInput

        [Header("Mobile Input")] [SerializeField]
        private ButtonController leftBtn;
        [SerializeField] private ButtonController rightBtn;
        [SerializeField] private Button jumpBtn;

        // Mob Input Setup on start
        private void StartMobInput() {
            jumpBtn.onClick.AddListener(Jump);
        }
        
        #endregion
    }
}

