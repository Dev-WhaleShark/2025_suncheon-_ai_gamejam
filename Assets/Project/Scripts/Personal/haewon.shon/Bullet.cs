using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage = 1;
    public float lifetime = 3.0f;
    
    public GameObject explosionPrefab;

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

        // 회전: up(0,1) 벡터가 velocity 방향을 가리키게
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg; // - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Hit " + other.name);
        Enemy enemyComponent = other.GetComponent<Enemy>();
        if (enemyComponent)
        {
            if (explosionPrefab)
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.AngleAxis(Random.Range(0.0f, 360.0f), new Vector3(0.0f, 0.0f, 1.0f)));
            }
            
            enemyComponent.OnTakeDamage(damage, rb.linearVelocityX);
            Destroy(gameObject);
            
        }
    }
}
