using System;
using UnityEngine;

namespace FYP
{
    // guesses what this player will do next. every table is kept twice: per action and per group
    //      chain: what the player does after their last action
    //      answers: what the player does when the boss starts an attack (the boss uses this one)
    // at the start of a fight the group table does most of the work,
    // the more the player plays, the more the per-action table counts
    public class ActionPredictor : MonoBehaviour
    {
        public TelemetryRecorder telemetry;
        public AttemptManager attempts;
        public BossController boss;

        [Range(0.5f, 1f)]
        public float decay = 0.9f;
        public float smoothing = 0.1f;

        // bigger number = trust the per-action table later
        public float backoffStrength = 3f;

        // with fewer answers than this, use the chain instead
        public float minAnswerSamples = 3f;

        // test switch: when off the boss knows nothing and every action is equally likely
        public bool predictionEnabled = true;

        public CountTable chain;
        public CountTable chainByCategory;
        public CountTable answers;
        public CountTable answersByCategory;
        // how often each action happens, to split a group back into actions
        public CountTable overall;

        public bool hasLastAction;
        public PlayerAction lastAction;
        public int observedCount;

        int actionCount;
        int categoryCount;
        int waitingForAnswerTo = -1;

        // create tables, subscribe
        void Awake()
        {
            actionCount = Enum.GetNames(typeof(PlayerAction)).Length;
            categoryCount = Enum.GetNames(typeof(ActionCategory)).Length;
            int attackCount = boss.profile.attacks.Length;

            chain = new CountTable(actionCount, actionCount, decay, smoothing);
            chainByCategory = new CountTable(categoryCount, categoryCount, decay, smoothing);
            answers = new CountTable(attackCount, actionCount, decay, smoothing);
            answersByCategory = new CountTable(attackCount, categoryCount, decay, smoothing);
            overall = new CountTable(1, actionCount, decay, smoothing);

            telemetry.onActionRecorded += OnActionRecorded;
            attempts.onAttemptStarted += OnAttemptStarted;
            boss.onAttackStarted += OnBossAttackStarted;
            boss.onAttackResolved += OnBossAttackResolved;
        }

        // learn from every new action
        void OnActionRecorded(ActionRecord record)
        {
            int action = (int)record.action;
            int category = (int)ActionCategories.Of(record.action);

            if (hasLastAction)
            {
                chain.Add((int)lastAction, action);
                chainByCategory.Add((int)ActionCategories.Of(lastAction), category);
            }
            overall.Add(0, action);
            lastAction = record.action;
            hasLastAction = true;
            observedCount++;

            // the first action after the boss starts an attack is the answer to it
            if (waitingForAnswerTo >= 0)
            {
                answers.Add(waitingForAnswerTo, action);
                answersByCategory.Add(waitingForAnswerTo, category);
                waitingForAnswerTo = -1;
            }
        }

        // a new attempt does not continue the last one
        void OnAttemptStarted(int attempt)
        {
            hasLastAction = false;
            waitingForAnswerTo = -1;
        }

        // wait for the player's answer
        void OnBossAttackStarted(int attackIndex)
        {
            waitingForAnswerTo = attackIndex;
        }

        // actions after the hit do not count as an answer
        void OnBossAttackResolved(string attackName, string outcome, float damage)
        {
            waitingForAnswerTo = -1;
        }

        // chain: what comes after the last action
        public float[] GetPrediction()
        {
            if (!predictionEnabled || !hasLastAction)
                return Uniform();

            int row = (int)lastAction;
            int categoryRow = (int)ActionCategories.Of(lastAction);
            return Blend(chain.Predict(row), chain.RowTotal(row), chainByCategory.Predict(categoryRow));
        }

        // enough data for this attack
        public bool HasEnoughAnswers(int attackIndex)
        {
            return answersByCategory.RowTotal(attackIndex) >= minAnswerSamples;
        }

        // answers: how the player reacts to this attack
        public float[] GetAnswerPrediction(int attackIndex)
        {
            if (!predictionEnabled)
                return Uniform();
            if (!HasEnoughAnswers(attackIndex))
                return GetPrediction();
            return Blend(answers.Predict(attackIndex), answers.RowTotal(attackIndex), answersByCategory.Predict(attackIndex));
        }

        // which table was used, for logs
        public string GetAnswerSource(int attackIndex)
        {
            if (!predictionEnabled)
                return "off";
            return HasEnoughAnswers(attackIndex) ? "answers" : "chain";
        }

        // how much to trust the per-action table
        public float GetDetailWeight(float samples)
        {
            return samples / (samples + backoffStrength);
        }

        // mix the per-action guess with the group guess.
        // a group's chance is split between its actions by how often each one is used
        float[] Blend(float[] detailed, float detailedSamples, float[] byCategory)
        {
            float weight = GetDetailWeight(detailedSamples);
            float[] share = overall.Predict(0);

            float[] categoryTotal = new float[categoryCount];
            for (int a = 0; a < actionCount; a++)
                categoryTotal[(int)ActionCategories.Of((PlayerAction)a)] += share[a];

            float[] result = new float[actionCount];
            for (int a = 0; a < actionCount; a++)
            {
                int category = (int)ActionCategories.Of((PlayerAction)a);
                float fromCategory = byCategory[category] * share[a] / categoryTotal[category];
                result[a] = weight * detailed[a] + (1 - weight) * fromCategory;
            }
            return result;
        }

        // index of the highest chance
        public static int MostLikely(float[] probabilities)
        {
            int best = 0;
            for (int i = 1; i < probabilities.Length; i++)
            {
                if (probabilities[i] > probabilities[best])
                    best = i;
            }
            return best;
        }

        // every action equally likely
        float[] Uniform()
        {
            float[] uniform = new float[actionCount];
            for (int i = 0; i < actionCount; i++)
                uniform[i] = 1f / actionCount;
            return uniform;
        }
    }
}
