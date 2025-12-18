using System;
using System.IO;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(StatsSystem))]
public class SurvivalAgent : Agent
{
    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Item Use")]
    public float useRadius = 0.5f;
    public LayerMask itemLayer;

    [Header("Experiment Logging")]
    public string experimentTag = "ppo";
    public bool logEpisodeToConsole = false;

    [Header("Reward (aligned with survival)")]
    [Tooltip("살아있는 것 자체 보상(누적). 너무 크면 보상만 먹고 놀 수 있음.")]
    public float aliveReward = 0.002f;

    [Tooltip("위험도(RiskLevel01, 0~1)에 비례한 페널티 가중치. aliveReward보다 크면 장기 생존이 벌점이 됨.")]
    public float riskPenaltyWeight = 0.0015f;

    [Tooltip("위험도 변화(이전-현재)가 줄어들면 +, 늘면 - 주는 shaping 가중치.")]
    public float deltaRiskShaping = 0.01f;

    [Tooltip("Hazard 존에 있으면 추가로 매 decision 페널티")]
    public float hazardPenalty = 0.0005f;

    [Tooltip("사망 시 큰 페널티(긴 생존이 이득이 되게 -1보다 크게)")]
    public float deathPenalty = -5.0f;

    [Tooltip("아이템 사용 성공 보상(너무 크면 아이템만 쫓음)")]
    public float itemUseReward = 0.02f;

    private Rigidbody2D rb;
    private StatsSystem stats;
    private Animator animator;

    // Episode tracking (per process)
    private int episodeIndex = 0;
    private int episodeDecisions = 0;
    private float lastEpisodeReward = 0f;
    private string lastEndReason = "unknown";
    private bool prevEpisodeRecorded = true;

    // Timing
    private int decisionPeriod = 1;

    // Academy step tracking
    private int lastAcademyStepSeen = 0;

    // CSV logging
    private int pid;
    private string stamp;
    private string csvPath;

    // Shaping state
    private float prevRisk = 0f;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<StatsSystem>();
        animator = GetComponent<Animator>();

        var dr = GetComponent<Unity.MLAgents.DecisionRequester>();
        if (dr != null) decisionPeriod = dr.DecisionPeriod;

        // tag 우선순위: 커맨드라인(-tag) > 환경변수(EXPERIMENT_TAG) > 인스펙터
        string argTag = GetArgValue("-tag");
        if (!string.IsNullOrEmpty(argTag)) experimentTag = argTag;

        string envTag = Environment.GetEnvironmentVariable("EXPERIMENT_TAG");
        if (!string.IsNullOrEmpty(envTag)) experimentTag = envTag;

        experimentTag = SanitizeTag(experimentTag);

        pid = System.Diagnostics.Process.GetCurrentProcess().Id;
        stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        string dir = Path.Combine(Application.persistentDataPath, "metrics", experimentTag, stamp);
        Directory.CreateDirectory(dir);

        csvPath = Path.Combine(dir, $"survival_pid{pid}.csv");

        File.WriteAllText(
            csvPath,
            "tag,stamp,pid,episode,academy_step_end,decisions,sim_seconds_est,reward,end_reason\n"
        );

        lastAcademyStepSeen = Academy.Instance.StepCount;

