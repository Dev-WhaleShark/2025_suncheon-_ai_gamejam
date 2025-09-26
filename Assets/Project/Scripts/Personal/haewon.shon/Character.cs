using UnityEngine;
using UnityEngine.InputSystem;

public class Character : MonoBehaviour
{
    [Header("Movement")]
    Rigidbody2D rb;
    private Vector2 moveInput;

    public float speed = 10.0f;

    [Header("Combat")]
    public GameObject projectile;
    public float HP = 10.0f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * speed;
    }

    void OnTakeDamage(int damage)
    {
        Debug.Log(damage);
    }

    void OnMove(InputValue value)
    {
        Vector2 inputVector = value.Get<Vector2>();
        moveInput = inputVector;
    }

    void OnAttack(InputValue value)
    {
        value.Get<float>();
        Debug.Log("Attack!");
        if (projectile)
        {
            GameObject proj = Instantiate(projectile, transform.position, Quaternion.identity);
            proj.GetComponent<Bullet>().SetDirection(Vector2.right);
            
        }
    }
}
