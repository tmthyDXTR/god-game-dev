using System;
using System.Collections;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Centralized tick manager that drives all tick-based game systems.
    /// Other managers (ResourceTickManager, agents, etc.) subscribe to OnTick.
    /// </summary>
    public class GlobalTickManager : MonoBehaviour
    {
        public static GlobalTickManager Instance { get; private set; }

        [Header("Tick Settings")]
        [Tooltip("Seconds between global ticks")]
        public float tickInterval = 2f;

        [Tooltip("If true, ticks run automatically. If false, call TriggerTick() manually (turn-based).")]
        public bool autoTick = true;

        /// <summary>
        /// Fired at the start of each tick. Subscribe for deterministic game logic.
        /// </summary>
        public event Action OnTick;

        /// <summary>
        /// Current tick number since game start.
        /// </summary>
        public int CurrentTick { get; private set; } = 0;

        /// <summary>
        /// Time elapsed since the last tick (0 to tickInterval). Useful for interpolation.
        /// </summary>
        public float TimeSinceLastTick { get; private set; } = 0f;

        /// <summary>
        /// Normalized progress through current tick (0 to 1). Useful for lerping.
        /// </summary>
        public float TickProgress => Mathf.Clamp01(TimeSinceLastTick / Mathf.Max(0.001f, tickInterval));

        private Coroutine tickLoop;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            if (!Application.isPlaying) return;
            if (autoTick)
                tickLoop = StartCoroutine(TickLoop());
        }

        void Update()
        {
            // Track time since last tick for interpolation
            TimeSinceLastTick += Time.deltaTime;
        }

        /// <summary>
        /// Trigger a single tick immediately (for turn-based or manual control).
        /// </summary>
        public void TriggerTick()
        {
            ExecuteTick();
        }

        /// <summary>
        /// Enable automatic ticking.
        /// </summary>
        public void EnableAutoTick()
        {
            autoTick = true;
            if (tickLoop == null && Application.isPlaying)
                tickLoop = StartCoroutine(TickLoop());
        }

        /// <summary>
        /// Disable automatic ticking.
        /// </summary>
        public void DisableAutoTick()
        {
            autoTick = false;
            if (tickLoop != null)
            {
                StopCoroutine(tickLoop);
                tickLoop = null;
            }
        }

        IEnumerator TickLoop()
        {
            while (Application.isPlaying)
            {
                yield return new WaitForSeconds(tickInterval);
                ExecuteTick();
            }
        }

        void ExecuteTick()
        {
            CurrentTick++;
            TimeSinceLastTick = 0f;
            try
            {
                OnTick?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void OnDisable()
        {
            if (tickLoop != null)
                StopCoroutine(tickLoop);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
