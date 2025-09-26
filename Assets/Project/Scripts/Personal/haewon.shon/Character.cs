using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Character : MonoBehaviour
{
    [Header("Movement")]
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

    private float currentHealth;
    private float immuneTimer = 1.0f;

    private bool hasSlowDebuff = false;
    private float slowTimer = 0.0f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
        immuneTimer -= Time.fixedDeltaTime;

        if(hasSlowDebuff)
        {
            slowTimer -= Time.fixedDeltaTime;
            if(slowTimer <= 0.0f)
            {
                hasSlowDebuff = false;
                moveSpeed /= (1.0f - 0.5f); // Assuming the slow amount is always 50%
            }
        }
    }

    public void OnTakeDamage(int damage)
    {
        if(immuneTimer > 0) return;

        currentHealth -= damage;
        immuneTimer = immuneTime;
        Debug.Log("health: " + currentHealth.ToString());
        if (currentHealth <= 0)
        {
            OnDied();
        }
    }

    public void OnSlow(float percent, float duration)
    {
        if(hasSlowDebuff)
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
        Vector2 inputVector = value.Get<Vector2>();
        moveInput = inputVector;
    }

    void OnAttack(InputValue value) // LMB
    {
        if (projectile)
        {
            GameObject proj = Instantiate(projectile, transform.position, Quaternion.identity);
            proj.GetComponent<Bullet>().SetDirection(Vector2.right);
        }
    }

    void OnDied()
    { 
        Debug.Log(gameObject.name + " is dead!");
        //Destroy(gameObject);
    }
}
