using UnityEngine;

public class Roamer : Enemy
{
    private Vector2 targetPoint;
    private float pointReachedThreshold = 0.5f;

    public Vector2 moveMinRange;
    public Vector2 moveMaxRange;

    public float idleTimeMin;
    public float idleTimeMax;
    private float idleTimer = 0.0f;

    [Header("Pollution Config")]
    [SerializeField] private float interval = 0.15f;
    [SerializeField] private float minSpeedThreshold = 0.05f;
    [SerializeField] private bool avoidDuplicateCell = true;
    [SerializeField] private int pollutionRadius = 1;

    private float _pollutionTimer = 0f;
    private Vector2Int _lastCell = new Vector2Int(int.MinValue, int.MinValue);

    private Stage _map;

    protected override void Awake()
    {
        base.Awake();
        _map = _map ?? FindFirstObjectByType<Stage>();
        _pollutionTimer = interval;
    }

    protected override void Idle()
    {
        if (idleTimer > 0.0f)
        {
            idleTimer -= Time.deltaTime;
        }
        else
        {
            currentState = EnemyState.Move;
            SetNextTargetPoint();
        }
    }

    protected override void Move()
    {
        if (Vector2.Distance(transform.position, targetPoint) < pointReachedThreshold)
        {
            currentState = EnemyState.Idle;
            idleTimer = Random.Range(idleTimeMin, idleTimeMax);
            rb.linearVelocity = Vector2.zero;
        }

        // 오염 타이머 처리
        _pollutionTimer -= Time.deltaTime;
        if (_pollutionTimer <= 0f)
        {
            if (ApplyPollutionTrail())
            {
                _pollutionTimer = interval;
            }
            else
            {
                _pollutionTimer = 0.01f;
            }
        }
    }

    void SetNextTargetPoint()
    {
        Vector2 midPoint = new Vector2(moveMinRange.x + moveMaxRange.x / 2, moveMinRange.y + moveMaxRange.y / 2);
        targetPoint = new Vector2(Random.Range(moveMinRange.x, moveMaxRange.x),
            Random.Range(moveMinRange.y, moveMaxRange.y));

        while (Vector2.Dot((Vector2)transform.position - midPoint, targetPoint - midPoint) >
               0.0f) // 중심 기준 반대편 절반을 목적지로 삼도록 함
        {
            targetPoint = new Vector2(Random.Range(moveMinRange.x, moveMaxRange.x),
                Random.Range(moveMinRange.y, moveMaxRange.y));
        }

        rb.linearVelocity = (targetPoint - (Vector2)transform.position).normalized * moveSpeed;

        // 바라보는 방향 설정
        if (rb.linearVelocityX > 0.0f) transform.localScale = new Vector3(-xScale, transform.localScale.y, 1.0f);
        else if (rb.linearVelocityX < 0.0f) transform.localScale = new Vector3(xScale, transform.localScale.y, 1.0f);
    }

    #region Pollution
    private bool ApplyPollutionTrail()
    {
        if (_map == null) return false;

        Vector2 moveDir = rb.linearVelocity;
        float speed = moveDir.magnitude;
        if (speed < minSpeedThreshold) return false;

        moveDir.Normalize();

        Vector3 pos = transform.position;
        pos += (Vector3)moveDir * -0.4f;

        if (!_map.WorldToGrid(pos, out var centerCell))
            return false;

        if (avoidDuplicateCell && centerCell == _lastCell)
            return false;

        // 지정된 크기만큼 오염 적용 (중심 기준 대칭)
        bool anyPollutionApplied = false;
        int halfRadius = pollutionRadius / 2;
        for (int x = -halfRadius; x < pollutionRadius - halfRadius; x++)
        {
            for (int y = -halfRadius; y < pollutionRadius - halfRadius; y++)
            {
                Vector2Int targetCell = centerCell + new Vector2Int(x, y);
                if (_map.IsValidGridPosition(targetCell))
                {
                    _map.SetPollution(targetCell, true);
                    anyPollutionApplied = true;
                }
            }
        }

        _lastCell = centerCell;
        return anyPollutionApplied;
    }

    #endregion

}
