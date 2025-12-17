using UnityEngine;

public class Scorpion : MonoBehaviour
{
    [Header("Movement")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 3f;
    
    [Header("Patrol (River 위 왔다갔다)")]
    public Transform pointA;  // 순찰 시작점
    public Transform pointB;  // 순찰 끝점
    private Transform currentTarget;
    
    [Header("Detection (시야각)")]
    public float viewDistance = 5f;      // 감지 거리
    [Range(0, 180)]
    public float viewAngle = 60f;        // 전방 시야각 (좌우 각각)
    public LayerMask agentLayer;         // Agent 레이어
    
    [Header("Chase Zone")]
    public Collider2D forestZoneBounds;  // ForestZone Collider 연결
    
    [Header("Attack")]
    public float attackDamage = 10f;
    public float attackCooldown = 1f;
    private float lastAttackTime;
    
    // 상태
    private Transform detectedAgent;
    private bool isChasing = false;
    private Vector2 moveDirection;
    private SpriteRenderer sr;
    private Animator animator;

    void Start()
    {
        currentTarget = pointB;
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();  // 추가

    }

    void Update()
    {
        // 1. Agent 감지 시도
        TryDetectAgent();
        
        // 2. 상태에 따라 행동
        if (isChasing && detectedAgent != null)
        {
            ChaseAgent();
        }
        else
        {
            Patrol();
        }
        
        // 3. 스프라이트 방향
        if (sr != null && moveDirection.x != 0)
        {
            sr.flipX = moveDirection.x < 0;
        }
    }

    void TryDetectAgent()
    {
        // 범위 내 Agent 찾기
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, viewDistance, agentLayer);
        
        detectedAgent = null;
        
        foreach (var hit in hits)
        {
            Vector2 dirToAgent = (hit.transform.position - transform.position).normalized;
            float angle = Vector2.Angle(moveDirection, dirToAgent);
            
            // 시야각 내에 있는지 확인
            if (angle <= viewAngle)
            {
                // Raycast로 장애물 체크 (선택사항)
                RaycastHit2D ray = Physics2D.Raycast(transform.position, dirToAgent, viewDistance, agentLayer);
                if (ray.collider != null)
                {
                    detectedAgent = hit.transform;
                    isChasing = true;
                    Debug.Log("🦂 Agent 발견!");
                    break;
                }
            }
        }
        
        // Agent를 놓쳤으면 추격 중단
        if (detectedAgent == null)
        {
            isChasing = false;
        }
    }

    void Patrol()
    {
        // pointA ↔ pointB 왕복
        if (currentTarget == null) return;
        
        Vector2 dir = (currentTarget.position - transform.position).normalized;
        moveDirection = dir;
        transform.position += (Vector3)(dir * patrolSpeed * Time.deltaTime);
        
        // 도착하면 방향 전환
        if (Vector2.Distance(transform.position, currentTarget.position) < 0.2f)
        {
            currentTarget = (currentTarget == pointA) ? pointB : pointA;
        }
    }

    void ChaseAgent()
    {
        if (detectedAgent == null) return;
        
        Vector2 targetPos = detectedAgent.position;
        
        // ForestZone 범위 내로 제한
        if (forestZoneBounds != null)
        {
            targetPos = forestZoneBounds.ClosestPoint(detectedAgent.position);
            
            // Agent가 ForestZone 밖이면 추격 중단
            if (!forestZoneBounds.OverlapPoint(detectedAgent.position))
            {
                isChasing = false;
                Debug.Log("🦂 Agent가 영역을 벗어남, 순찰로 복귀");
                return;
            }
        }
        
        Vector2 dir = (targetPos - (Vector2)transform.position).normalized;
        moveDirection = dir;
        transform.position += (Vector3)(dir * chaseSpeed * Time.deltaTime);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Agent와 충돌 시 데미지
        if (Time.time - lastAttackTime < attackCooldown) return;
        
        StatsSystem stats = other.GetComponent<StatsSystem>();
        if (stats != null)
        {
            stats.ApplyDelta(-attackDamage, 0, 0, 0);
            lastAttackTime = Time.time;
            Debug.Log($"🦂 Agent 공격! -{attackDamage} HP");
        }
    }

    // 디버그용: Scene에서 시야각 표시
    void OnDrawGizmosSelected()
    {
        // 감지 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);
        
        // 시야각
        Vector3 leftBound = Quaternion.Euler(0, 0, viewAngle) * (Vector3)moveDirection * viewDistance;
        Vector3 rightBound = Quaternion.Euler(0, 0, -viewAngle) * (Vector3)moveDirection * viewDistance;
        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + leftBound);
        Gizmos.DrawLine(transform.position, transform.position + rightBound);
    }
}