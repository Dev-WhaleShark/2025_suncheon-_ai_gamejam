using Unity.VisualScripting;
using UnityEngine;

public class TrashPile : Enemy
{
    public GameObject sludgePrefab;
    public float moveRouteRadius = 10.0f;
    public int numberOfPoints = 32;

    private Vector2[] points;
    private int targetPointIndex = 0;
    private float pointReachedThreshold = 0.5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        base.Start();

        points = new Vector2[numberOfPoints];
        for (int i = 0; i < numberOfPoints; i++)
        {
            float angle = i * Mathf.PI * 2 / numberOfPoints;
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * moveRouteRadius + new Vector2(transform.position.x, transform.position.y);
        }

        targetPointIndex = Random.Range(0, numberOfPoints);
        transform.position = new Vector3(points[targetPointIndex].x, points[targetPointIndex].y, -1);
    }

    // Update is called once per frame
    protected override void Update()
    {
        if (currentState == EnemyState.Dead) return;

        StateLogic();
    }

    protected override void Idle()
    {
        currentState = EnemyState.Move;
    }

    protected override void Move()
    {
        Vector2 targetPoint = points[targetPointIndex];
        if (Vector2.Distance(transform.position, targetPoint) < pointReachedThreshold)
        {
            targetPointIndex = (targetPointIndex + 1) % numberOfPoints;
            targetPoint = points[targetPointIndex];
            rb.linearVelocity = (targetPoint - (Vector2)transform.position).normalized * moveSpeed;
        }

        if (canAttack)
        { 
            currentState = EnemyState.Attack;
        }
    }
    protected override void Attack()
    {
        if (!canAttack) return;

        Instantiate(sludgePrefab, transform.position, Quaternion.identity);
        currentState = EnemyState.Move;

        // 공격 구현 (예: 데미지 전달)
        StartCoroutine(AttackCooldown());
    }
}
