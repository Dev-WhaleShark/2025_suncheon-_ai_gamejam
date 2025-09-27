using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public int damage = 1;
    public float lifetime = 3.0f;
    public float speed = 10.0f;
    protected Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    public void SetDirection(Vector2 dir)
    {
       rb.linearVelocity = dir * speed;

        // 회전: up(0,1) 벡터가 velocity 방향을 가리키게
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg; // - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Character characterComponent = other.GetComponent<Character>();
        if (characterComponent)
        { 
            other.GetComponent<Character>().OnTakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
