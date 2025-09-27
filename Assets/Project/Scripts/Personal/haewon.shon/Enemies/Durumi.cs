using UnityEngine;
using System.Collections;
using Mono.Cecil;

public class Durumi : Enemy
{
    public float moveRouteRadius = 10.0f;
    public int numberOfPointsOnRoute = 32;

    private Vector2[] points;
    private int targetPointIndex = 0;
    private float pointReachedThreshold = 0.5f;

    [Header("Combat")]
    public GameObject projectile;
    public int featherCount = 10;
    public float featherThrowingAngle = 180.0f;
    bool isWatchingLeft;
    public float delayBeforeAttack = 0.5f;
    public float delayAfterAttack = 0.75f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();

        points = new Vector2[numberOfPointsOnRoute];
        for (int i = 0; i < numberOfPointsOnRoute; i++)
        {
            float angle = i * Mathf.PI * 2 / numberOfPointsOnRoute;
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * moveRouteRadius + new Vector2(transform.position.x, transform.position.y);
        }

        targetPointIndex = Random.Range(0, numberOfPointsOnRoute);
        transform.position = new Vector3(points[targetPointIndex].x, points[targetPointIndex].y, -1);
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
            targetPointIndex = (targetPointIndex + 1) % numberOfPointsOnRoute;
            targetPoint = points[targetPointIndex];
            rb.linearVelocity = (targetPoint - (Vector2)transform.position).normalized * moveSpeed;
            isWatchingLeft = rb.linearVelocityX < 0;

            // 바라보는 방향 설정
            if (rb.linearVelocityX > 0.0f) spriteRenderer.flipX = true;
            else if (rb.linearVelocityX < 0.0f) spriteRenderer.flipX = false;
        }

        if (canAttack)
        { 
            currentState = EnemyState.Attack;
        }
    }
    protected override void Attack()
    {
        if (!canAttack) return;


        StartCoroutine(AttackRoutine());

        // 공격 구현 (예: 데미지 전달)
        StartCoroutine(AttackCooldown());
    }
    protected virtual IEnumerator AttackRoutine()
    {
        rb.linearVelocity = Vector2.zero;
        animator.SetTrigger("OnAttack");
        yield return new WaitForSeconds(delayBeforeAttack);
        
        audioSource.clip = attackSound;
        audioSource.Play();

        // 전방위 깃털 흩뿌리기
        int count = 12;
        float angle = Mathf.PI / 2.0f;
        for (int i = 0; i < count; ++i)
        {
            Vector2 v = new Vector2(Mathf.Cos(angle - Mathf.PI / 6.0f * i), Mathf.Sin(angle - Mathf.PI / 6.0f * i));
            GameObject spawnedProjectile = Instantiate(projectile, transform.position, Quaternion.identity);
            // feather rotation
            spawnedProjectile.GetComponent<EnemyProjectile>().SetDirection(v);
            yield return new WaitForSeconds(0.1f); // 약간의 딜레이 추가
        }

        yield return new WaitForSeconds(delayAfterAttack); // 약간의 딜레이 추가
        currentState = EnemyState.Move;
        animator.SetTrigger("OnAttackEnd");
        rb.linearVelocity = (points[targetPointIndex] - (Vector2)transform.position).normalized * moveSpeed;
    }
}
