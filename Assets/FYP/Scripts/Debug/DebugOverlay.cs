// AI assistance:
//   The table rendering (DrawTable) and the layout of the GUI panels
//   (spacing, sizes, colours) were developed with help from
//   Anthropic (2026) Claude [Large language model].
//   Available at: https://claude.ai (Accessed: 6 October 2026).
//   The panel content, debug controls and overall logic are the author's own.

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FYP
{
    // test screens. left: attempt, player, last actions. right: what the boss knows and why it picked its attack.
    // F2 hides the boss panel, F3 turns prediction off for the test without it, F4 switches adaptive and fsm
    public class DebugOverlay : MonoBehaviour
    {
        public FYPPlayerController player;
        public Health bossHealth;
        public BossController boss;
        public BossBrain brain;
        public ActionPredictor predictor;
        public AttemptManager attempts;
        public TelemetryRecorder telemetry;

        public int historyLength = 12;
        public bool showBossPanel = true;

        string[] shortNames;

        // short names from the capital letters: LightAttack = LA. new actions get one by themselves
        void Awake()
        {
            string[] names = Enum.GetNames(typeof(PlayerAction));
            shortNames = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                shortNames[i] = "";
                foreach (char letter in names[i])
                {
                    if (char.IsUpper(letter))
                        shortNames[i] += letter;
                }
            }
        }

        // debug keys
        void Update()
        {
            if (Keyboard.current.f2Key.wasPressedThisFrame)
                showBossPanel = !showBossPanel;
            if (Keyboard.current.f3Key.wasPressedThisFrame)
                predictor.predictionEnabled = !predictor.predictionEnabled;
            if (Keyboard.current.f4Key.wasPressedThisFrame)
                brain.mode = brain.mode == BossMode.Adaptive ? BossMode.StaticFSM : BossMode.Adaptive;
        }

        // draw both panels
        void OnGUI()
        {
            DrawPlayerPanel();
            if (showBossPanel && predictor.chain != null)
                DrawBossPanel();
        }

        // left panel: attempt, player, recent actions
        void DrawPlayerPanel()
        {
            GUILayout.BeginArea(new Rect(10, 10, 280, 480), GUI.skin.box);

            if (attempts.attemptActive)
                GUILayout.Label("Attempt " + attempts.attemptNumber + "   " + attempts.GetTimeInAttempt().ToString("0.0") + " s");
            else
                GUILayout.Label("Attempt " + attempts.attemptNumber + " ended, restarting...");

            GUILayout.Label("Lock-on: " + (player.isLockedOn ? "ON" : "off"));
            GUILayout.Label("Player HP: " + player.health.currentHealth.ToString("0") + " / " + player.health.maxHealth);
            GUILayout.Label("Stamina: " + player.stamina.ToString("0") + " / " + player.maxStamina + (player.isSprinting ? "  (sprint)" : ""));
            GUILayout.Label("Boss HP: " + bossHealth.currentHealth.ToString("0") + " / " + bossHealth.maxHealth);

            GUILayout.Space(6);
            GUILayout.Label("Participant " + telemetry.participantId + ", " + telemetry.history.Count + " actions recorded");
            GUILayout.Label("Recent actions (newest last):");
            int start = Mathf.Max(0, telemetry.history.Count - historyLength);
            for (int i = start; i < telemetry.history.Count; i++)
            {
                ActionRecord record = telemetry.history[i];
                GUILayout.Label("  A" + record.attempt + "  " + record.time.ToString("0.0") + "s  " + record.action);
            }

            GUILayout.Space(6);
            GUILayout.Label("Shift: sprint   MMB: lock-on   R: restart   Esc: cursor");
            GUILayout.EndArea();
        }

        // right panel: predictions, tables, utilities
        void DrawBossPanel()
        {
            string[] actionNames = Enum.GetNames(typeof(PlayerAction));
            GUILayout.BeginArea(new Rect(Screen.width - 470, 10, 460, 900), GUI.skin.box);

            GUILayout.Label("BOSS BRAIN   prediction: " + (predictor.predictionEnabled ? "ON" : "OFF (ablation)") + "   mode: " + brain.mode);
            GUILayout.Label("Observed actions: " + predictor.observedCount + "   decay: " + predictor.decay);
            GUILayout.Label("Last action: " + (predictor.hasLastAction ? predictor.lastAction.ToString() : "none (new attempt)"));

            GUILayout.Space(6);
            float chainDetail = predictor.hasLastAction ? predictor.GetDetailWeight(predictor.chain.RowTotal((int)predictor.lastAction)) * 100 : 0;
            GUILayout.Label("Chain: predicted next action (detail " + chainDetail.ToString("0") + "%)");
            float[] prediction = predictor.GetPrediction();
            int best = ActionPredictor.MostLikely(prediction);
            for (int i = 0; i < prediction.Length; i++)
            {
                GUI.color = i == best ? Color.yellow : Color.white;
                DrawBar(actionNames[i], 110, (prediction[i] * 100).ToString("0") + "%", prediction[i]);
            }
            GUI.color = Color.white;

            GUILayout.Space(6);
            GUILayout.Label("Chain table (rows = last action, columns = next):");
            // yellow row = the one the guess is read from
            DrawTable(predictor.chain, shortNames, predictor.hasLastAction ? (int)predictor.lastAction : -1);

            GUILayout.Space(6);
            string attackName = boss.currentAttack >= 0 ? brain.profile.attacks[boss.currentAttack].name : "-";
            GUILayout.Label("Boss state: " + boss.state + "   attack: " + attackName + "   last decision: " + brain.lastReason);
            for (int i = 0; i < brain.utilities.Length; i++)
            {
                GUI.color = i == brain.lastChoice ? Color.yellow : Color.white;
                // detail % = how much the per-action table counts for this attack
                float detail = predictor.GetDetailWeight(predictor.answers.RowTotal(i)) * 100;
                string label = brain.profile.attacks[i].name + " (" + predictor.GetAnswerSource(i) + ", detail " + detail.ToString("0") + "%)";
                DrawBar(label, 200, brain.utilities[i].ToString("0.00"), brain.utilities[i]);
            }
            GUI.color = Color.white;

            GUILayout.Space(6);
            GUILayout.Label("Answer table (rows = boss attack, columns = player answer):");
            string[] attackNames = new string[brain.profile.attacks.Length];
            for (int i = 0; i < attackNames.Length; i++)
                attackNames[i] = brain.profile.attacks[i].name;
            // yellow row = the attack the boss is doing now
            DrawTable(predictor.answers, attackNames, boss.currentAttack);

            GUILayout.Space(6);
            GUILayout.Label("F2: hide   F3: prediction on/off   F4: adaptive / static FSM");
            GUILayout.EndArea();
        }

        // one labelled bar
        void DrawBar(string label, float labelWidth, string valueText, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(labelWidth));
            GUILayout.Label(valueText, GUILayout.Width(45));
            GUILayout.Box("", GUILayout.Width(Mathf.Max(2, value * 180)), GUILayout.Height(14));
            GUILayout.EndHorizontal();
        }

        // count table as a grid
        void DrawTable(CountTable table, string[] rowNames, int highlightRow)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(50));
            for (int c = 0; c < table.columns; c++)
                GUILayout.Label(shortNames[c], GUILayout.Width(38));
            GUILayout.EndHorizontal();

            for (int r = 0; r < table.rows; r++)
            {
                GUI.color = r == highlightRow ? Color.yellow : Color.white;
                GUILayout.BeginHorizontal();
                GUILayout.Label(rowNames[r], GUILayout.Width(50));
                for (int c = 0; c < table.columns; c++)
                {
                    float value = table.counts[r, c];
                    GUILayout.Label(value < 0.05f ? "." : value.ToString("0.0"), GUILayout.Width(38));
                }
                GUILayout.EndHorizontal();
            }
            GUI.color = Color.white;
        }
    }
}
