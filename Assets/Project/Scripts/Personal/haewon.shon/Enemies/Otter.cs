using UnityEngine;

public class Otter : Enemy
{
    public GameObject projectile;
    private Vector2[] points;
    private int targetPointIndex = 0;
    private float pointReachedThreshold = 0.5f;

    private int numberOfPoints = 2;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        base.Start();

        // 임시 왕복 지점 지정
        points = new Vector2[numberOfPoints];
        points[0] = new Vector2(transform.position.x - 5.0f, transform.position.y - 5.0f);
        points[1] = new Vector2(transform.position.x + 5.0f, transform.position.y + 5.0f);

        rb.linearVelocity = (points[targetPointIndex] - (Vector2)transform.position).normalized * moveSpeed;
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
            targetPointIndex = (targetPointIndex + 1) % numberOfPoints;
            targetPoint = points[targetPointIndex];
            rb.linearVelocity = (targetPoint - (Vector2)transform.position).normalized * moveSpeed;
                
            // 바라보는 방향 설정
            if (rb.linearVelocityX > 0.0f) transform.localScale = new Vector3(-xScale, transform.localScale.y, 1.0f);
            else if (rb.linearVelocityX < 0.0f) transform.localScale = new Vector3(xScale, transform.localScale.y, 1.0f);
        }

        if (canAttack)
        { 
            currentState = EnemyState.Attack;
        }
    }
    protected override void Attack()
    {
        if (!canAttack) return;

        GameObject spawnedProjectile = Instantiate(projectile, transform.position, Quaternion.identity);
        EnemyProjectile projComponent = spawnedProjectile.GetComponent<EnemyProjectile>();
        projComponent.SetDirection((target.position - transform.position).normalized);
        currentState = EnemyState.Move;

        // 공격 구현 (예: 데미지 전달)
        StartCoroutine(AttackCooldown());
    }
}
