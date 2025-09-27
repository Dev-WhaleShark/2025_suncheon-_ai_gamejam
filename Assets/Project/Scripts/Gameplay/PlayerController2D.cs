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

        [Header("TopDown Settings")]
        [Tooltip("대각선 입력 시 하나의 축만 선택(절대값 큰 축)하여 순수 4방향 이동")]
        public bool cardinalOnly = true;

        [Tooltip("입력 민감도 임계 (이 값 이하 입력은 0으로 간주)")]
        public float inputDeadZone = 0.1f;

        [Tooltip("(선택) 이동 후 타일 셀 중앙에 스냅")]
        public bool snapToGrid;

        [Tooltip("그리드 스냅 적용 허용 오차(셀 중심까지 거리)")]
        public float snapThreshold = 0.15f;

        [Header("Debug")]
        public bool debugDrawVelocity;

        private Rigidbody2D _rb;
        private Vector2 _moveDir;
        private InputManager _input;

        public Vector2 CurrentMoveDirection => _moveDir; // IProvidesMovementDirection 구현

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f; // 탑뷰: 중력 제거
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void Start()
        {
            _input = InputManager.Instance;
        }

        private void Update()
        {
            HandleInput();
            HandleFlip();
        }

        private void FixedUpdate()
        {
            HandleMovement();
        }

        private void HandleInput()
        {
            if (config == null)
            {
                return;
            }

            Vector2 raw = _input != null ? _input.MoveInput : Vector2.zero;

            if (raw.magnitude < inputDeadZone)
            {
                _moveDir = Vector2.zero;
                return;
            }

            if (cardinalOnly)
            {
                if (Mathf.Abs(raw.x) > Mathf.Abs(raw.y))
                {
                    _moveDir = new Vector2(Mathf.Sign(raw.x), 0f);
                }
                else
                {
                    _moveDir = new Vector2(0f, Mathf.Sign(raw.y));
                }
            }
            else
            {
                _moveDir = raw.normalized;
            }
        }

        private void HandleMovement()
        {
            if (config == null)
            {
                return;
            }

            _rb.linearVelocity = _moveDir * config.moveSpeed;
        }

        private void HandleFlip()
        {
            if (_moveDir.x != 0f)
            {
                transform.localScale = new Vector3(Mathf.Sign(_moveDir.x), 1f, 1f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugDrawVelocity || _rb == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(_rb.linearVelocity * 0.25f));
        }
    }
}
