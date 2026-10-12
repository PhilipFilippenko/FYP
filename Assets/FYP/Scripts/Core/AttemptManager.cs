using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FYP
{
    public enum AttemptResult
    {
        PlayerDied,
        BossDefeated,
        Restarted,
        Quit
    }

    public class AttemptManager : MonoBehaviour
    {
        public FYPPlayerController player;
        public Health boss;
        public float restartDelay = 2f;

        public int attemptNumber;
        public bool attemptActive;
        public float attemptStartTime;

        public Action<int> onAttemptStarted;
        public Action<int, AttemptResult, float> onAttemptEnded;

        // start positions
        Vector3 playerStartPosition;
        Quaternion playerStartRotation;
        Vector3 bossStartPosition;
        Quaternion bossStartRotation;

        // start runs after awake, to assign health properly
        void Start()
        {
            playerStartPosition = player.transform.position;
            playerStartRotation = player.transform.rotation;
            bossStartPosition = boss.transform.position;
            bossStartRotation = boss.transform.rotation;

            player.health.onDied += OnPlayerDied;
            boss.onDied += OnBossDied;

            StartAttempt();
        }

        // restart session
        void Update()
        {
            if (Keyboard.current.rKey.wasPressedThisFrame && attemptActive)
            {
                EndAttempt(AttemptResult.Restarted);
                RestartAttempt();
            }
        }

        // quit session
        void OnApplicationQuit()
        {
            if (attemptActive)
                EndAttempt(AttemptResult.Quit);
        }

        // seconds since the attempt started
        public float GetTimeInAttempt()
        {
            return Time.time - attemptStartTime;
        }

        // avoid player and boss die at the same time
        void OnPlayerDied()
        {
            if (!attemptActive)
                return;
            EndAttempt(AttemptResult.PlayerDied);
            Invoke(nameof(RestartAttempt), restartDelay);
        }

        void OnBossDied()
        {
            if (!attemptActive)
                return;
            EndAttempt(AttemptResult.BossDefeated);
            Invoke(nameof(RestartAttempt), restartDelay);
        }

        // start timer
        void StartAttempt()
        {
            attemptNumber++;
            attemptStartTime = Time.time;
            attemptActive = true;

            if (onAttemptStarted != null)
                onAttemptStarted(attemptNumber);
        }

        // stop timer, send result
        void EndAttempt(AttemptResult result)
        {
            float duration = GetTimeInAttempt();
            attemptActive = false;

            if (onAttemptEnded != null)
                onAttemptEnded(attemptNumber, result, duration);
        }

        // avoid double restart after player death
        void RestartAttempt()
        {
            CancelInvoke();
            player.ResetForAttempt(playerStartPosition, playerStartRotation);
            boss.transform.position = bossStartPosition;
            boss.transform.rotation = bossStartRotation;
            boss.ResetHealth();
            StartAttempt();
        }
    }
}
