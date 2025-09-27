using UnityEngine;
using System.Collections;
using Unity.InferenceEngine;

public class Crab : Enemy
{
    public Vector2 moveMinRange;
    public Vector2 moveMaxRange;

    private Vector2 velocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
    }

    protected override void Idle()
    {
        currentState = EnemyState.Move;
    }

    protected override void Move()
    {
        Vector2 pos = transform.position;
        Vector2 vel = rb.linearVelocity;

        // X축 체크
        if (pos.x < moveMinRange.x || pos.x > moveMaxRange.x)
        {
            vel.x = -vel.x;
            // 범위 밖으로 너무 나가지 않게 클램프
            pos.x = Mathf.Clamp(pos.x, moveMinRange.x, moveMaxRange.x);
        }

        // Y축 체크
        if (pos.y < moveMinRange.y || pos.y > moveMaxRange.y)
        {
            vel.y = -vel.y;
            pos.y = Mathf.Clamp(pos.y, moveMinRange.y, moveMaxRange.y);
        }

        transform.position = pos;
        rb.linearVelocity = vel;

        if (canAttack)
        {
            currentState = EnemyState.Attack;
        }
    }
    protected override void Attack()
    {
        if (!canAttack) return;

        velocity = rb.linearVelocity;
        rb.linearVelocity = Vector2.zero;

        animator.SetTrigger("OnAttack");
        StartCoroutine(AttackCooldown());
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
    }
}
