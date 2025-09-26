using UnityEngine;
using WhaleShark.Config;
using WhaleShark.Core;

namespace WhaleShark.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerController2D : MonoBehaviour
    {
        public MoveConfig config;

        [Header("Ground Check")]
        public Transform groundCheck; // 선택: 별도의 Ground 기준 Transform
        public Vector2 groundBoxSize = new Vector2(0.5f, 0.1f); // 0 또는 음수면 자동 계산
        [Range(0f, 0.2f)] public float groundCheckSkin = 0.02f; // 폭 축소 및 발 아래 여유

        [Header("Debug")]
        public bool showGroundCheck = true;

        Rigidbody2D rb;
        Collider2D col;
        float moveX;
        float coyoteTimer;
        float jumpBufferTimer;
        int jumpCount;
        bool isGrounded;
        bool wasGrounded;
        InputManager input;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
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
            if (input != null)
            {
                moveX = input.MoveInput.x;
                if (input.JumpPressed)
                {
                    jumpBufferTimer = config.jumpBufferTime;
                }
            }
        }

        void CheckGrounded()
        {
            wasGrounded = isGrounded;

            // BoxCast 원점과 크기 계산
            var bounds = col.bounds;

            Vector2 size = groundBoxSize;
            if (size.x <= 0f || size.y <= 0f)
            {
                // 플레이어 콜라이더 기준 자동 사이즈
                float width = Mathf.Max(0.1f, bounds.size.x - 2f * groundCheckSkin);
                float height = Mathf.Max(0.02f, Mathf.Min(0.2f, config.groundCheckDist));
                size = new Vector2(width, height);
            }
            else
            {
                // 스킨 두께만큼 가로 축소
                size.x = Mathf.Max(0.05f, size.x - 2f * groundCheckSkin);
            }

            float angle;
            Vector2 origin;
            Vector2 dir;
            if (groundCheck != null)
            {
                origin = groundCheck.position;
                angle = groundCheck.eulerAngles.z;
                dir = -(Vector2)groundCheck.up; // groundCheck 아래 방향으로 캐스트
            }
            else
            {
                // 발 위치 근처에서 시작 (박스의 아래쪽이 콜라이더 바닥에 가깝도록 살짝 올려줌)
                origin = new Vector2(bounds.center.x, bounds.min.y + size.y * 0.5f + groundCheckSkin);
                angle = 0f;
                dir = Vector2.down;
            }

            var hit = Physics2D.BoxCast(origin, size, angle, dir, config.groundCheckDist, config.groundMask);
            isGrounded = hit.collider != null;

            // 착지 시 점프 카운트 리셋
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
            if (jumpBufferTimer > 0)
                jumpBufferTimer -= Time.deltaTime;
        }

        void HandleMovement()
        {
            float targetSpeed = moveX * config.moveSpeed;

            // 공중에서는 제어력 감소
            if (!isGrounded)
                targetSpeed *= config.airControl;

            rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);

            // 스프라이트 플립
            if (Mathf.Abs(moveX) > 0.01f)
                transform.localScale = new Vector3(Mathf.Sign(moveX), 1, 1);
        }

        void HandleJump()
        {
            bool canJump = (coyoteTimer > 0 || jumpCount < config.maxJumps) && jumpBufferTimer > 0;

            if (canJump)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(Vector2.up * config.jumpPower, ForceMode2D.Impulse);

                jumpCount++;
                jumpBufferTimer = 0;
                coyoteTimer = 0;
            }
        }

        void HandleVariableJumpHeight()
        {
            if (rb.linearVelocity.y < 0)
            {
                // 떨어질 때 중력 증가
                rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (config.fallMultiplier - 1) * Time.fixedDeltaTime);
            }
            else if (rb.linearVelocity.y > 0 && !(input?.JumpHeld ?? Input.GetButton("Jump")))
            {
                // 짧은 점프
                rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (config.lowJumpMultiplier - 1) * Time.fixedDeltaTime);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!showGroundCheck) return;

            // Gizmo에서 안전하게 콜라이더/사이즈 계산
            var gizmoCol = Application.isPlaying ? col : GetComponent<Collider2D>();
            if (gizmoCol == null || config == null) return;

            var bounds = gizmoCol.bounds;
            Vector2 size = groundBoxSize;
            if (size.x <= 0f || size.y <= 0f)
            {
                float width = Mathf.Max(0.1f, bounds.size.x - 2f * groundCheckSkin);
                float height = Mathf.Max(0.02f, Mathf.Min(0.2f, config.groundCheckDist));
                size = new Vector2(width, height);
            }
            else
            {
                size.x = Mathf.Max(0.05f, size.x - 2f * groundCheckSkin);
            }

            float angle;
            Vector3 origin;
            Vector3 dir;
            if (groundCheck != null)
            {
                angle = groundCheck.eulerAngles.z;
                origin = groundCheck.position;
                dir = -groundCheck.up;
            }
            else
            {
                angle = 0f;
                origin = new Vector3(bounds.center.x, bounds.min.y + size.y * 0.5f + groundCheckSkin, transform.position.z);
                dir = Vector3.down;
            }

            // 시작 박스와 끝 박스 그리기
            var end = origin + dir.normalized * config.groundCheckDist;
            Gizmos.color = isGrounded ? Color.green : Color.red;
            // 시작
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(origin, Quaternion.Euler(0, 0, angle), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0.01f));
            // 끝
            Gizmos.matrix = Matrix4x4.TRS(end, Quaternion.Euler(0, 0, angle), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0.01f));
            Gizmos.matrix = old;

            // 캐스트 경로 표시
            Gizmos.DrawLine(origin, end);
        }
    }
}