using UnityEngine;
using UnityEngine.InputSystem;
using WhaleShark.Gameplay;

namespace WhaleShark.Core
{
    public class InputManager : Singleton<InputManager>
    {
        [Header("Input Actions")]
        public InputActionAsset inputActions;

        InputActionMap playerActionMap;
        InputActionMap uiActionMap;

        // Player Actions
        InputAction moveAction;
        InputAction jumpAction;
        InputAction interactAction;

        // UI Actions
        InputAction cancelAction;

        public Vector2 MoveInput { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool InteractPressed { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            SetupInputActions();
        }

        void Start()
        {
            EnablePlayerInput();
        }

        void SetupInputActions()
        {
            if (inputActions == null) return;

            playerActionMap = inputActions.FindActionMap("Player");
            uiActionMap = inputActions.FindActionMap("UI");

            // Player Actions
            moveAction = playerActionMap?.FindAction("Move");
            jumpAction = playerActionMap?.FindAction("Jump");
            interactAction = playerActionMap?.FindAction("Interact");

            // UI Actions
            cancelAction = uiActionMap?.FindAction("Cancel");

            // 이벤트 연결
            if (jumpAction != null)
            {
                jumpAction.performed += OnJumpPerformed;
                jumpAction.canceled += OnJumpCanceled;
            }

            if (interactAction != null)
                interactAction.performed += OnInteractPerformed;
            
            if (cancelAction != null)
                cancelAction.performed += OnCancelPerformed;
        }

        void Update()
        {
            UpdateInputValues();
        }

        void UpdateInputValues()
        {
            MoveInput = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            JumpHeld = jumpAction?.IsPressed() ?? false;
        }

        void OnJumpPerformed(InputAction.CallbackContext context)
        {
            JumpPressed = true;
        }

        void OnJumpCanceled(InputAction.CallbackContext context)
        {
            JumpPressed = false;
        }

        void OnInteractPerformed(InputAction.CallbackContext context)
        {
            InteractPressed = true;
        }
        
        void OnCancelPerformed(InputAction.CallbackContext context)
        {
            GameManager.Instance.TogglePause();

            Debug.Log("Cancel Pressed");
        }

        public void EnablePlayerInput()
        {
            playerActionMap?.Enable();
            uiActionMap?.Enable();
        }

        public void DisablePlayerInput()
        {
            playerActionMap?.Disable();
        }

        public void EnableUIInput()
        {
            uiActionMap?.Enable();
        }

        public void DisableUIInput()
        {
            uiActionMap?.Disable();
        }

        void LateUpdate()
        {
            // 프레임 끝에서 일회성 입력 초기화
            JumpPressed = false;
            InteractPressed = false;
        }

        void OnEnable()
        {
            inputActions?.Enable();
        }

        void OnDisable()
        {
            inputActions?.Disable();
        }
    }
}