using FogWalker.Gameplay.Player;
using UnityEngine;
using UnityEngine.AI;

namespace FogWalker.Gameplay.AI
{
    /// <summary>
    /// پوشش NavMeshAgent برای دشمن: سرعت حالت‌محور، توقف/حرکت، رسیدن به مقصد،
    /// جدایی‌پذیری (Avoidance) که جلوی تلپورت/قرارگیری روی هم را می‌گیرد، و پارامترهای Animator.
    /// Pool-friendly: ریست کامل در OnSpawned.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyMotor : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private NavMeshAgent _agent;
        private float _walkSpeed = 2.4f;
        private float _runSpeed = 4.6f;
        private bool _running;

        /// <summary>آماده حرکت است (روی NavMesh قرار دارد)؟</summary>
        public bool IsReady => _agent != null && _agent.enabled && _agent.isOnNavMesh;
        /// <summary>به مقصد رسید؟</summary>
        public bool Arrived => IsReady && !_agent.pathPending && _agent.remainingDistance <= Mathf.Max(0.35f, _agent.stoppingDistance + 0.05f);
        /// <summary>سرعت فعلی بر اساس Agent.</summary>
        public float CurrentSpeed => IsReady ? _agent.velocity.magnitude : 0f;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            // جدایی‌پذیری: Agentها از روی هم رد نمی‌شوند/تجمع نامنظم ندارند
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            _agent.avoidancePriority = Random.Range(30, 70);
            _agent.stoppingDistance = 0.4f;
            _agent.acceleration = 12f;
            _agent.angularSpeed = 360f;
        }

        /// <summary>تنظیم سرعت‌ها از آرکی‌تایپ.</summary>
        public void Configure(float walk, float run)
        {
            _walkSpeed = walk;
            _runSpeed = run;
            ApplySpeed();
        }

        /// <summary>حرکت به مقصد.</summary>
        public bool MoveTo(Vector3 destination, bool run)
        {
            if (!IsReady) return false;
            _running = run;
            ApplySpeed();
            _agent.isStopped = false;
            return _agent.SetDestination(destination);
        }

        /// <summary>توقف در جای فعلی.</summary>
        public void Stop()
        {
            if (!IsReady) return;
            _agent.isStopped = true;
        }

        /// <summary>نیمه‌توقف (نگه‌داشتن موقعیت با چرخش).</summary>
        public void HoldPosition()
        {
            Stop();
        }

        /// <summary>چرخش نرم به سمت هدف بدون جابه‌جایی (زمان ایستادن و تیراندازی).</summary>
        public void FaceTowards(Vector3 worldPosition, float rotateSpeed = 360f)
        {
            Vector3 dir = worldPosition - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotateSpeed * Time.deltaTime);
        }

        /// <summary>ریست برای Spawn از Pool: Warp به نقطه شروع + پاک‌سازی مسیر.</summary>
        public void ResetTo(Vector3 position)
        {
            if (!_agent.enabled) _agent.enabled = true;
            if (_agent.isOnNavMesh) _agent.ResetPath();
            _agent.Warp(position);
            _agent.isStopped = false;
        }

        /// <summary>غیرفعال‌سازی کامل (مرگ یا Despawn).</summary>
        public void Deactivate()
        {
            if (_agent != null && _agent.enabled)
                _agent.enabled = false;
        }

        private void ApplySpeed()
        {
            if (_agent != null) _agent.speed = _running ? _runSpeed : _walkSpeed;
        }

        private void Update()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            SafeAnim.SetFloat(animator, "Speed", CurrentSpeed);
            SafeAnim.SetBool(animator, "IsAiming", false);
        }
    }
}
