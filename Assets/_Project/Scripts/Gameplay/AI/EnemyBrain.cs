using FogWalker.Core;
using FogWalker.Gameplay.Combat;
using FogWalker.Gameplay.Player;
using FogWalker.Optimization;
using FogWalker.Utilities;
using UnityEngine;

namespace FogWalker.Gameplay.AI
{
    /// <summary>حالات FSM دشمن.</summary>
    public enum EState
    {
        Idle, Patrol, Suspicious, Alert, SeekCover, Combat,
        Flank, Reload, Retreat, Search, Stunned, Dead
    }

    /// <summary>
    /// مغز دشمن: FSM ماژولار (StateMachine&lt;EState&gt;) + وابستگی‌های Motor/Perception/Combat/Health.
    /// رفتارها: گشت، تحقیق صدا، Alert، کاور، Combat با حفظ فاصله، Flank (Rusher)، Reload، Retreat، جست‌وجو.
    /// Pool-friendly: Reset کامل در OnSpawnedFromPool؛ Disabled در Dead بدون تخریب.
    /// </summary>
    [RequireComponent(typeof(EnemyMotor))]
    [RequireComponent(typeof(EnemyPerception))]
    [RequireComponent(typeof(EnemyCombat))]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class EnemyBrain : MonoBehaviour, IPoolable
    {
        [Header("داده")]
        [SerializeField] private EnemyArchetypeDataSO data;

        [Header("گشت")]
        [SerializeField, Tooltip("نقاط گشت (اختیاری)")]
        private Transform[] patrolPoints;

        private EnemyMotor _motor;
        private EnemyPerception _perception;
        private EnemyCombat _combat;
        private HealthComponent _health;
        private StateMachine<EState> _fsm;

        private Transform _player;
        private int _patrolIndex;
        private CoverPoint _myCover;
        private Vector3 _flankTarget;
        private float _combatRepositionTimer;
        private float _searchTimer;
        private float _stunTimer;
        private bool _isDead;

        /// <summary>آرکی‌تایپ این دشمن.</summary>
        public EnemyArchetypeDataSO Data => data;
        /// <summary>حالت فعلی (برای HUD/Spawn/Debug).</summary>
        public EState CurrentState => _fsm != null ? _fsm.Current : EState.Idle;
        /// <summary>شناسه گروه Spawn برای اهداف Eliminate.</summary>
        public string SpawnGroupId { get; set; }

        private void Awake()
        {
            _motor = GetComponent<EnemyMotor>();
            _perception = GetComponent<EnemyPerception>();
            _combat = GetComponent<EnemyCombat>();
            _health = GetComponent<HealthComponent>();

            _health.OnDamaged += HandleDamaged;
            _health.OnDied += HandleDied;

            BuildFsm();
        }

        /// <summary>پیکربندی بعد از Spawn (داده + هدف + سختی).</summary>
        public void Configure(EnemyArchetypeDataSO archetype, Transform player)
        {
            data = archetype;
            _player = player;

            float healthMult = DifficultyContext.Current != null ? DifficultyContext.Current.enemyHealthMultiplier : 1f;
            _health.Initialize(data.health * healthMult, 0f, 0f);
            _motor.Configure(data.walkSpeed, data.runSpeed);
            _perception.Configure(data, player);
            _combat.Configure(data, player);
        }

        public void OnSpawnedFromPool()
        {
            _isDead = false;
            transform.rotation = Quaternion.identity; // ریست وضعیت جنازه قبلی (مرگ Placeholder)
            transform.localScale = Vector3.one;
            _perception.ResetPerception();
            _combat.ResetCombat();
            _health.Revive();
            ReleaseMyCover();
            _patrolIndex = 0;
            _fsm.Start(patrolPoints != null && patrolPoints.Length > 0 ? EState.Patrol : EState.Idle);
        }

        public void OnReturnedToPool()
        {
            ReleaseMyCover();
            _fsm.Change(EState.Idle);
        }

        private void Update()
        {
            if (!_isDead && data != null)
            {
                _fsm.Tick();
            }
        }

        // ---------- ساختار FSM ----------

        private void BuildFsm()
        {
            _fsm = new StateMachine<EState>().WithLogging(false)
                .Add(EState.Idle, onTick: TickIdle)
                .Add(EState.Patrol, onEnter: EnterPatrol, onTick: TickPatrol)
                .Add(EState.Suspicious, onEnter: EnterSuspicious, onTick: TickSuspicious)
                .Add(EState.Alert, onEnter: EnterAlert, onTick: TickAlert)
                .Add(EState.SeekCover, onEnter: EnterSeekCover, onTick: TickSeekCover, onExit: ReleaseMyCover)
                .Add(EState.Combat, onEnter: EnterCombat, onTick: TickCombat)
                .Add(EState.Flank, onEnter: EnterFlank, onTick: TickFlank)
                .Add(EState.Reload, onEnter: EnterReload, onTick: TickReload)
                .Add(EState.Retreat, onEnter: EnterRetreat, onTick: TickRetreat)
                .Add(EState.Search, onEnter: EnterSearch, onTick: TickSearch)
                .Add(EState.Stunned, onEnter: EnterStunned, onTick: TickStunned)
                .Add(EState.Dead, onEnter: EnterDead);
        }

        // ---------- توابع کمکی انتقال ----------

        private bool PlayerVisibleOrRecent() =>
            _perception.HasVisual || _perception.LastKnownAge < 2.5f;

        private bool ShouldGoAlert() => _perception.Awareness >= 1f;
        private bool ShouldInvestigate() => _perception.Awareness >= 0.3f || _perception.HeardRecently;

        private float DistanceToPlayer()
        {
            if (_player == null) return float.MaxValue;
            return Vector3.Distance(transform.position, _player.position);
        }

        private void ReleaseMyCover()
        {
            if (_myCover != null)
            {
                _myCover.Release(this);
                _myCover = null;
            }
        }

        // ---------- حالات ----------

        private void TickIdle()
        {
            if (ShouldGoAlert()) { _fsm.Change(EState.Alert); return; }
            if (ShouldInvestigate()) { _fsm.Change(EState.Suspicious); return; }
        }

        private void EnterPatrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) { _fsm.Change(EState.Idle); return; }
            _motor.MoveTo(patrolPoints[_patrolIndex % patrolPoints.Length].position, run: false);
        }

        private void TickPatrol()
        {
            if (ShouldGoAlert()) { _fsm.Change(EState.Alert); return; }
            if (ShouldInvestigate()) { _fsm.Change(EState.Suspicious); return; }

            if (_motor.Arrived)
            {
                _patrolIndex++;
                if (patrolPoints != null && patrolPoints.Length > 0)
                    _motor.MoveTo(patrolPoints[_patrolIndex % patrolPoints.Length].position, run: false);
            }
        }

        private void EnterSuspicious()
        {
            // تحقیق به سمت آخرین صدا/موقعیت
            Vector3 target = _perception.HeardRecently ? _perception.LastHeardPosition : _perception.LastKnownPlayerPosition;
            if (_perception.LastKnownAge < 3f || _perception.HeardRecently)
                _motor.MoveTo(target, run: true);
        }

        private void TickSuspicious()
        {
            if (ShouldGoAlert()) { _fsm.Change(EState.Alert); return; }
            if (_motor.Arrived)
            {
                if (_fsm.TimeInState > 3f) // کمی مکث و بازگشت
                    _fsm.Change(patrolPoints != null && patrolPoints.Length > 0 ? EState.Patrol : EState.Idle);
            }
        }

        private void EnterAlert()
        {
            _combat.BeginEngagement();
            AISoundBus.Report(transform.position, 14f, 0.6f); // فریاد هشدار به دیگران (تقویت‌جویی محدود)

            // رفتار آرکی‌تایپی: کاور، فلنک، یا مستقیم
            if (_player != null && data.canUseCover && Random.value < data.coverPreference &&
                DistanceToPlayer() > data.preferredRange.x)
            {
                _fsm.Change(EState.SeekCover);
                return;
            }
            if (data.flankChance > 0f && Random.value < data.flankChance)
            {
                _fsm.Change(EState.Flank);
                return;
            }
            _fsm.Change(EState.Combat);
        }

        private void TickAlert() { /* بلافاصله به حالت‌های دیگر می‌رود */ }

        private void EnterSeekCover()
        {
            if (_player == null) { _fsm.Change(EState.Combat); return; }

            _myCover = CoverService.FindBestCoverForAI(transform.position, _player.position, 14f);
            if (_myCover == null || !_myCover.TryOccupy(this))
            {
                _myCover = null;
                _fsm.Change(EState.Combat);
                return;
            }
            _motor.MoveTo(_myCover.transform.position, run: true);
        }

        private void TickSeekCover()
        {
            if (!PlayerVisibleOrRecent() && _perception.LastKnownAge > 5f) { _fsm.Change(EState.Search); return; }
            if (_motor.Arrived) _fsm.Change(EState.Combat);
        }

        private void EnterCombat()
        {
            _motor.HoldPosition();
            _combatRepositionTimer = 0f;
        }

        private void TickCombat()
        {
            if (_player == null) { _fsm.Change(EState.Idle); return; }

            // گم‌کردن بازیکن → جست‌وجو
            if (!PlayerVisibleOrRecent() && _perception.LastKnownAge > 3.5f)
            {
                _combat.EndEngagement();
                _fsm.Change(EState.Search);
                return;
            }

            float distance = DistanceToPlayer();
            _motor.FaceTowards(_perception.LastKnownPlayerPosition);

            // حفظ فاصله ترجیحی: نزدیک‌تر → عقب، دورتر → جلو
            bool tooFar = distance > data.preferredRange.y;
            bool tooClose = distance < data.preferredRange.x;

            _combatRepositionTimer -= Time.deltaTime;
            if (_combatRepositionTimer <= 0f && (tooFar || tooClose))
            {
                _combatRepositionTimer = 1.2f;
                Vector3 dir = tooFar
                    ? (_player.position - transform.position).normalized
                    : (transform.position - _player.position).normalized;
                Vector3 candidate = transform.position + dir * (tooFar ? 4f : 3f);
                _motor.MoveTo(candidate, run: tooFar);
            }
            else if (_motor.Arrived)
            {
                _motor.HoldPosition();
            }

            // عقب‌نشینی کم‌جان (اگر آرکی‌تایپ اجازه دهد)
            if (data.retreatBelowHealth > 0f && _health.Normalized < data.retreatBelowHealth)
            {
                _fsm.Change(EState.Retreat);
                return;
            }

            // شلیک (فقط با دید واقعی)
            if (_combat.IsEngaged && _perception.HasVisual)
            {
                bool shot = _combat.TryShootOnce();
                if (!shot && _combatRepositionTimer <= 0f && distance < data.preferredRange.y)
                {
                    // برست تمام: گاهی عقب‌بکش کوچک
                    _combatRepositionTimer = 0.8f;
                }
            }
        }

        private void EnterFlank()
        {
            if (_player == null) { _fsm.Change(EState.Combat); return; }

            // هدف فلنک: نقطه‌ای کنار بازیکن (90 درجه) در فاصله ۳-۶ متری
            Vector3 toPlayer = (_player.position - transform.position).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, toPlayer) * (Random.value < 0.5f ? 1f : -1f);
            _flankTarget = _player.position + side * Random.Range(3f, 6f);
            _motor.MoveTo(_flankTarget, run: true);
        }

        private void TickFlank()
        {
            if (_motor.Arrived || _fsm.TimeInState > 4f) _fsm.Change(EState.Combat);
            if (!PlayerVisibleOrRecent() && _perception.LastKnownAge > 4f) _fsm.Change(EState.Search);
        }

        private void EnterReload() { _motor.HoldPosition(); }
        private void TickReload()
        {
            if (_fsm.TimeInState > 1.6f) _fsm.Change(EState.Combat);
        }

        private void EnterRetreat()
        {
            if (_player == null) { _fsm.Change(EState.Combat); return; }
            Vector3 away = (transform.position - _player.position).normalized;
            _motor.MoveTo(transform.position + away * 8f, run: true);
        }

        private void TickRetreat()
        {
            if (_motor.Arrived || _fsm.TimeInState > 3f)
                _fsm.Change(EState.Combat);
        }

        private void EnterSearch()
        {
            _searchTimer = 6f;
            _motor.MoveTo(_perception.LastKnownPlayerPosition, run: false);
        }

        private void TickSearch()
        {
            if (ShouldGoAlert()) { _fsm.Change(EState.Alert); return; }

            _searchTimer -= Time.deltaTime;
            if (_motor.Arrived && _searchTimer <= 0f)
                _fsm.Change(patrolPoints != null && patrolPoints.Length > 0 ? EState.Patrol : EState.Idle);
            else if (_motor.Arrived)
            {
                // پرسه کوچک اطراف آخرین موقعیت
                Vector2 rnd = Random.insideUnitCircle * 3f;
                Vector3 target = _perception.LastKnownPlayerPosition + new Vector3(rnd.x, 0f, rnd.y);
                _motor.MoveTo(target, run: false);
            }
        }

        private void EnterStun(float duration)
        {
            _motor.HoldPosition();
            _stunTimer = duration;
        }

        private void EnterStunned() { /* توسط EnterStun مدیریت می‌شود */ }
        private void TickStunned()
        {
            _stunTimer -= Time.deltaTime;
            if (_stunTimer <= 0f)
                _fsm.Change(_perception.Awareness >= 1f ? EState.Alert : EState.Search);
        }

        private void EnterDead()
        {
            _motor.Deactivate();
            ReleaseMyCover();
            _combat.EndEngagement();
        }

        // ---------- رویدادهای سلامت ----------

        private void HandleDamaged(DamageInfo info, float remain)
        {
            if (_isDead) return;

            // ضربه سنگین (انفجار/Headshot) → غافلگیری کوتاه
            bool heavy = info.Type == DamageType.Explosion || info.Amount >= 45f;
            if (heavy && _fsm.Current != EState.Dead)
            {
                EnterStun(0.8f);
                _fsm.Change(EState.Stunned);
            }

            // آگاهی سریع از موقعیت حمله‌کننده
            if (info.Instigator != null)
            {
                _perception.ResetPerception();
                _perception.Configure(data, _player);
            }
        }

        private void HandleDied(DamageInfo lastHit)
        {
            if (_isDead) return;
            _isDead = true;
            _fsm.Change(EState.Dead);

            // گزارش مرگ به سیستم‌ها (Spawn/Mission)
            EnemyLifecycleEvents.RaiseEnemyDied(this);

            // مرگ کم‌هزینه (بدون Ragdoll کامل): خوابیدن + برگشت به Pool بعد از چند ثانیه
            StartCoroutine(DeathCleanupRoutine());
        }

        private System.Collections.IEnumerator DeathCleanupRoutine()
        {
            // انیمیشن مرگ جایگزین‌پذیر؛ Placeholder: چرخش روی زمین + کاهش Scale
            float t = 0f;
            Quaternion start = transform.rotation;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                transform.rotation = start * Quaternion.Euler(90f * (t / 0.4f), 0f, 0f);
                yield return null;
            }
            yield return new WaitForSeconds(2.2f);

            var poolable = GetComponent<PoolableObject>();
            if (poolable != null) poolable.Release();
            else gameObject.SetActive(false);
        }
    }

    /// <summary>رویداد چرخه عمر دشمن‌ها (مرگ با گروه Spawn).</summary>
    public static class EnemyLifecycleEvents
    {
        /// <summary>پس از مرگ هر دشمن.</summary>
        public static event System.Action<EnemyBrain> OnEnemyDied;
        public static void RaiseEnemyDied(EnemyBrain enemy) => OnEnemyDied?.Invoke(enemy);
    }
}
