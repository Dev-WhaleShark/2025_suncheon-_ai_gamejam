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

    public float stunDuration = 0.2f;
    public float stunImmune = 1.0f;

    [Header("State")]
    public EnemyState currentState = EnemyState.Idle;
    protected Rigidbody2D rb;
    protected Transform target;
    protected float currentHealth;
    protected bool canAttack = true;

    protected float stunTimer;
    protected float stunImmuneTimer;

    protected float xScale;
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;

    public EnemyHPDisplayUI hpBar;

    [Header("Sound")]
    public AudioClip deathSound;
    public AudioClip attackSound;
    protected AudioSource audioSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        currentHealth = maxHealth;
        xScale = transform.localScale.x;
        stunTimer = 0.0f;
    }

    protected virtual void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;

        hpBar.Initialize(maxHealth);
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (currentState == EnemyState.Dead) return;

        stunImmuneTimer -= Time.deltaTime;
        if (stunTimer <= 0.0f)
        {
            StateLogic();
        }
        else
        {
            stunTimer -= Time.deltaTime;
        }
    }

    public void OnTakeDamage(int damage, float bulletVelocity)
    {
        if (currentState == EnemyState.Dead) return;

        currentHealth -= damage;
        hpBar.SetHP(currentHealth);

        if (currentHealth <= 0)
        {
            OnDied();
            return;
        }

        if (stunImmuneTimer <= 0.0f && currentState != EnemyState.Attack)
        { 
            animator.SetTrigger("OnHit");
            stunTimer = stunDuration;
            stunImmuneTimer = stunImmune;
            StartCoroutine(RecoverVelocity(stunTimer));
        }

        // // 바라보는 방향 설정
        // if (bulletVelocity > 0.0f) transform.localScale = new Vector3(xScale, transform.localScale.y, 1.0f);
        // else if (bulletVelocity < 0.0f) transform.localScale = new Vector3(-xScale, transform.localScale.y, 1.0f);
    }

    public void OnDied()
    {
        Debug.Log(gameObject.name + " is dead!");
        animator.SetTrigger("OnDeath");
        currentState = EnemyState.Dead;
        Destroy(gameObject, 1.0f);
        rb.linearVelocity = Vector2.zero;

        audioSource.clip = deathSound;
        audioSource.Play();
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

    protected virtual IEnumerator RecoverVelocity(float stopDuration)
    {
        rb.simulated = false;
        yield return new WaitForSeconds(stopDuration);
        rb.simulated = true;

        // // 바라보는 방향 설정
        // if (rb.linearVelocityX > 0.0f) transform.localScale = new Vector3(-xScale, transform.localScale.y, 1.0f);
        // else if (rb.linearVelocityX < 0.0f) transform.localScale = new Vector3(xScale, transform.localScale.y, 1.0f);
    }
}
