using UnityEngine;
using System.Collections;

public class Mudskipper : Enemy
{
    public GameObject projectile;
    public Transform projectileReleasePoint;
    private Vector2[] points;
    private int targetPointIndex = 0;
    private float pointReachedThreshold = 0.5f;

    private int numberOfPoints = 4;
    public float delayBeforeAttack = 1.0f;
    private Vector2 velocity;
    // temp
    private float offset = 10.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        base.Start();

        // 임시 왕복 지점 지정
        points = new Vector2[numberOfPoints];
        points[0] = new Vector2(transform.position.x - offset, transform.position.y - offset);
        points[1] = new Vector2(transform.position.x + offset, transform.position.y + offset);
        points[2] = new Vector2(transform.position.x - offset, transform.position.y + offset);
        points[3] = new Vector2(transform.position.x + offset, transform.position.y - offset);

        attackCooldown = attackCooldown + delayBeforeAttack; // 공격에 걸리는 시간 합산
    }


    protected override void Idle()
    {
        currentState = EnemyState.Move;
        SetNextTargetPoint();
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
        if (!canAttack)
        {
            return;
        }

        // play attack anim -> anim event 1. projectile set -> anim event 2. attack end

        animator.SetTrigger("OnAttack");
        velocity = rb.linearVelocity;
        rb.linearVelocity = Vector2.zero;

        float xDir = target.position.x - gameObject.transform.position.x;
        if (xDir < 0)
        {
            transform.localScale = new Vector3(-xScale, transform.localScale.y, 1.0f);
        }
        else
        {
            transform.localScale = new Vector3(xScale, transform.localScale.y, 1.0f);
        }
        
        StartCoroutine(AttackCooldown());
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

    void SetProjectile()
    {
        GameObject spawnedProjectile = Instantiate(projectile, projectileReleasePoint.position, Quaternion.identity);
        Mud projComponent = spawnedProjectile.GetComponent<Mud>();
        projComponent.SetDestination(target.position);
    }

    void OnAttackEnd()
    {
        currentState = EnemyState.Move;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, 1.0f);
        SetNextTargetPoint();
    }
}
