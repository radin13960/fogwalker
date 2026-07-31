using System.Collections.Generic;
using UnityEngine;

namespace FogWalker.Gameplay.Player
{
    /// <summary>
    /// نقطه کاور روی لبه‌ی یک مانع: جهت بیرون کاور (normal)، اشغال‌بودن، و امکان Peek.
    /// توسط بازیکن و AI مشترکاً استفاده می‌شود (رجیستری سراسری CoverService).
    /// قرارگیری: روی GameObject لایه Cover، نزدیک سطح مانع، نرمال به‌سمت بیرون دیوار.
    /// </summary>
    public sealed class CoverPoint : MonoBehaviour
    {
        [Tooltip("جهت بیرون کاور (سمت امنِ پشت مانع)")]
        public Vector3 forward = Vector3.forward;

        [Tooltip("آیا امکان تیراندازی از بالای کاور هست؟")]
        public bool allowPeekOver = true;

        /// <summary>آیا اشغال است؟</summary>
        public bool IsOccupied { get; private set; }
        /// <summary>اشغال‌کننده فعلی.</summary>
        public Component Occupant { get; private set; }
        /// <summary>جهت بیرون کاور در فضای جهان (نرمال‌شده).</summary>
        public Vector3 WorldForward => transform.TransformDirection(forward).normalized;
        /// <summary>امتداد مانع (عمود بر نرمال) برای حرکت در کاور.</summary>
        public Vector3 WorldTangent => Vector3.Cross(Vector3.up, WorldForward).normalized;

        private void OnEnable() => CoverService.Register(this);
        private void OnDisable() => CoverService.Unregister(this);

        /// <summary>اشغال/آزادسازی کاور.</summary>
        public bool TryOccupy(Component by)
        {
            if (IsOccupied && Occupant != by) return false;
            IsOccupied = true;
            Occupant = by;
            return true;
        }

        /// <summary>آزادسازی.</summary>
        public void Release(Component by)
        {
            if (Occupant == by)
            {
                IsOccupied = false;
                Occupant = null;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = IsOccupied ? Color.red : Color.green;
            Gizmos.DrawRay(transform.position + Vector3.up, WorldForward);
            Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(0.8f, 0.4f, 0.2f));
        }
#endif
    }

    /// <summary>
    /// رجیستری و کوئری کاور: نزدیک‌ترین نقطه آزاد برای بازیکن، و انتخاب کاور تاکتیکی برای AI
    /// (نزدیک به دشمن، دور از خط تهدید، بدون تداخل اشغال).
    /// </summary>
    public static class CoverService
    {
        private static readonly List<CoverPoint> Points = new List<CoverPoint>(64);

        public static void Register(CoverPoint p) { if (!Points.Contains(p)) Points.Add(p); }
        public static void Unregister(CoverPoint p) { Points.Remove(p); }

        /// <summary>نزدیک‌ترین نقطه کاور آزاد به یک موقعیت (برای دکمه Cover بازیکن).</summary>
        public static CoverPoint FindNearestFree(Vector3 position, float maxDistance)
        {
            CoverPoint best = null;
            float bestDist = maxDistance;
            for (int i = 0; i < Points.Count; i++)
            {
                CoverPoint p = Points[i];
                if (p == null || p.IsOccupied) continue;
                float d = Vector3.Distance(position, p.transform.position);
                if (d < bestDist) { bestDist = d; best = p; }
            }
            return best;
        }

