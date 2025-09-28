using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;
using WhaleShark.Core;
using WhaleShark.Gameplay;
using Random = UnityEngine.Random;

[Serializable]
public enum BuffType
{
    CLEANSING_AURA = 0, // 확인
    FEATHER_BLADE, // 확인
    STORM_PURGE, // 확인
    LIFE_STEAL,
    LONG_LEGS, // o
    PURIFICATION_ZONE_BUFF,
    SHARP_BEAK,
    SPRING_OF_LIFE,
    TAIL_WIND, // o
    THICK_FEATHER,
    BUFF_COUNT
};

public class Character : MonoBehaviour
{
    Rigidbody2D rb;
    private Vector2 moveInput;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 2f;

    //public float attackRange = 1.5f;
    //public float attackCooldown = 2f;
    //public int attackDamage = 10;
    public float immuneTime = 1.0f;

    /// <summary>
    /// buff multipliers
    /// </summary>
    private float attackMultiplier = 1.0f;
    private float defenseRate = 0.0f;

    private float speedMultiplier = 1.0f;

    private HPBarUI HPBar;

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
    [SerializeField] private int characterRadius = 1;

    public AudioClip cleaningSound;

    private Vector2Int _lastCell = new Vector2Int(int.MinValue, int.MinValue);

    private Stage _map;

    private bool isInPurifiedArea = false;

    void Awake()
    {
        _map = _map ?? FindFirstObjectByType<Stage>();
    }

    public void ResetMap()
    {
        _map = FindFirstObjectByType<Stage>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        currentHealth = maxHealth;

        WhaleShark.Core.EventBus.RewardCollected += RewardCollected;

        //////////////
        /// 테스트용 버프 적용
        ///
        // ApplyBuff(BuffType.TAIL_WIND); // 추가타

        HPBar = FindAnyObjectByType<HPBarUI>();
        if (HPBar)
        {
            Debug.Log("HP Interface Set");
            HPBar.Initialize(maxHealth);
        }
    }

    private void OnDestroy()
    {
        WhaleShark.Core.EventBus.RewardCollected -= RewardCollected;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isDead) return;