        UnityEngine.Debug.Log($"[METRIC] Initialized tag={experimentTag} pid={pid} csv={csvPath}");
    }

    public override void OnEpisodeBegin()
    {
        // 직전 에피소드가 MaxStep 등으로 자동 종료된 경우 기록
        if (!prevEpisodeRecorded && episodeIndex > 0)
        {
            if (string.IsNullOrEmpty(lastEndReason) || lastEndReason == "unknown")
                lastEndReason = "maxstep";

            RecordEpisode(lastEndReason);
        }

        episodeIndex++;
        episodeDecisions = 0;
        lastEpisodeReward = 0f;
        lastEndReason = "unknown";
        prevEpisodeRecorded = false;

        stats.ResetRandom();
        transform.position = Vector3.zero;
        rb.linearVelocity = Vector2.zero;

        // shaping 초기화
        prevRisk = stats.RiskLevel01;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // ✅ 0~1 스케일로 정리 (온도는 Temperature01 사용!)
        sensor.AddObservation(stats.Hp01);
        sensor.AddObservation(stats.Hunger01);
        sensor.AddObservation(stats.Thirst01);
        sensor.AddObservation(stats.Temperature01);

        // 위치/속도 (맵이 크면 정규화 권장)
        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(transform.position.y);
        sensor.AddObservation(rb.linearVelocity.x);
        sensor.AddObservation(rb.linearVelocity.y);

        // 시간대
        sensor.AddObservation(stats.TimeOfDay01);

        // 존 상태
        sensor.AddObservation(stats.IsInBonfire ? 1f : 0f);
        sensor.AddObservation(stats.IsInForest ? 1f : 0f);
        sensor.AddObservation(stats.IsInHazard ? 1f : 0f);

        // 총 12개 관측 유지
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        episodeDecisions++;
        lastAcademyStepSeen = Academy.Instance.StepCount;

        var discrete = actions.DiscreteActions;
        int move = discrete[0];
        int use = discrete[1];

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

        // 보상/종료
        StepRewards();

        // MaxStep 자동 종료 감지(기록용)
        if (!prevEpisodeRecorded && MaxStep > 0 && StepCount >= MaxStep - 1)
        {
            lastEndReason = "maxstep";
        }
    }

    private void StepRewards()
    {
        // 사망
        if (stats.IsDead)
        {
            AddReward(deathPenalty);
            lastEndReason = "death";
            RecordEpisode(lastEndReason);
            EndEpisode();
            return;
        }

        // 1) 살아있음 보상(누적)
        AddReward(aliveReward);

        // 2) 위험도 기반 페널티: 위험도 0~1에 비례
        float risk = stats.RiskLevel01;
        AddReward(-riskPenaltyWeight * risk);

        // 3) 위험도 변화 shaping: 위험이 줄면 +, 늘면 -
        //    (예: 아이템 먹고 위험 내려가면 보상)
        float delta = prevRisk - risk; // 양수면 개선
        AddReward(deltaRiskShaping * delta);
        prevRisk = risk;

        // 4) Hazard 존 추가 페널티(회피 유도)
        if (stats.IsInHazard)
        {
            AddReward(-hazardPenalty);
        }
    }

    private void TryUseNearbyItem()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, useRadius, itemLayer);
        foreach (var hit in hits)
        {
            var item = hit.GetComponent<Item>();
            if (item != null)
            {
                if (item.TryApply(stats))
                {
                    // ✅ 아이템 사용 성공 보상(너무 크게 주지 말 것)
                    AddReward(itemUseReward);
                    break;
                }
            }
        }
    }

    private void RecordEpisode(string reason)
    {
        lastEpisodeReward = GetCumulativeReward();

        int academyEnd = lastAcademyStepSeen > 0 ? lastAcademyStepSeen : Academy.Instance.StepCount;
        float simSecondsEst = episodeDecisions * decisionPeriod * Time.fixedDeltaTime;

        string line =
            $"{experimentTag},{stamp},{pid},{episodeIndex},{academyEnd},{episodeDecisions},{simSecondsEst:F4},{lastEpisodeReward:F4},{reason}\n";

        File.AppendAllText(csvPath, line);
        prevEpisodeRecorded = true;

        if (logEpisodeToConsole)
        {
            UnityEngine.Debug.Log($"[METRIC] tag={experimentTag} ep={episodeIndex} reason={reason} decisions={episodeDecisions} reward={lastEpisodeReward:F2}");
        }
    }

    private void OnDisable()
    {
        try
        {
            if (!prevEpisodeRecorded && episodeIndex > 0)
            {
                RecordEpisode("quit");
            }
        }
        catch { }
    }

    private string GetArgValue(string key)
    {
        try
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == key) return args[i + 1];
            }
        }
        catch { }
        return null;
    }

    private string SanitizeTag(string s)
    {
        if (string.IsNullOrEmpty(s)) return "untagged";
        foreach (char c in Path.GetInvalidFileNameChars())
            s = s.Replace(c.ToString(), "_");
        return s.Trim();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
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
