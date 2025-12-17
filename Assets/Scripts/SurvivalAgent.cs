using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies; // 추가

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(StatsSystem))]
public class SurvivalAgent : Agent
{
    public float moveSpeed = 3f;

    private Rigidbody2D rb;
    private StatsSystem stats;
    private Animator animator;

    // 아이템 사용 범위, 나중에 팀원이 콜라이더/레이로 구현 가능
    public float useRadius = 0.5f;
    public LayerMask itemLayer;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<StatsSystem>();
        animator = GetComponent<Animator>();
        
        Debug.Log("SurvivalAgent Initialized!");
    }

    

    public override void OnEpisodeBegin()
    {
        // 스탯 리셋
        stats.ResetRandom();

        // 위치 리셋은 나중에 EpisodeManager에서 호출해도 되고 여기서 랜덤으로 해도 됨
        transform.position = Vector3.zero;
        rb.linearVelocity = Vector2.zero;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 스탯 4개 (0~1 정규화)
        sensor.AddObservation(stats.hp / 100f);
        sensor.AddObservation(stats.hunger / 100f);
        sensor.AddObservation(stats.thirst / 100f);
        sensor.AddObservation(stats.temperature / 100f);

        // 간단히 위치, 속도도 관측
        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(transform.position.y);
        sensor.AddObservation(rb.linearVelocity.x);
        sensor.AddObservation(rb.linearVelocity.y);

        // 시간대 (0~1)
        sensor.AddObservation(stats.TimeOfDay01);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var discrete = actions.DiscreteActions;
        int move = discrete[0]; // 0~4
        int use = discrete[1]; // 0 or 1

        // 이동
        Vector2 dir = Vector2.zero;
        switch (move)
        {
            case 1: dir = Vector2.up; break;
            case 2: dir = Vector2.down; break;
            case 3: dir = Vector2.left; break;
            case 4: dir = Vector2.right; break;
        }
        float actualSpeed = moveSpeed * stats.CurrentSpeedMultiplier;
        rb.linearVelocity = dir * actualSpeed;

        // 애니메이션 추가
        if (animator != null)
        {
            animator.SetFloat("x", dir.x);
            animator.SetFloat("y", dir.y);
        }

        // 아이템 사용
        if (use == 1)
        {
            TryUseNearbyItem();
        }

        // 리워드/종료
        StepRewards();
    }

    void StepRewards()
    {
        if (stats.IsDead)
        {
            AddReward(-1.0f);
            EndEpisode();
            return;
        }

        // 살아있는 한 작은 보상
        AddReward(+0.01f);

        // 스탯이 위험 구간이면 페널티
        if (stats.hunger < stats.lowThreshold ||
            stats.thirst < stats.lowThreshold ||
            stats.temperature < stats.lowThreshold ||
            stats.temperature > 100f - stats.lowThreshold)
        {
            AddReward(-0.01f);
        }
    }

    void TryUseNearbyItem()
    {
        // 여기서는 “계약”만 정의. 실제로는 나중에 팀원이 itemLayer / 콜라이더 세팅.
        var hits = Physics2D.OverlapCircleAll(transform.position, useRadius, itemLayer);
        foreach (var hit in hits)
        {
            var item = hit.GetComponent<Item>();
            if (item != null)
            {
                bool used = item.TryApply(stats);
                if (used)
                {
                    // 잘 사용했으면 약간의 보상
                    AddReward(+0.05f);
                    break;
                }
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Debug.Log("🎮 Heuristic called!"); // 디버그

        var discrete = actionsOut.DiscreteActions;
        int move = 0;
        int use = 0;

        if (Input.GetKey(KeyCode.W)) move = 1;
        else if (Input.GetKey(KeyCode.S)) move = 2;
        else if (Input.GetKey(KeyCode.A)) move = 3;
        else if (Input.GetKey(KeyCode.D)) move = 4;

        if (Input.GetKey(KeyCode.E)) use = 1;

        discrete[0] = move;
        discrete[1] = use;
    }
}