        rb.linearVelocity = moveInput * moveSpeed * speedMultiplier;
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
            ApplyCleaning(cleaningRadius);
        }

        isInPurifiedArea = IsInPurifiedArea();
    }

    public void OnTakeDamage(int damage)
    {
        if (isDead) return;
        if (immuneTimer > 0) return;

        damage = (int)Mathf.Ceil(damage * (1.0f - defenseRate));
        currentHealth -= damage;
        immuneTimer = immuneTime;

        Debug.Log("health: " + currentHealth.ToString());
        if (HPBar)
        {
            HPBar.UpdateHPBar(Mathf.Max(currentHealth, 0.0f));
        }

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
        if (isDead) return;

        if (value.isPressed)
        {
            isCleaning = true;
            audioSource.clip = cleaningSound;
            audioSource.loop = true;
            audioSource.Play();
        }
        else
        {
            isCleaning = false;
            audioSource.Stop();
            audioSource.loop = false;
        }
        animator.SetBool("isCleaning", isCleaning);
    }

    void OnDied()
    {
        if (isDead) return;

        Debug.Log(gameObject.name + " is dead!");
        animator.SetTrigger("OnDeath");
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        audioSource.clip = deathSound;
        audioSource.Play();

        GameManager.Instance.OnPlayerDied();

        if (corpse)
        {
            GameObject deadBody = Instantiate(corpse, gameObject.transform.position, Quaternion.identity);
            deadBody.transform.localScale = transform.localScale;
        }
    }

    void SetProjectile()
    {
        GameObject proj = Instantiate(projectile, transform.position, Quaternion.identity);
        Bullet bulletComponent = proj.GetComponent<Bullet>();
        bulletComponent.SetDirection((mousePos - (Vector2)transform.position).normalized);

        int damage = bulletComponent.damage;
        damage = (int)(damage * (attackMultiplier + (hasPurificationZoneBuff && isInPurifiedArea ? 0.1f : 0.0f))); // 버프 & 정화구역내 10%추가
        bulletComponent.damage = damage;

        if (hasFeatherBlade)
        {
            float randVal = Random.Range(0.0f, 1.0f);
            if (randVal < 0.2f)
            {
                // 추가깃털 발사
                GameObject bonousProj = Instantiate(projectile, transform.position, Quaternion.identity);
                bulletComponent = bonousProj.GetComponent<Bullet>();
                Vector2 baseDir = (mousePos - (Vector2)transform.position).normalized;

                // -5도 ~ +5도 중 랜덤 각도
                float angle = Random.Range(-5f, 5f);

                // Z축을 기준으로 회전
                Vector2 spreadDir = Quaternion.Euler(0f, 0f, angle) * baseDir;

                // 발사
                bulletComponent.SetDirection(spreadDir);
                bulletComponent.damage = damage;
            }
        }

        audioSource.clip = attackSound;
        audioSource.Play();
    }

    private void ApplyCleaning(float radius)
    {
        if (_map == null) return;

        Vector2 moveDir = rb.linearVelocity;
        float speed = moveDir.magnitude;
        if (speed < minSpeedThreshold) return;

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

    private bool IsInPurifiedArea()
    {
        if (_map == null) return false;

        Vector2 moveDir = rb.linearVelocity;
        float speed = moveDir.magnitude;
        if (speed < minSpeedThreshold) return false;

        moveDir.Normalize();

        if (!_map.WorldToGrid(cleaningPos.position, out var centerCell))
            return false;

        // 지정된 크기만큼 오염 적용 (중심 기준 대칭)
        int halfRadius = characterRadius / 2;
        for (int x = -halfRadius; x < characterRadius - halfRadius; x++)
        {
            for (int y = -halfRadius; y < characterRadius - halfRadius; y++)
            {
                Vector2Int targetCell = centerCell + new Vector2Int(x, y);
                if (_map.IsValidGridPosition(targetCell))
                {
                    if (_map.HasPollution(targetCell))
                        return false;
                }
            }
        }

        return true;
    }

    private bool hasFeatherBlade = false;
    private bool hasPurificationZoneBuff = false;
    void ApplyBuff(BuffType type)
    {
        switch (type)
        {
            case BuffType.CLEANSING_AURA: // 자동정화
                StartCoroutine(ApplyCleaningAura());
                break;
            case BuffType.FEATHER_BLADE: // 추가 공격 확률
                hasFeatherBlade = true;
                break;
            case BuffType.STORM_PURGE: // 정화범위 5초간 2배
                StartCoroutine(ApplyStormPurge());
                break;
            case BuffType.LIFE_STEAL:
                break;
            case BuffType.LONG_LEGS:  // fixedupdate 적용
                speedMultiplier += 0.2f;
                break;
            case BuffType.PURIFICATION_ZONE_BUFF: // 정화구역 피해량증가 - SetProjectile에서 적용
                hasPurificationZoneBuff = true;
                break;
            case BuffType.SHARP_BEAK: // SetProjectile에서 적용
                attackMultiplier += 0.15f;
                break;
            case BuffType.SPRING_OF_LIFE:
                StartCoroutine(ApplySpringOfLife());
                break;
            case BuffType.TAIL_WIND:
                StartCoroutine(ApplyTailWind());
                break;
            case BuffType.THICK_FEATHER: // OnTakeDamage에서 적용
                defenseRate += 0.1f;
                break;
        }
    }

    private IEnumerator ApplyStormPurge() // 정화범위 5초간 증가
    {
        cleaningRadius *= 2;
        yield return new WaitForSeconds(5.0f);
        cleaningRadius /= 2;
    }

    private IEnumerator ApplySpringOfLife() // 정화구역내 5초마다 회복
    {
        while (!isDead) // ...
        {
            yield return new WaitForSeconds(5.0f);
            if (isInPurifiedArea)
            {
                currentHealth += 2;
            }
        }
    }

    private IEnumerator ApplyTailWind() // 이동시(0.5초단위 체크) 확률적 속도버프
    {
        while (!isDead)
        {
            if (rb.linearVelocity.magnitude > 0)
            {
                speedMultiplier += 0.5f;
                yield return new WaitForSeconds(2.0f);
                speedMultiplier -= 0.5f;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator ApplyCleaningAura() // 플레이어 주변 1.0m 범위의 오염된 타일이 초당 1개씩 자동으로 정화됩니다.
    {
        while (!isDead)
        {
            ApplyCleaning(1.0f);
            yield return new WaitForSeconds(1.0f);
        }
    }

    private void RewardCollected(RewardData data)
    {
        Debug.Log("Player Collected Reward: " + (data ? data.id : "(null)") );
        if (data != null && data.buffType != BuffType.BUFF_COUNT)
        {
            ApplyBuff(data.buffType);
        }
    }
}
