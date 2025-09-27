using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage = 1;
    public float lifetime = 3.0f;
    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {

    }
    public void SetDirection(Vector2 dir)
    {
       rb.linearVelocity = dir * 10.0f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Hit " + other.name);
        Enemy enemyComponent = other.GetComponent<Enemy>();
        if (enemyComponent)
        { 
            enemyComponent.OnTakeDamage(damage, rb.linearVelocityX);
            Destroy(gameObject);
        }
    }
}
