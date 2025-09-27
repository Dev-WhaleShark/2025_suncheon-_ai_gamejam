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
}
