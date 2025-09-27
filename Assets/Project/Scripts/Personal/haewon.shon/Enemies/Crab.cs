using UnityEngine;
using System.Collections;
using Unity.InferenceEngine;

public class Crab : Enemy
{
    private Vector2[] points;
    private int targetPointIndex = 0;
    private float pointReachedThreshold = 0.5f;

    private int numberOfPoints = 2;

    public float delayBeforeAttack = 2.0f;
    public float delayAfterAttack = 1.0f;
    public float arcAngle = 120.0f; // 공격 범위 각도
    public LayerMask playerLayer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        base.Start();

        // 임시 왕복 지점 지정
        points = new Vector2[numberOfPoints];
        points[0] = new Vector2(transform.position.x - 5.0f, transform.position.y);
        points[1] = new Vector2(transform.position.x + 5.0f, transform.position.y);

        rb.linearVelocity = (points[targetPointIndex] - (Vector2)transform.position).normalized * moveSpeed;

        attackCooldown = attackCooldown + delayBeforeAttack + delayAfterAttack; // 공격에 걸리는 시간 합산
    }

    protected override void Idle()
    {
        currentState = EnemyState.Move;
    }

    protected override void Move()
    {
        Vector2 targetPoint = points[targetPointIndex];
        if (Vector2.Distance(transform.position, targetPoint) < pointReachedThreshold)
        {
            targetPointIndex = (targetPointIndex + 1) % numberOfPoints;
            targetPoint = points[targetPointIndex];
            rb.linearVelocity = (targetPoint - (Vector2)transform.position).normalized * moveSpeed;
        }

        if (canAttack)
        {
            currentState = EnemyState.Attack;
        }
    }
    protected override void Attack()
    {
        if (!canAttack) return;

        //attack logic
        StartCoroutine(AttackRoutine());

        // 공격 구현 (예: 데미지 전달)
        StartCoroutine(AttackCooldown());
    }

    protected virtual IEnumerator AttackRoutine()
    {
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(delayBeforeAttack);

        CheckHit();

        yield return new WaitForSeconds(delayAfterAttack);
        currentState = EnemyState.Move;
        rb.linearVelocity = (points[targetPointIndex] - (Vector2)transform.position).normalized * moveSpeed;
        
        // 바라보는 방향 설정
        if (rb.linearVelocityX > 0.0f) transform.localScale = new Vector3(-xScale, transform.localScale.y, 1.0f);
        else if (rb.linearVelocityX < 0.0f) transform.localScale = new Vector3(xScale, transform.localScale.y, 1.0f);
    }

    void CheckHit()
    { 
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, playerLayer);
        foreach (var hit in hits)
        {
            Vector2 dirToTarget = (hit.transform.position - transform.position).normalized;
            // 왼쪽진행시 위 타격, 오른쪽진행시 아래 타격
            float angle = (rb.linearVelocityX < 0.0f) ? Vector2.Angle(transform.up, dirToTarget) : Vector2.Angle(-transform.up, dirToTarget);
            if (angle <= arcAngle / 2)
            {
                // 공격 적용
                hit.GetComponent<Character>().OnTakeDamage(attackDamage);
            }
        }
    }

}
