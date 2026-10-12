using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace FYP
{
    // one line of the history
    public class ActionRecord
    {
        public PlayerAction action;
        public int attempt;
        public float time;
    }

    public class TelemetryRecorder : MonoBehaviour
    {
        public FYPPlayerController player;
        public BossController boss;
        public AttemptManager attempts;

        // id like P07, never a real name
        public string participantId = "";
        public int historySize = 500;

        [Header("Movement detection")]
        public float moveCheckInterval = 0.5f;
        public float moveThreshold = 0.75f;

        public bool writeCsv = true;

        // one list per player, kept between attempts: the boss learns from all of them
        public List<ActionRecord> history = new List<ActionRecord>();
        public Action<ActionRecord> onActionRecorded;

        // folder + id + time, other scripts add their own file name
        public string filePrefix;

        string actionsFile;
        string attemptsFile;
        string combatFile;
        int[] actionCounts;

        Health bossHealth;

        // combat totals for the current attempt
        float damageDealt;
        float damageTaken;
        int playerHits;
        int playerMisses;
        int bossHits;
        int bossDodged;
        int bossMisses;

        bool hasLastAction;
        PlayerAction lastAction;

        bool hasLastDistance;
        float lastDistance;
        float nextMoveCheck;

        // set id, subscribe, open files
        void Awake()
        {
            if (participantId == "")
                participantId = "P" + UnityEngine.Random.Range(100000, 1000000);

            actionCounts = new int[Enum.GetNames(typeof(PlayerAction)).Length];

            bossHealth = boss.GetComponent<Health>();
            player.onActionPerformed += OnPlayerAction;

            // count a hit when the damage happens: a killing hit ends the attempt before the swing is over
            bossHealth.onDamaged += OnBossDamaged;
            player.GetComponent<Health>().onDamaged += OnPlayerDamaged;
            player.onAttackResolved += OnPlayerAttackResolved;
            boss.onAttackResolved += OnBossAttackResolved;
            attempts.onAttemptStarted += OnAttemptStarted;
            attempts.onAttemptEnded += OnAttemptEnded;

            if (writeCsv)
                CreateCsvFiles();
        }

        // new csv files with headers
        void CreateCsvFiles()
        {
            string folder = Path.Combine(Application.persistentDataPath, "FYP_Telemetry");
            Directory.CreateDirectory(folder);

            string session = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            filePrefix = Path.Combine(folder, participantId + "_" + session);
            actionsFile = filePrefix + "_actions.csv";
            attemptsFile = filePrefix + "_attempts.csv";
            combatFile = filePrefix + "_combat.csv";

            File.WriteAllText(actionsFile, "participant,attempt,time_in_attempt,action,distance_to_boss\n");
            File.WriteAllText(combatFile, "participant,attempt,time_in_attempt,attacker,attack,outcome,damage,player_hp,boss_hp,distance_to_boss\n");

            string header = "participant,attempt,result,duration_s,damage_dealt,damage_taken,player_hits,player_misses,boss_hits,boss_dodged,boss_misses,total_actions";
            foreach (string actionName in Enum.GetNames(typeof(PlayerAction)))
                header += "," + actionName;
            File.WriteAllText(attemptsFile, header + "\n");

            Debug.Log("[FYP Telemetry] Logging to " + folder);
        }

        // watch distance every frame
        void Update()
        {
            CheckMovement();
        }

        void OnPlayerAction(PlayerAction action)
        {
            // a dodge or attack moves the player, so start measuring the distance again
            hasLastDistance = false;
            Record(action);
        }

        // detect moving closer or away
        void CheckMovement()
        {
            if (!attempts.attemptActive || player.health.IsDead())
                return;

            if (player.isBusy)
            {
                hasLastDistance = false;
                return;
            }

            float distance = GetDistanceToBoss();
            if (!hasLastDistance)
            {
                lastDistance = distance;
                nextMoveCheck = Time.time + moveCheckInterval;
                hasLastDistance = true;
                return;
            }

            if (Time.time < nextMoveCheck)
                return;

            float change = distance - lastDistance;
            lastDistance = distance;
            nextMoveCheck = Time.time + moveCheckInterval;

            // one walk toward the boss = one MoveCloser. otherwise the chain fills up with the same action again and again
            if (change < -moveThreshold && !LastActionWas(PlayerAction.MoveCloser))
                Record(PlayerAction.MoveCloser);
            else if (change > moveThreshold && !LastActionWas(PlayerAction.MoveAway))
                Record(PlayerAction.MoveAway);
        }

        // same as the previous action
        bool LastActionWas(PlayerAction action)
        {
            return hasLastAction && lastAction == action;
        }

        // save one action everywhere
        void Record(PlayerAction action)
        {
            if (!attempts.attemptActive)
                return;

            ActionRecord record = new ActionRecord();
            record.action = action;
            record.attempt = attempts.attemptNumber;
            record.time = attempts.GetTimeInAttempt();

            history.Add(record);
            if (history.Count > historySize)
                history.RemoveAt(0);

            actionCounts[(int)action]++;
            lastAction = action;
            hasLastAction = true;

            if (writeCsv)
            {
                string line = participantId + "," + record.attempt + "," + ToText(record.time) + "," + action + "," + ToText(GetDistanceToBoss());
                File.AppendAllText(actionsFile, line + "\n");
            }

            if (onActionRecorded != null)
                onActionRecorded(record);
        }

        // reset counters for the new attempt
        void OnAttemptStarted(int attempt)
        {
            actionCounts = new int[actionCounts.Length];
            hasLastAction = false;
            hasLastDistance = false;

            damageDealt = 0;
            damageTaken = 0;
            playerHits = 0;
            playerMisses = 0;
            bossHits = 0;
            bossDodged = 0;
            bossMisses = 0;
        }

        // player hit the boss
        void OnBossDamaged(float damage)
        {
            if (!attempts.attemptActive)
                return;
            playerHits++;
            damageDealt += damage;
            LogCombat("Player", player.currentAction.ToString(), "Hit", damage);
        }

        // boss hit the player
        void OnPlayerDamaged(float damage)
        {
            if (!attempts.attemptActive)
                return;
            bossHits++;
            damageTaken += damage;
            string attackName = boss.currentAttack >= 0 ? boss.profile.attacks[boss.currentAttack].name : "Unknown";
            LogCombat("Boss", attackName, "Hit", damage);
        }

        // misses and dodges
        void OnPlayerAttackResolved(PlayerAction action, float damage)
        {
            if (damage > 0 || !attempts.attemptActive)
                return;
            playerMisses++;
            LogCombat("Player", action.ToString(), "Missed", 0);
        }

        // boss attack missed or dodged
        void OnBossAttackResolved(string attackName, string outcome, float damage)
        {
            if (outcome == "Hit" || !attempts.attemptActive)
                return;
            if (outcome == "Dodged")
                bossDodged++;
            else
                bossMisses++;
            LogCombat("Boss", attackName, outcome, 0);
        }

        // one line in combat csv
        void LogCombat(string attacker, string attack, string outcome, float damage)
        {
            if (!writeCsv || !attempts.attemptActive)
                return;

            string line = participantId + "," + attempts.attemptNumber + "," + ToText(attempts.GetTimeInAttempt())
                + "," + attacker + "," + attack + "," + outcome + "," + ToText(damage)
                + "," + ToText(player.health.currentHealth) + "," + ToText(bossHealth.currentHealth) + "," + ToText(GetDistanceToBoss());
            File.AppendAllText(combatFile, line + "\n");
        }

        // attempt totals to csv
        void OnAttemptEnded(int attempt, AttemptResult result, float duration)
        {
            if (!writeCsv)
                return;

            int total = 0;
            foreach (int count in actionCounts)
                total += count;

            string line = participantId + "," + attempt + "," + result + "," + ToText(duration)
                + "," + ToText(damageDealt) + "," + ToText(damageTaken)
                + "," + playerHits + "," + playerMisses + "," + bossHits + "," + bossDodged + "," + bossMisses
                + "," + total;
            foreach (int count in actionCounts)
                line += "," + count;
            File.AppendAllText(attemptsFile, line + "\n");
        }

        // flat distance, height ignored
        float GetDistanceToBoss()
        {
            Vector3 offset = player.lockTarget.position - player.transform.position;
            offset.y = 0;
            return offset.magnitude;
        }

        // always use a dot for decimals. on some windows settings it is a comma, and that breaks the csv
        public static string ToText(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
