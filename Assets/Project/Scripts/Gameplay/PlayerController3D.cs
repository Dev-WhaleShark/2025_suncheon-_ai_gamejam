using UnityEngine;
using WhaleShark.Config;
using WhaleShark.Core;

namespace WhaleShark.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class PlayerController3D : MonoBehaviour
    {
        public MoveConfig config;

        [Header("Ground Check")]
        [Tooltip("선택: 별도의 Ground 기준 Transform (방향/위치 커스터마이즈)")] public Transform groundCheck;
        [Tooltip("XZ 평면에서의 박스 가로/세로(깊이) 크기. 0 또는 음수면 콜라이더 기반 자동 계산")] public Vector2 groundBoxSize = new Vector2(0.5f, 0.5f);
        [Range(0f, 0.2f)] public float groundCheckSkin = 0.02f;

        [Header("Options")] public bool rotateTowardsMovement = true;
        [Tooltip("입력 방향으로 도는 속도 (deg/sec)")] public float rotationSpeed = 540f;
        [Tooltip("회전 적용 위한 최소 입력 세기 제곱 (노이즈 방지)")] public float rotateInputThresholdSqr = 0.01f; // ~= 0.1 magnitude

        [Header("Debug")] public bool showGroundCheck = true;

        Rigidbody rb;
        Collider col;
        Vector2 moveInput; // XZ 평면 이동 입력 (x = 좌우, y = 전후)
        float coyoteTimer;
        float jumpBufferTimer;
        int jumpCount;
        bool isGrounded;
        bool wasGrounded;
        InputManager input;
        Vector3 lastMoveDir = Vector3.forward; // 마지막 안정된 이동 방향

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            // 3D 플랫폼 게임에서 보편적으로 회전 물리 안정화를 위해
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        void Start()
        {
            input = InputManager.Instance;
        }

        void Update()
        {
            HandleInput();
            CheckGrounded();
            HandleCoyoteTime();
            HandleJumpBuffer();
        }

        void FixedUpdate()
        {
            HandleMovement();
            HandleJump();
            HandleVariableJumpHeight();
        }

        void HandleInput()
        {
            if (input == null) return;

            // 가정: InputManager.MoveInput 은 Vector2 (x: 좌우, y: 전후)
            moveInput = input.MoveInput;
            if (input.JumpPressed)
            {
                jumpBufferTimer = config.jumpBufferTime;
            }
        }

        void CheckGrounded()
        {
            wasGrounded = isGrounded;
            var bounds = col.bounds;

            // 사이즈 계산 (BoxCast 는 halfExtents 를 사용하므로 별도 처리)
            float widthX;
            float depthZ;
            if (groundBoxSize.x <= 0f)
                widthX = Mathf.Max(0.1f, bounds.size.x - 2f * groundCheckSkin);
            else
                widthX = Mathf.Max(0.05f, groundBoxSize.x - 2f * groundCheckSkin);

            if (groundBoxSize.y <= 0f)
                depthZ = Mathf.Max(0.1f, bounds.size.z - 2f * groundCheckSkin);
            else
                depthZ = Mathf.Max(0.05f, groundBoxSize.y - 2f * groundCheckSkin);

            float heightY = Mathf.Max(0.02f, Mathf.Min(0.2f, config.groundCheckDist));

            Vector3 halfExtents = new Vector3(widthX * 0.5f, heightY * 0.5f, depthZ * 0.5f);

            Vector3 origin;
            Quaternion rot;
            Vector3 dir;
            if (groundCheck != null)
            {
                origin = groundCheck.position;
                rot = groundCheck.rotation;
                dir = -groundCheck.up; // 지정 Transform 의 아래 방향
            }
            else
            {
                origin = new Vector3(bounds.center.x, bounds.min.y + halfExtents.y + groundCheckSkin, bounds.center.z);
                rot = Quaternion.identity;
                dir = Vector3.down;
            }

            // BoxCast 수행
            RaycastHit hit;
            bool hitSomething = Physics.BoxCast(origin, halfExtents, dir, out hit, rot, config.groundCheckDist, config.groundMask, QueryTriggerInteraction.Ignore);
            isGrounded = hitSomething;

            if (isGrounded && !wasGrounded)
            {
                jumpCount = 0;
            }
        }

        void HandleCoyoteTime()
        {
            if (isGrounded)
                coyoteTimer = config.coyoteTime;
            else
                coyoteTimer -= Time.deltaTime;
        }

        void HandleJumpBuffer()
        {
            if (jumpBufferTimer > 0f)
                jumpBufferTimer -= Time.deltaTime;
        }

        void HandleMovement()
        {
            Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);
            float magSqr = inputDir.sqrMagnitude;
            if (magSqr > 1f) inputDir.Normalize();

            Vector3 targetVelHorizontal = inputDir * config.moveSpeed;
            if (!isGrounded)
                targetVelHorizontal *= config.airControl;

            Vector3 currentVel = rb.linearVelocity;
            Vector3 newVel = new Vector3(targetVelHorizontal.x, currentVel.y, targetVelHorizontal.z);
            rb.linearVelocity = newVel;

            if (rotateTowardsMovement)
            {
                // 충분한 입력이 있을 때만 방향 갱신 (미세 노이즈로 인한 회전 방지)
                if (magSqr >= rotateInputThresholdSqr)
                {
                    lastMoveDir = inputDir.normalized; // 안정된 이동 방향 저장
                }

                // 마지막 방향만으로 Yaw 회전 (수직 고정)
                if (lastMoveDir.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lastMoveDir, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
                    // 물리 토크로 인한 지속 회전 방지
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        void HandleJump()
        {
            bool canJump = (coyoteTimer > 0f || jumpCount < config.maxJumps) && jumpBufferTimer > 0f;
            if (!canJump) return;

            var v = rb.linearVelocity;
            v.y = 0f; // 점프 직전 상승/하강 속도 리셋
            rb.linearVelocity = v;
            rb.AddForce(Vector3.up * config.jumpPower, ForceMode.Impulse);

            jumpCount++;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        void HandleVariableJumpHeight()
        {
            var v = rb.linearVelocity;
            if (v.y < 0f)
            {
                // 낙하 가속
                v += Vector3.up * (Physics.gravity.y * (config.fallMultiplier - 1f) * Time.fixedDeltaTime);
            }
            else if (v.y > 0f && !(input?.JumpHeld ?? Input.GetButton("Jump")))
            {
                // 짧은 점프
                v += Vector3.up * (Physics.gravity.y * (config.lowJumpMultiplier - 1f) * Time.fixedDeltaTime);
            }
            rb.linearVelocity = v;
        }

        void OnDrawGizmosSelected()
        {
            if (!showGroundCheck || config == null) return;
            var gizmoCol = Application.isPlaying ? col : GetComponent<Collider>();
            if (gizmoCol == null) return;

            var bounds = gizmoCol.bounds;
            float widthX = (groundBoxSize.x <= 0f) ? Mathf.Max(0.1f, bounds.size.x - 2f * groundCheckSkin) : Mathf.Max(0.05f, groundBoxSize.x - 2f * groundCheckSkin);
            float depthZ = (groundBoxSize.y <= 0f) ? Mathf.Max(0.1f, bounds.size.z - 2f * groundCheckSkin) : Mathf.Max(0.05f, groundBoxSize.y - 2f * groundCheckSkin);
            float heightY = Mathf.Max(0.02f, Mathf.Min(0.2f, config.groundCheckDist));
            Vector3 halfExtents = new Vector3(widthX * 0.5f, heightY * 0.5f, depthZ * 0.5f);

            Vector3 origin;
            Quaternion rot;
            Vector3 dir;
            if (groundCheck != null)
            {
                origin = groundCheck.position;
                rot = groundCheck.rotation;
                dir = -groundCheck.up;
            }
            else
            {
                origin = new Vector3(bounds.center.x, bounds.min.y + halfExtents.y + groundCheckSkin, bounds.center.z);
                rot = Quaternion.identity;
                dir = Vector3.down;
            }

            Vector3 end = origin + dir.normalized * config.groundCheckDist;
            Gizmos.color = isGrounded ? Color.green : Color.red;

            // 시작 박스
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(origin, rot, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
            // 끝 박스
            Gizmos.matrix = Matrix4x4.TRS(end, rot, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
            Gizmos.matrix = old;

            Gizmos.DrawLine(origin, end);
        }
    }
}