        /// <summary>
        /// انتخاب کاور هوشمند برای AI حول یک نقطه: نزدیک به جست‌وجوگر، دور از زاویه تهدید، آزاد.
        /// امتیاز = فاصله از AI×w1 + فاصله از تهدید×w2 (بیشتر بهتر) + جریمه اگر در دید تهدید.
        /// </summary>
        public static CoverPoint FindBestCoverForAI(Vector3 seekerPos, Vector3 threatPos, float searchRadius)
        {
            CoverPoint best = null;
            float bestScore = float.MinValue;
            Vector3 threatDir = (threatPos - seekerPos).normalized;

            for (int i = 0; i < Points.Count; i++)
            {
                CoverPoint p = Points[i];
                if (p == null || p.IsOccupied) continue;
                Vector3 ppos = p.transform.position;
                float dSeek = Vector3.Distance(seekerPos, ppos);
                if (dSeek > searchRadius) continue;

                // کاور باید بین AI/تهدید نیفتد؛ نرمال کاور باید تقریباً به سمت تهدید باشد
                Vector3 toThreat = (threatPos - ppos).normalized;
                float facing = Vector3.Dot(p.WorldForward, toThreat);
                if (facing < 0.25f) continue;

                float dThreat = Vector3.Distance(threatPos, ppos);
                float score = -dSeek * 1.2f + dThreat * 0.6f;

                if (score > bestScore) { bestScore = score; best = p; }
            }
            return best;
        }
    }

    /// <summary>
    /// کنترلر کاور بازیکن (Smart Cover ساده و پایدار): Snap به نزدیک‌ترین کاور،
    /// حرکت محدود در امتداد مانع، Peek/تیراندازی هنگام Aim (بالای کاور یا کنار)، خروج تمیز.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class CoverController : MonoBehaviour
    {
        private CharacterController _controller;
        private PlayerController _player;
        private CoverPoint _current;

        /// <summary>در کاور است؟</summary>
        public bool IsInCover => _current != null;
        /// <summary>کاور فعلی.</summary>
        public CoverPoint CurrentCover => _current;
        /// <summary>آیا دارد از کاور بیرون می‌زند (Peek)؟</summary>
        public bool IsPeeking { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _player = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (GameplayInputSource.Current == null || _player == null || _player.Tuning == null) return;

            if (!IsInCover)
                return;

            GameplayInputSource input = GameplayInputSource.Current;

            // خروج از کاور با تلم به عقب یا فشردن مجدد
            if (input.CoverPressed || input.Move.y < -0.6f)
            {
                ExitCover();
                return;
            }

            // حرکت محدود در امتداد کاور (چپ/راست)
            Vector3 tangent = _current.WorldTangent;
            float strafe = input.Move.x;
            Vector3 move = tangent * (strafe * _player.Tuning.coverMoveSpeed);
            _controller.Move(move * Time.deltaTime + Vector3.down * 9f * Time.deltaTime);

            // Peek هنگام Aim: جابه‌جایی جزئی به سمت نرمال کاور برای خط شلیک
            IsPeeking = _player.IsAiming;
            if (IsPeeking)
            {
                Vector3 peek = transform.right * _player.Tuning.coverPeekOffset.x +
                               _current.WorldForward * _player.Tuning.coverPeekOffset.z;
                _controller.Move(peek * Time.deltaTime * 3f);
            }

            // چرخش: رو به نرمال کاور
            Quaternion targetRot = Quaternion.LookRotation(_current.WorldForward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
        }

        /// <summary>ورود به نزدیک‌ترین کاور آزاد.</summary>
        public bool TryEnterCover()
        {
            if (_player == null || _player.Tuning == null) return false;
            CoverPoint point = CoverService.FindNearestFree(transform.position, _player.Tuning.coverSnapDistance);
            if (point == null) return false;
            if (!point.TryOccupy(this)) return false;

            _current = point;
            IsPeeking = false;

            // Snap به پشت کاور
            Vector3 snapPos = point.transform.position - point.WorldForward * 0.45f;
            snapPos.y = transform.position.y;
            _player.Teleport(snapPos, Quaternion.LookRotation(point.WorldForward));
            return true;
        }

        /// <summary>خروج اجباری (مثلاً مرگ/کات‌سین).</summary>
        public void ExitCover()
        {
            if (_current == null) return;
            _current.Release(this);
            _current = null;
            IsPeeking = false;
        }
    }
}
