using UnityEngine;
using WhaleShark.Config;
using WhaleShark.Core;
using UnityEngine.Tilemaps;

namespace WhaleShark.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerController2D : MonoBehaviour
    {
        public MoveConfig config;

        [Header("TopDown Settings")] [Tooltip("대각선 입력 시 하나의 축만 선택(절대값 큰 축)하여 순수 4방향 이동")]
        public bool cardinalOnly = true;

        [Tooltip("입력 민감도 임계 (이 값 이하 입력은 0으로 간주)")]
        public float inputDeadZone = 0.1f;

        [Tooltip("(선택) 이동 후 타일 셀 중앙에 스냅")] public bool snapToGrid;

        [Tooltip("그리드 스냅 적용 허용 오차(셀 중심까지 거리)")]
        public float snapThreshold = 0.15f;

        [Tooltip("스냅 대상 타일맵 (없으면 스냅 불가)")] public Tilemap referenceTilemap;

        [Header("Debug")] public bool debugDrawVelocity;

        [Header("Test Pollution")] 
        [Tooltip("테스트: 이동 중 바라보는/이동하는 방향 타일 오염 활성화")] public bool testPolluteWhileMoving = true;
        [Tooltip("오염 적용 주기(초)")] public float pollutionInterval = 0.15f;
        [Tooltip("앞으로 몇 타일 떨어진 곳을 오염시킬지 (0=현재, 1=한 칸 앞)")] public int forwardTiles; // 기본 0
        [Tooltip("타일 월드 크기(스케일). forwardTiles>0 일 때 오프셋 계산용")] public float tileWorldSize = 1f;
        [Tooltip("맵 매니저 수동 할당(없으면 자동 탐색)")] public MapManager mapManager;

        Rigidbody2D _rb;
        Vector2 _moveDir;
        InputManager _input;

        float _pollutionTimer;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            // Collider2D 참조는 현재 사용 안 하므로 제거 (필요 시 복원)
            _rb.gravityScale = 0f; // 탑뷰: 중력 제거
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        void Start() => _input = InputManager.Instance;

        void Update()
        {
            HandleInput();
            HandleFlip();
            if (snapToGrid)
                TrySnapToGrid();
        }

        void FixedUpdate()
        {
            HandleMovement();
            if (testPolluteWhileMoving)
                ApplyTestPollution();
        }

        void HandleInput()
        {
            if (config == null) return;
            Vector2 raw = _input != null ? _input.MoveInput : Vector2.zero;

            if (raw.magnitude < inputDeadZone)
            {
                _moveDir = Vector2.zero;
                return;
            }

            if (cardinalOnly)
            {
                if (Mathf.Abs(raw.x) > Mathf.Abs(raw.y))
                    _moveDir = new Vector2(Mathf.Sign(raw.x), 0f);
                else
                    _moveDir = new Vector2(0f, Mathf.Sign(raw.y));
            }
            else
            {
                _moveDir = raw.normalized;
            }
        }

        void HandleMovement()
        {
            if (config == null) return;
            _rb.linearVelocity = _moveDir * config.moveSpeed;
        }

        void HandleFlip()
        {
            if (_moveDir.x != 0f)
            {
                transform.localScale = new Vector3(Mathf.Sign(_moveDir.x), 1f, 1f);
            }
        }

        void TrySnapToGrid()
        {
            if (referenceTilemap == null) return;
            // 이동 중이 아닐 때만 스냅(멈췄거나 축단위 입력 해제 시)
            if (_moveDir != Vector2.zero) return;
            Vector3 worldPos = transform.position;
            Vector3Int cell = referenceTilemap.WorldToCell(worldPos);
            Vector3 cellCenter = referenceTilemap.GetCellCenterWorld(cell);
            float dist = Vector2.Distance(new Vector2(worldPos.x, worldPos.y), new Vector2(cellCenter.x, cellCenter.y));
            if (dist <= snapThreshold)
            {
                transform.position = new Vector3(cellCenter.x, cellCenter.y, transform.position.z);
            }
        }

        void ApplyTestPollution()
        {
            if (mapManager == null)
            {
                mapManager = FindFirstObjectByType<MapManager>();
                if (mapManager == null) return;
            }
            if (_moveDir == Vector2.zero) { _pollutionTimer = 0f; return; }
            _pollutionTimer -= Time.fixedDeltaTime;
            if (_pollutionTimer > 0f) return;
            _pollutionTimer = pollutionInterval;

            Vector3 basePos = transform.position;
            Vector3 forwardOffset = Vector3.zero;
            if (forwardTiles > 0)
                forwardOffset = new Vector3(_moveDir.x, _moveDir.y, 0f) * (tileWorldSize * forwardTiles * 0.98f);
            mapManager.PolluteAtWorld(basePos + forwardOffset);
        }

        void OnDrawGizmosSelected()
        {
            if (!debugDrawVelocity || _rb == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(_rb.linearVelocity * 0.25f));
        }
    }
}
