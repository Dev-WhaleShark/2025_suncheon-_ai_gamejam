using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Character : MonoBehaviour
{
    Rigidbody2D rb;
    private Vector2 moveInput;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 2f;
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
    public int attackDamage = 10;
    public float immuneTime = 1.0f;

    [Header("Combat")]
    public GameObject projectile;
    public GameObject corpse;
    private Vector2 mousePos;

    private float currentHealth;
    private float immuneTimer = 1.0f;

    private bool hasSlowDebuff = false;
    private float slowTimer = 0.0f;

    private bool isDead = false;

    // Anim
    private Animator animator;

    [Header("SFX")]
    private AudioSource audioSource;
    public AudioClip deathSound;
    public AudioClip attackSound;


    [Header("Cleaning")]
    private bool isCleaning = false;


    [Header("Cleaning Config")]
    public Transform cleaningPos;
    [SerializeField] private float minSpeedThreshold = 0.05f;
    [SerializeField] private bool avoidDuplicateCell = true;
    [SerializeField] private int cleaningRadius = 1;

    private Vector2Int _lastCell = new Vector2Int(int.MinValue, int.MinValue);

    private Stage _map;

    void Awake()
    {
        _map = _map ?? FindFirstObjectByType<Stage>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        currentHealth = maxHealth;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isDead) return;

        rb.linearVelocity = moveInput * moveSpeed;
        animator.SetFloat("xVelocity", rb.linearVelocityX);
        animator.SetBool("isMoving", rb.linearVelocity.magnitude > 0.0f);
        immuneTimer -= Time.fixedDeltaTime;

        if (hasSlowDebuff)
        {
            slowTimer -= Time.fixedDeltaTime;
            if (slowTimer <= 0.0f)
            {
                hasSlowDebuff = false;
                moveSpeed /= (1.0f - 0.5f); // Assuming the slow amount is always 50%
            }
        }

        if (isCleaning)
        {
            ApplyCleaning();
        }
    }

    public void OnTakeDamage(int damage)
    {
        if (isDead) return;
        if (immuneTimer > 0) return;

        currentHealth -= damage;
        immuneTimer = immuneTime;
        Debug.Log("health: " + currentHealth.ToString());
        animator.SetTrigger("OnHit");
        if (currentHealth <= 0)
        {
            OnDied();
        }
    }

    public void OnSlow(float percent, float duration)
    {
        if (hasSlowDebuff)
        {
            slowTimer = duration; // Refresh the slow duration
            return;
        }

        moveSpeed *= 1.0f - percent;
        hasSlowDebuff = true;
        slowTimer = duration;
    }

    void OnMove(InputValue value)
    {
        if (isDead) return;

        Vector2 inputVector = value.Get<Vector2>();
        moveInput = inputVector;
    }

    void OnAttack(InputValue value) // LMB
    {
        if (isDead || isCleaning) return;

        if (value.isPressed)
        {
            animator.SetTrigger("OnAttack");
            mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            if (mousePos.x < transform.position.x) // 공격 방향전환 위한 일시 update
            {
                animator.SetFloat("xVelocity", -1);
            }
            else
            {
                animator.SetFloat("xVelocity", 1);
            }
        }
    }

    void OnClean(InputValue value) // space
    {
        if (value.isPressed)
        {
            isCleaning = true;
        }
        else
        {
            isCleaning = false;
        }
        animator.SetBool("isCleaning", isCleaning);
    }

    void OnDied()
    {
        Debug.Log(gameObject.name + " is dead!");
        animator.SetTrigger("OnDeath");
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        audioSource.clip = deathSound;
        audioSource.Play();

        if (corpse)
        {
            GameObject deadBody = Instantiate(corpse, gameObject.transform.position, Quaternion.identity);
            deadBody.transform.localScale = transform.localScale;
        }
    }

    void SetProjectile()
    {
        GameObject proj = Instantiate(projectile, transform.position, Quaternion.identity);
        proj.GetComponent<Bullet>().SetDirection((mousePos - (Vector2)transform.position).normalized);

        audioSource.clip = attackSound;
        audioSource.Play();
    }
    
    private void ApplyCleaning()
    {
        if (_map == null) return;

        Vector2 moveDir = rb.linearVelocity;
        float speed = moveDir.magnitude;
        if (speed < minSpeedThreshold) return;

        moveDir.Normalize();

        if (!_map.WorldToGrid(cleaningPos.position, out var centerCell))
            return;

        if (avoidDuplicateCell && centerCell == _lastCell)
            return;

        // 지정된 크기만큼 오염 적용 (중심 기준 대칭)
        int halfRadius = cleaningRadius / 2;
        for (int x = -halfRadius; x < cleaningRadius - halfRadius; x++)
        {
            for (int y = -halfRadius; y < cleaningRadius - halfRadius; y++)
            {
                Vector2Int targetCell = centerCell + new Vector2Int(x, y);
                if (_map.IsValidGridPosition(targetCell))
                {
                    _map.SetPollution(targetCell, false); // 오염만 해제
                    //_map.CleanCell(targetCell); // 쓰레기도 같이?
                }
            }
        }

        _lastCell = centerCell;
    }
}
