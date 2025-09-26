using UnityEngine;
using System.Collections;

public enum EnemyState
{
    Idle,
    Move,
    Attack,
    Skill,
    Dead
}


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 2f;
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
    public int attackDamage = 10;

    [Header("State")]
    public EnemyState currentState = EnemyState.Idle;
    protected Rigidbody2D rb;
    protected Transform target;
    protected float currentHealth;
    protected bool canAttack = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    protected virtual void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (currentState == EnemyState.Dead) return;

        StateLogic();
    }

    public void OnTakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            OnDied();
        }
    }

    public void OnDied()
    {
        Debug.Log(gameObject.name + " is dead!");
        Destroy(gameObject);
    }

    protected virtual void StateLogic()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                Idle();
                break;
            case EnemyState.Move:
                Move();
                break;
            case EnemyState.Attack:
                Attack();
                break;
            case EnemyState.Skill:
                Skill();
                break;
        }
    }
    protected virtual void Idle()
    {
        // 기본: 타겟이 가까우면 Move로 전환
        if (target != null && Vector2.Distance(transform.position, target.position) < 5f)
            currentState = EnemyState.Move;
    }

    protected virtual void Move()
    {
        if (target == null) return;

        Vector2 direction = (target.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        if (Vector2.Distance(transform.position, target.position) <= attackRange)
        {
            rb.linearVelocity = Vector2.zero;
            currentState = EnemyState.Attack;
        }
    }

    protected virtual void Attack()
    {
        if (!canAttack) return;

        // 공격 구현 (예: 데미지 전달)
        StartCoroutine(AttackCooldown());
    }

    protected virtual IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
        currentState = EnemyState.Move;
    }

    protected virtual void Skill()
    {
        // 특수 스킬 구현용 오버라이드
    }

    public virtual void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0f) Die();
    }

    protected virtual void Die()
    {
        currentState = EnemyState.Dead;
        rb.linearVelocity = Vector2.zero;
        // 사망 처리: 애니메이션, 오브젝트 제거 등
        Destroy(gameObject, 1f);
    }
}
