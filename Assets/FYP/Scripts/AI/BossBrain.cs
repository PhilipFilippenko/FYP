using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FYP
{
    // adaptive boss or simpler bosses to compare with. new modes go at the end, the scene stores the number
    public enum BossMode
    {
        Adaptive,
        StaticFSM,
        Frequency
    }

    // picks the next boss attack.
    // adaptive: score of an attack = how likely each answer is x how well the attack punishes it, added up.
    // frequency: same scoring, but the guess is only how often each action happens, without the order.
    // shows whether the markov sequence is what makes the difference.
    // static fsm: same attacks in a fixed order, does not look at the player. used to compare
    public class BossBrain : MonoBehaviour
    {
        public BossMode mode = BossMode.Adaptive;
        public BossProfile profile;
        public ActionPredictor predictor;
        public TelemetryRecorder telemetry;
        public AttemptManager attempts;

        // all random choices use this one generator with a fixed seed, so a run can be repeated exactly
        public int randomSeed = 12345;

        // sometimes pick a random attack, so the boss is not too easy to read
        [Range(0f, 1f)]
        public float explorationChance = 0.1f;

        // a player right next to the boss can run around it faster than the boss turns,
        // so a front attack is only picked when the player is clearly in front
        public float angleSafetyMargin = 20f;

        [Header("Last decision (read only)")]
        public float[] utilities;
        public int lastChoice = -1;
        public string lastReason = "";

        System.Random random;
        int fsmStep;
        string decisionsFile;

        // random with a fixed seed, empty scores
        void Awake()
        {
            random = new System.Random(randomSeed);
            utilities = new float[profile.attacks.Length];
        }

        // start, not awake: telemetry makes filePrefix in its awake
        void Start()
        {
            if (!telemetry.writeCsv)
                return;

            decisionsFile = telemetry.filePrefix + "_boss_decisions.csv";
            string header = "participant,attempt,time_in_attempt,mode,prediction_on,distance,angle,chain_predicted_action,chain_probability,chosen_attack,reason,expected_answer,answer_probability,answer_source";
            foreach (BossAttack attack in profile.attacks)
                header += ",utility_" + attack.name;
            File.WriteAllText(decisionsFile, header + "\n");
        }

        // fsm starts its order again
        public void ResetForAttempt()
        {
            fsmStep = 0;
            lastChoice = -1;
            lastReason = "";
        }

        // angle: 0 = player in front, 180 = player right behind
        bool CanReach(BossAttack attack, float distance, float angle)
        {
            if (distance < attack.innerRadius || distance > attack.outerRadius)
                return false;
            if (attack.halfAngle >= 180)
                return true;
            return angle <= attack.halfAngle - angleSafetyMargin;
        }

        // returns the chosen attack, or -1 if no attack can reach the player
        public int ChooseAttack(float distance, float angle)
        {
            List<int> usable = new List<int>();
            for (int i = 0; i < profile.attacks.Length; i++)
            {
                if (CanReach(profile.attacks[i], distance, angle))
                    usable.Add(i);
            }
            if (usable.Count == 0)
                return -1;

            // score each attack by how this player usually answers it
            for (int i = 0; i < profile.attacks.Length; i++)
            {
                float[] answer = GetGuess(i);
                utilities[i] = 0;
                for (int a = 0; a < answer.Length; a++)
                    utilities[i] += answer[a] * profile.attacks[i].counters.Get((PlayerAction)a);
            }

            int choice;
            if (mode == BossMode.StaticFSM)
            {
                choice = usable[fsmStep % usable.Count];
                fsmStep++;
                lastReason = "fsm cycle";
            }
            else
            {
                choice = usable[0];
                foreach (int i in usable)
                {
                    if (utilities[i] > utilities[choice])
                        choice = i;
                }
                lastReason = "best utility";

                // always take both random numbers, so the random order stays the same between runs
                double roll = random.NextDouble();
                int randomPick = usable[random.Next(usable.Count)];
                if (roll < explorationChance)
                {
                    choice = randomPick;
                    lastReason = "exploration";
                }
            }

            lastChoice = choice;
            LogDecision(distance, angle, choice);
            return choice;
        }

        // what the boss expects the player to do if it uses this attack
        float[] GetGuess(int attackIndex)
        {
            if (mode == BossMode.Frequency)
                return predictor.GetFrequencyPrediction();
            return predictor.GetAnswerPrediction(attackIndex);
        }

        // both guesses go to the log, so the report can compare them
        void LogDecision(float distance, float angle, int choice)
        {
            if (decisionsFile == null)
                return;

            float[] chain = predictor.GetPrediction();
            int predicted = ActionPredictor.MostLikely(chain);
            float[] answer = GetGuess(choice);
            string source = mode == BossMode.Frequency && predictor.predictionEnabled ? "frequency" : predictor.GetAnswerSource(choice);
            int expected = ActionPredictor.MostLikely(answer);

            string line = telemetry.participantId + "," + attempts.attemptNumber + "," + TelemetryRecorder.ToText(attempts.GetTimeInAttempt())
                + "," + mode + "," + predictor.predictionEnabled + "," + TelemetryRecorder.ToText(distance) + "," + TelemetryRecorder.ToText(angle)
                + "," + (PlayerAction)predicted + "," + TelemetryRecorder.ToText(chain[predicted])
                + "," + profile.attacks[choice].name + "," + lastReason
                + "," + (PlayerAction)expected + "," + TelemetryRecorder.ToText(answer[expected]) + "," + source;
            foreach (float utility in utilities)
                line += "," + TelemetryRecorder.ToText(utility);
            File.AppendAllText(decisionsFile, line + "\n");
        }
    }
}
