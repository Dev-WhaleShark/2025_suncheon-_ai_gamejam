using UnityEngine;

public class Crab : Enemy
{
    public Vector2 moveMinRange;
    public Vector2 moveMaxRange;

    private Vector2 velocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();

        rb.linearVelocity = new Vector2(10.0f, Random.Range(1.0f, 2.0f)).normalized * moveSpeed;
    }

    protected void FixedUpdate()
    { 
        Vector2 pos = transform.position;
        Vector2 vel = rb.linearVelocity;

        // X축 체크
        if (pos.x < moveMinRange.x || pos.x > moveMaxRange.x)
        {
            vel.x = -vel.x;
            velocity.x = -velocity.x; // 저장된 속도도 반전시켜 돌진 후에도 방향유지

            // 범위 밖으로 너무 나가지 않게 클램프
            //pos.x = Mathf.Clamp(pos.x, moveMinRange.x, moveMaxRange.x);
        }

        // Y축 체크
        if (pos.y < moveMinRange.y || pos.y > moveMaxRange.y)
        {
            vel.y = -vel.y;
            velocity.y = -velocity.y; // 저장된 속도도 반전시켜 돌진 후에도 방향유지
            //pos.y = Mathf.Clamp(pos.y, moveMinRange.y, moveMaxRange.y);
        }

        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        rb.linearVelocity = vel;
    }

    protected override void Idle()
    {
        currentState = EnemyState.Move;
    }

    protected override void Move()
    {
        if (canAttack)
        {
            currentState = EnemyState.Attack;
        }
    }
    protected override void Attack()
    {
        if (!canAttack) return;
        canAttack = false;

        velocity = rb.linearVelocity;
        rb.linearVelocity = Vector2.zero;

        animator.SetTrigger("OnAttack");
    }

    void SetDoubleSpeed()
    {
        rb.linearVelocity = velocity * 2;

        audioSource.clip = attackSound;
        audioSource.Play();
    }
    void SetHalfSpeed()
    {
        rb.linearVelocity = velocity;
        currentState = EnemyState.Move;
        StartCoroutine(AttackCooldown());
    }
}
