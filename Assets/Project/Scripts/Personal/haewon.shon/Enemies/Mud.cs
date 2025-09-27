using UnityEngine;
using System.Collections;

public class Mud : EnemyProjectile
{
    private Collider2D collider;
    private SpriteRenderer renderer;
    public float flightTime;
    public float afterEffectLifeTime;
    public float collisionLifeTime;
    public Sprite brokenMudSprite;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
        renderer = GetComponent<SpriteRenderer>();
        //Destroy(gameObject, flightTime);

        collider.enabled = false;
        StartCoroutine(SetLifeCycleTimer(flightTime - collisionLifeTime));
    }

    void FixedUpdate()
    {
        if (rb.simulated)
        { 
            Vector2 v = rb.linearVelocity;

            // 속도가 거의 0이면 방향 계산을 건너뛰기
            if (v.sqrMagnitude < 0.0001f) return;

            // 속도 벡터의 각도(라디안 -> 도) 구하기
            float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;

            // 로컬 Up이 속도 방향을 향하도록 Z축 회전
            // 현재 Up이 +Y이므로 +90도 보정
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
    }

    protected virtual IEnumerator SetLifeCycleTimer(float timer)
    {
        yield return new WaitForSeconds(timer);
        collider.enabled = true;
        yield return new WaitForSeconds(collisionLifeTime);
        // sprite set
        renderer.sprite = brokenMudSprite;
        transform.rotation = Quaternion.identity;
        collider.enabled = false;
        rb.simulated = false;
        Destroy(gameObject, afterEffectLifeTime);
    }

    public void SetDestination(Vector2 dest)
    { 
        float t = flightTime; // 원하는 비행 시간
        float g = Mathf.Abs(Physics2D.gravity.y);

        float vx = (dest.x - transform.position.x) / t;
        float vy = (dest.y - transform.position.y + 0.5f * g * t * t) / t;

        rb.linearVelocity = new Vector2(vx, vy);
    }
}
