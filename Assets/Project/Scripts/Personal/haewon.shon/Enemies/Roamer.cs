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
    }
    
    void SetNextTargetPoint()
    {
        Vector2 midPoint = new Vector2(moveMinRange.x + moveMaxRange.x / 2, moveMinRange.y + moveMaxRange.y / 2);
        targetPoint = new Vector2(Random.Range(moveMinRange.x, moveMaxRange.x), Random.Range(moveMinRange.y, moveMaxRange.y));

        while (Vector2.Dot((Vector2)transform.position - midPoint, targetPoint - midPoint) > 0.0f) // 중심 기준 반대편 절반을 목적지로 삼도록 함
        {
            targetPoint = new Vector2(Random.Range(moveMinRange.x, moveMaxRange.x), Random.Range(moveMinRange.y, moveMaxRange.y));
        }
        rb.linearVelocity = (targetPoint - (Vector2)transform.position).normalized * moveSpeed;

        // 바라보는 방향 설정
        if (rb.linearVelocityX > 0.0f) transform.localScale = new Vector3(-xScale, transform.localScale.y, 1.0f);
        else if (rb.linearVelocityX < 0.0f) transform.localScale = new Vector3(xScale, transform.localScale.y, 1.0f);
    }
}
