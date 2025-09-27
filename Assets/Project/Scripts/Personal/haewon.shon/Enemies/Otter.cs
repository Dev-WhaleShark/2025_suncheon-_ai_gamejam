using UnityEngine;

public class Otter : Enemy
{
    public GameObject projectile;
    public Transform projectileReleasePoint;
    private Vector2[] points;
    private int targetPointIndex = 0;
    private float pointReachedThreshold = 0.5f;

    private int numberOfPoints = 2;

    private Vector2 velocity;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();

        // 임시 왕복 지점 지정
        points = new Vector2[numberOfPoints];
        points[0] = new Vector2(transform.position.x - 5.0f, transform.position.y - 5.0f);
        points[1] = new Vector2(transform.position.x + 5.0f, transform.position.y + 5.0f);

        rb.linearVelocity = (points[targetPointIndex] - (Vector2)transform.position).normalized * moveSpeed;
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

        // play attack anim -> anim event 1. projectile set -> anim event 2. attack end

        animator.SetTrigger("OnAttack");
        velocity = rb.linearVelocity;
        rb.linearVelocity = Vector2.zero;
        
        StartCoroutine(AttackCooldown());
    }

    void SetProjectile()
    {
        GameObject spawnedProjectile = Instantiate(projectile, projectileReleasePoint.position, Quaternion.identity);
        EnemyProjectile projComponent = spawnedProjectile.GetComponent<EnemyProjectile>();
        projComponent.SetDirection((target.position - projectileReleasePoint.position).normalized);
        
        audioSource.clip = attackSound;
        audioSource.Play();
    }

    void OnAttackEnd()
    { 
        currentState = EnemyState.Move;
        rb.linearVelocity = velocity;
    }
}
