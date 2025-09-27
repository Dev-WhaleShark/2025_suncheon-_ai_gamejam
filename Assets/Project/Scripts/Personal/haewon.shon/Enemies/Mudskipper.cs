using UnityEngine;
using System.Collections;

public class Mudskipper : Enemy
{
    public GameObject projectile;
    private Vector2[] points;
    private int targetPointIndex = 0;
    private float pointReachedThreshold = 0.5f;

    private int numberOfPoints = 4;
    public float delayBeforeAttack = 1.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        base.Start();

        // 임시 왕복 지점 지정
        points = new Vector2[numberOfPoints];
        points[0] = new Vector2(transform.position.x - 5.0f, transform.position.y - 5.0f);
        points[1] = new Vector2(transform.position.x + 5.0f, transform.position.y + 5.0f);
        points[2] = new Vector2(transform.position.x - 5.0f, transform.position.y + 5.0f);
        points[3] = new Vector2(transform.position.x + 5.0f, transform.position.y - 5.0f);

        targetPointIndex = Random.Range(0, numberOfPoints);
        rb.linearVelocity = (points[targetPointIndex] - (Vector2)transform.position).normalized * moveSpeed;

        attackCooldown = attackCooldown + delayBeforeAttack; // 공격에 걸리는 시간 합산
    }

    // Update is called once per frame
    protected override void Update()
    {
        if (currentState == EnemyState.Dead) return;

        StateLogic();
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

        GameObject spawnedProjectile = Instantiate(projectile, transform.position, Quaternion.identity);
        EnemyProjectile projComponent = spawnedProjectile.GetComponent<EnemyProjectile>();
        projComponent.SetDirection((target.position - transform.position).normalized);

        yield return new WaitForSeconds(0.1f); // 약간의 딜레이 추가

        currentState = EnemyState.Move;
        SetNextTargetPoint();
    }

    void SetNextTargetPoint()
    {
        int nextTargetIndex = Random.Range(0, numberOfPoints);
        while (nextTargetIndex == targetPointIndex)
        {
            nextTargetIndex = Random.Range(0, numberOfPoints);
        }
        targetPointIndex = nextTargetIndex;
        rb.linearVelocity = (points[targetPointIndex] - (Vector2)transform.position).normalized * moveSpeed;
        
        // 바라보는 방향 설정
        if (rb.linearVelocityX > 0.0f) transform.localScale = new Vector3(-xScale, transform.localScale.y, 1.0f);
        else if (rb.linearVelocityX < 0.0f) transform.localScale = new Vector3(xScale, transform.localScale.y, 1.0f);
    }

}
