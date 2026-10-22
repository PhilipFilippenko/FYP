<!-- generated from FYP_Project.docx, edit the .docx instead -->

# Adaptive Boss AI: Markov Chain-Based Player Behaviour Modelling for Personalised Combat Encounters in Action Games

Philip Filippenko, T00259086

BSc (Hons) in Computing, Munster Technological University

## Abstract

[Abstract: to be written last. One paragraph on the problem, the system, the evaluation and the main results.]

Table of Contents

## Glossary

- Ablation: a test with one part of the system switched off, to measure what that part adds.

- Adaptivity setting: a value from 0 to 1 that controls how strongly an enemy counters the player.

- Finite state machine (FSM): an AI that switches between fixed states by fixed rules.

- Invulnerability frames (i-frames): the part of a dodge in which the player cannot be hurt.

- Markov chain: a model that gives the probability of the next action from the current one.

- PSX style: a low-resolution look based on early PlayStation games.

- Response table: a table of how one player answers each boss attack.

- Utility score: a number that rates how good a boss attack is in the current situation.

## 1. Introduction

### 1.1 Background and Motivation

Boss fights are a crucial element of action games, and almost every boss follows a fixed, pre-designed pattern. After several tries, the player memorises this pattern and defeats the boss because they know it, rather than through their own decision-making during the fight. The player can also skip this learning and watch walkthrough videos of the boss fight, so the only thing they need to do is repeat a solution found by another person, e.g. a certain spot where the player can hide from the boss's attacks. Thus, in both scenarios the boss fight ends with one solution that any player can easily copy. As a consequence, the boss stops feeling like a real opponent and the fight itself becomes repetitive.

This project tries to create a boss that can adapt its behaviour to the particular player, so that the fight cannot be reduced to one universal solution. The system tracks the player's actions during the fight, predicts their next action based on the history of their actions, and chooses a counteraction. The second mechanic deals with players who exploit the game environment. If the player repeatedly uses a certain spot for hiding or attacking, the boss destroys this spot at the beginning of the next attempt. Thus, the player can see that the boss reacts to their tactics.

The wider goal is a reusable system for adaptive enemies. It takes a Markov model of the player as its base and extends it with a table of how the player answers each attack, back-off to action categories when data is short, and counter values computed from the attacks themselves. The same system should be able to drive a boss or an ordinary enemy, with a setting that controls how strongly the enemy adapts. The target is an enemy that reacts to the player and stays fair and interesting to fight.

The prototype is created in Unity as a boss encounter in a PSX-style action game.

### 1.2 Problem Statement

There are two problems behind the need for this work. The first problem is that a boss controlled by a fixed behaviour tree or finite state machine makes the same decisions for every player in a particular situation. This means that a solution found by any one player will be equally helpful for everyone who adopts it. The second problem is that when the level design is fixed, an exploit such as a spot the boss cannot reach can make part of the fight trivial once it is found, and the game has no way to respond to it.

### 1.3 Research Question and Hypothesis

Research question: How much would an adaptive boss that selects its moves based on a Markov model of each player's gameplay behaviour, together with observable changes to the environment when a player keeps exploiting it, decrease the transferability of strategies between players and increase the differences between individual strategies, compared to a static finite state machine boss of similar overall difficulty? Here, similar overall difficulty means identical structural parameters (health, damage, attack timings and hit areas), so that action selection is the only independent variable.

Hypothesis: In comparison with a static finite state machine boss, a boss that uses a Markov model for each player will show a statistically significant difference in (a) the number of attempts a playtester needs to beat the boss when using a strategy learned from another player's playthrough, and (b) the variety of action sequences used against the boss by different playtesters.

### 1.4 Aims and Objectives

The aim of this project is to build a reusable adaptive enemy system based on a Markov model of each player, and to test it on a boss fight. The boss should feel like an opponent that reacts to the player, while staying fair and interesting to fight.

- Build a boss encounter prototype in Unity in a PSX visual style.

- Log the player's combat actions during the fight with a telemetry engine.

- Predict the player's next action with a Markov chain that is built live for each player, without a pre-existing dataset.

- Choose the boss's counteraction with a utility-based scoring function.

- Make the system reusable for other enemies through data assets and one adaptivity setting, and show this on one simple enemy.

- At the start of the next attempt, destroy a spot that the player keeps exploiting.

- Compare the adaptive boss with a static finite state machine boss, first with scripted bots and then with 10 to 15 playtesters.

### 1.5 Main Research Areas

- Player and opponent modelling

- Markov chains and routinisation in games

- Adaptive game AI

- Decision making in game AI

- Exploits and level design

- Evaluation of game AI with players

## 2. Literature Review

### 2.1 Player and Opponent Modelling

Developing a model of the player based on their in-game actions, so-called player modelling or opponent modelling, is a well-researched subject in game AI. Bakkes, Spronck and van Lankveld (2012) introduce four different methods of player behaviour modelling (action, tactic, strategy and player profile modelling) and show how they can be applied to adaptive game AI. Previously, Bakkes, Spronck and van den Herik (2009) showed that incorporating opponent modelling into adaptive AI results in significantly higher efficiency of that AI in a real-time strategy game.

Yannakakis, Spronck, Loiacono and André (2013) give an overview of this field. They define player modelling as the study of computational models of players in games. They also separate models by their input: data from gameplay, from the player's body (for example physiological signals) and from the game context. This project uses gameplay data only, so every input to the model is an action the player takes during the fight.

Gray, Zhu, Arigo, Forman and Ontañón (2020) use multi-armed bandits for player modelling rather than full machine learning models, mainly because it is difficult to gather enough training data about a particular player during a single play session. The current project faces the same issue. Lastly, Obsidian, an undergraduate project developed at Worcester Polytechnic Institute in 2011, created an adaptive boss fight in which the boss uses AI that learns during play. It shows that an adaptive boss can be implemented in an undergraduate project, but it does not combine per-player Markov prediction with visible changes to the environment, which is the focus of this project.

### 2.2 Markov Chains and Routinisation

The study of Wallner, Pichlmair, Hecher and Wimmer (2015) is the closest to this project. They use Markov chains and information theory to model routinisation, i.e. the process in which a player forms repeatable and predictable behaviour patterns through practice. The study supports the core concept of this project: the more a player plays, the more predictable their behaviour becomes, and a Markov chain can model this without the large number of samples required by most other machine learning techniques.

### 2.3 Adaptive Game AI

Adaptive game AI has also been built without a model of the individual player. Hunicke (2005) argues for dynamic difficulty adjustment, where the game changes values such as the supply of health and ammunition when the player struggles. This changes how hard the game is, but not how the enemy decides. Spronck, Ponsen, Sprinkhuizen-Kuyper and Postma (2006) went further with dynamic scripting, an online learning method in which enemies change the rules they use during play, based on how well those rules worked. They also added a way to scale the AI's difficulty to the player's skill. Both approaches adapt to how well the player is doing, while this project adapts to what the player is likely to do next.

### 2.4 Decision Making in Game AI

[To be written: finite state machines and behaviour trees in boss design, and utility-based decision making.]

### 2.5 Exploits and Level Design

[To be written: how players exploit fixed levels and how games respond to it.]

### 2.6 Evaluating Game AI with Players

[To be written: within-subjects design, order effects, and the variety metrics (Shannon entropy, Levenshtein distance).]

## 3. Methodology

### 3.1 Development Lifecycle

The project uses a hybrid model. The module phases (Plan, Specification, Formal Design and Submission 3) have fixed dates, so this part runs in order, like Waterfall. Day-to-day work runs on Scrumban, an Agile method that mixes Scrum and Kanban.

- All tasks sit on one Kanban board: Backlog, To Do, In Progress, Testing, Done.

- Work is split into two-week sprints. Each sprint has one goal and ends with a working increment that can be tested.

- At most three tasks are in progress at once, so work does not spread too thin.

- At the end of each sprint the board is reviewed and the backlog is re-ordered.

Waterfall does not fit, because the boss AI has to be tuned after testing, and in Waterfall testing comes at the end. Pure Scrum is built for teams, with a Product Owner, a Scrum Master and daily stand-ups, which make no sense in a solo project. Scrumban sits in between.

Two things are frozen early: the scripted test bots and the counter-value table (section 4.7). This way later sprints cannot tune them in favour of the adaptive boss.

### 3.2 Offline Bot Tests

Offline bot tests take place before any human playtesting. The bots are scripted and represent different play styles, for example an aggressive close-range bot and a cautious bot that keeps its distance. At this stage, two checks are conducted. The first is a determinism check, which confirms that, with a fixed random seed, the same input always produces the same sequence of boss decisions. The second is an ablation check, where the full adaptive boss system is compared to the same system without the predictive layer, so that it works as a static boss, using the same bots as input. This shows how much difference the adaptive layer actually makes before any playtesters are involved.

### 3.3 Human Playtesting

Human testing will use a counterbalanced within-subjects design, where every playtester fights both the adaptive boss and a static finite state machine boss of the same difficulty level. All playtesters will be split into two groups: one group fights the static boss first and the adaptive boss second, while in the other group the order is reversed. This takes care of the possible learning or warm-up effect between the two fights. The sample will be a convenience sample of friends and classmates. Only people who have played action or souls-like games before will be included, as the hypothesis is about experienced players. The target sample is 10 to 15 playtesters. To measure strategy transfer, a playtester watches a recording of another playtester's successful attempt against the same boss version and then tries to repeat it; the number of attempts and the survival time are compared between the static and the adaptive boss.

All metrics are behavioural and will be recorded automatically by the telemetry engine rather than collected through questionnaires: number of attempts to win, survival time per attempt, frequency of each action type, and whether the player tries to use a destroyed exploit again after it has been removed. The results of both versions will be compared using a paired t-test, or a Wilcoxon signed-rank test if the data is not close to normally distributed, at a significance level of α = 0.05. Research ethics approval for the human testing phase will be requested as early as possible, and the requirements will be checked with the module coordinator before any playtesters are recruited. Variety of strategies is measured with the Shannon entropy of each player's action sequence and the Levenshtein distance between the action sequences of different players. Telemetry uses anonymous participant IDs; no names or personal data are stored, and participants agree to take part before playing.

### 3.4 Threats to Validity

Because the same person builds the boss and tests it, the following controls are used against experimenter bias. The test bots are scripted with fixed rules and frozen (tagged in version control) before the predictive layer is tuned, and are never changed to make the boss look better. The counter values in the decision layer are derived from a fixed rule before any testing (based on how each attack's hit area covers the position a player action leads to) and stay the same for every player. All playtesters receive the same written briefing, and the researcher stays a passive observer and gives no hints. Finally, a third baseline, a boss that only counts how often each action occurs without using the sequence, shows whether the Markov sequence itself is what improves the predictions.

### 3.5 Scope and Limitations

Exploitable objects are marked manually by the designer and are not detected automatically by the environment response subsystem. This keeps the subsystem achievable within the project timeline, but it means that it cannot work automatically on any level layout. The Markov model is limited to first or second order (over the nine actions in section 4.2), so that it can learn from the limited data of a single playtesting session. Finally, the playtesters are a convenience sample, not a random sample, and this limitation will be stated clearly when the results are discussed.

Reuse is shown on one extra enemy type in the same game, not across different games. The project does not measure whether the system speeds up game development; that would need a study with other developers.

### 3.6 Risk Analysis

Table 1 lists the main risks, how each one is prevented, and what is done if it happens anyway.

Table 1. Risk analysis

| Risk | Likelihood | Impact | Prevention | Action plan |
| --- | --- | --- | --- | --- |
| Time goes into the game and art instead of the AI | High | High | AI tasks always go first; art only after the AI works | Use placeholder models in the demo |
| Base AI not ready by 7 Dec | Medium | High | A basic Markov block already works | Improvements wait for Semester 2 |
| The model does not adapt well | Medium | Medium | Bot tests (ablation check) show this early | A negative result still answers the research question |
| Not enough playtesters | Medium | High | Start finding testers early (classmates, online groups) | Each player tests both versions |
| Ethics approval is late | Medium | High | Apply early and check the rules with the module coordinator | Run the bot tests first and move the playtests later |
| A Unity or package update breaks the project | Low | High | Lock one Unity version for the year | Roll back to the last working commit |
| Losing work or a broken build | Low | High | Git and GitHub, push after every session | Restore the last working commit |
| Other modules take time | Medium | Medium | Check the board every sprint | Move tasks to the next sprint |

## 4. Design

### 4.1 System Overview

The system is divided into separate subsystems, so that the decision-making subsystem can later be reused by other bosses without rewriting it.

There are four subsystems. The telemetry engine records each player action. The predictive layer turns this history into a guess of what the player will do next, and of how they will answer each boss attack. The decision layer scores the boss attacks with that guess and picks one. The environment response subsystem works between attempts and removes cover that the player keeps using. Telemetry feeds the predictive layer and the predictive layer feeds the decision layer, while the environment response subsystem counts cover use on its own. Each part can be tested separately.

The prototype is built in Unity 6 (6000.3) with the Universal Render Pipeline and the new Input System, and all code is written in C#. Per-boss data is stored in ScriptableObject assets. The project is kept in Git, with a repository on GitHub.

### 4.2 Telemetry Engine

The telemetry engine logs the player's combat actions performed during the fight in a history buffer for that particular player. Each action belongs to a small list of predefined action types.

The current vocabulary has nine actions: LightAttack, HeavyAttack, DodgeLeft, DodgeRight, DodgeBack, DodgeForward, MoveCloser, MoveAway and UseCover. They describe what the player does, not which key they press. Dodge directions are measured relative to the boss, so they mean the same with and without lock-on.

Two dodges were added during development. A dodge with no direction is a backstep and is now logged as DodgeBack. A roll toward the boss used to be logged as DodgeRight; it is now DodgeForward. MoveCloser and MoveAway come from the change in distance to the boss, checked every 0.5 s with a threshold of 0.75 m. A long walk toward the boss is logged only once; otherwise the chain would fill up with the same action. UseCover will be logged once the cover objects exist.

Each session writes four CSV files: every player action, a summary of every attempt, every hit, miss and dodge, and every boss decision with the score of each attack.

### 4.3 Predictive Layer

A low-order Markov chain is constructed from the history of the player's actions and estimates how likely each action is to be the player's next one, depending on their most recent actions. The model is constantly updated during the session and does not require any pre-existing dataset, which avoids the data problem highlighted by Gray et al. (2020). The chain is first-order. Older observations are weighted down with a decay factor (0.9 per new observation in the same row), so the model follows a change of strategy instead of being held back by the start of the fight. To avoid a large and sparse transition matrix, the model keeps two levels: detailed actions and five abstract categories (Attack, Side dodge, Retreat, Approach, Defend). Predictions blend both levels with a weight of n / (n + 3), where n is the amount of data for the current state: early in the fight the categories carry the prediction, and as data accumulates the detailed actions take over (back-off).

Response table. In addition to the chain, the boss keeps a table of how this player responds to each of its attacks (the first player action after an attack starts). In the developer's own test sessions the plain chain predicted the player's response at the moment of the boss's decision only 9% of the time, below chance (11%), because players react to the boss's wind-up rather than continue their own sequence; with the response table, correct predictions rose to 31 to 47% of decisions. The decision layer therefore scores each boss attack by the predicted response to that attack. These figures come from an early, unfinished prototype and only show the direction; final numbers will come from the bot tests and playtests.

### 4.4 Decision Layer

A utility-based scoring function determines the boss's next action. The scoring function takes into account both the predicted action of the player and the counter value of each boss attack (section 4.7), so that the boss favours counteractions to the player's most likely next action rather than just following a script.

The boss only scores attacks that can reach the player. The first version checked the distance only. In one test session 72% of boss attacks missed, because the player stood behind the boss, where frontal attacks cannot hit. A player close to the boss can circle it at about 140 degrees per second, while the boss turns at 45 degrees per second during a windup. The final rule allows a frontal attack only if the player is inside its angle minus a 20 degree margin. After this change the boss hit rate in the developer's test sessions went from 25% to 38%.

In 10% of decisions the boss picks a random attack that can reach the player, so it is not fully predictable. All random choices come from one generator with a fixed seed, which makes every test run repeatable.

The same decision code runs the four boss conditions of the experiment. All four use the same attacks, damage and timings:

- Adaptive: the full system.

- Ablation: prediction switched off, so every answer looks equally likely.

- Frequency: the boss only counts how often each action happens, not the order.

- Static FSM: the boss uses the reachable attacks in a fixed order and ignores the player.

### 4.5 Environment Response

Several objects in the arena (cover spots, high ground) are marked by the designer, and each object counts how many times the player uses it during an attempt. If this number exceeds a certain threshold, the object is flagged. At the beginning of the player's next attempt, before the fight starts, the boss performs a short animation directed at this object, and the object's model is replaced with a pre-made destroyed version. This replacement does not involve any physics calculations and has a negligible impact on performance.

### 4.6 Boss Profile and Attacks

All data for one boss is kept in a BossProfile asset: the attacks, their timings and hit areas, the counter values and the movement speeds. The same code can run another boss with a new profile. Each hit area is a piece of a ring around the boss, set by an inner radius, an outer radius and a half angle. The placeholder boss, Knell, is about 10 m tall against a 1.8 m player, so the ranges are large.

Table 2. Attacks of the boss Knell

| Attack | Hit area | Damage | Windup (s) | Active (s) | Recovery (s) |
| --- | --- | --- | --- | --- | --- |
| Slam | Narrow line ahead, 3 to 10 m, 20 degrees each side | 35 | 1.0 | 0.15 | 1.2 |
| Sweep | Wide arc, 2 to 9 m, 100 degrees each side | 25 | 0.8 | 0.2 | 1.0 |
| Stomp | Full circle up to 5.5 m | 20 | 0.6 | 0.15 | 0.9 |
| Toll | Full ring from 9 to 22 m | 20 | 1.2 | 0.2 | 1.4 |

Between attacks the boss pauses for 0.6 s. This is the moment the player can hit back. The boss walks at 3 m/s and turns at 90 degrees per second, or 45 during a windup.

Known issue: Slam is almost never chosen, because its half angle (20 degrees) is the same as the safety margin of the reach check. This is a balance question and will be settled before the test bots are frozen.

### 4.7 Counter-Value Table

The counter value of an attack against a player action says how well the attack punishes that action, from 0 to 1. The first values were set by hand, but hand-set values could be tuned in favour of the adaptive boss, and some of them did not match the game. For example, Toll had a value of 0 against attacks, although a player who attacks inside its ring is always hit.

The values now come from one fixed rule, set before any testing. For each attack, the rule takes a grid of start positions where the boss would choose that attack. The player starts the action between 0.2 and 0.5 s after the windup begins, since players react at different speeds. The counter value is the share of these cases in which the player is hit. A small simulation uses the game's own numbers: the player walks at 5 m/s, a dodge covers 4 m in 0.6 s with 0.25 s of invulnerability, the boss turns 45 degrees per second during the windup, and a swing that was dodged cannot hit later.

Table 3. Counter values from the fixed rule (frozen on 22 October 2026)

| Attack | Light | Heavy | Dodge side | Dodge back | Dodge forward | Move closer | Move away |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Slam | 1.00 | 1.00 | 0.79 | 0.52 | 0.52 | 0.60 | 0.60 |
| Sweep | 1.00 | 1.00 | 0.98 | 0.65 | 0.65 | 0.75 | 0.75 |
| Stomp | 1.00 | 1.00 | 0.57 | 0.35 | 0.57 | 1.00 | 0.81 |
| Toll | 1.00 | 1.00 | 1.00 | 0.75 | 0.75 | 0.71 | 0.71 |

A player who attacks stands still inside the hit area, so every boss attack punishes it fully. The choice of attack therefore depends on how the player moves. Stomp has a short windup: a dodge is often still invulnerable when it lands. Toll has the longest windup and punishes players who dodge too early. UseCover keeps its first values until the cover objects exist.

### 4.8 Reuse and the Adaptivity Setting

The predictive layer models the player, not the enemy. One model per player can therefore serve every enemy in the fight, and each enemy adds data to it, so it learns faster than a model per enemy would.

Everything specific to one enemy sits in its profile asset: attacks, timings, hit areas and movement. The counter values do not have to be set by hand, because the rule in section 4.7 computes them from the attack data. A new enemy therefore needs a new profile, not new code.

A planned adaptivity setting, from 0 to 1, will control how often an enemy follows the best counter instead of its own default pattern. A boss can run at 1, while a common enemy can run close to 0 and stay simple. The same setting gives a way to tune difficulty without changing the attacks.

Some rules keep the boss fair whatever the setting: every attack has a visible windup, the boss pauses after each attack so the player can hit back, attacks are only chosen when they can actually reach the player, and 10% of choices are random. The current counter rule only covers melee hit areas around the enemy; ranged attacks would need an extension of the rule.

### 4.9 Class Structure

[To be added: a class diagram of the main scripts.]

## 5. Conclusion and Next Steps

[To be written. Future work: other enemy types, ranged attacks, an editor tool for the counter rule, and a player test of different adaptivity levels.]

## References

Bakkes, S.C.J., Spronck, P.H.M. and van den Herik, H.J. (2009) 'Opponent modelling for case-based adaptive game AI', Entertainment Computing, 1(1), pp. 27–37.

Bakkes, S.C.J., Spronck, P.H.M. and van Lankveld, G. (2012) 'Player behavioural modelling for video games', Entertainment Computing, 3(3), pp. 71–79.

Gray, R.C., Zhu, J., Arigo, D., Forman, E. and Ontañón, S. (2020) 'Player Modeling via Multi-Armed Bandits', in Proceedings of the 15th International Conference on the Foundations of Digital Games (FDG '20). Bugibba, Malta: ACM, pp. 1–8. Available at: https://arxiv.org/abs/2102.05264.

Hunicke, R. (2005) 'The case for dynamic difficulty adjustment in games', in Proceedings of the 2005 ACM SIGCHI International Conference on Advances in Computer Entertainment Technology (ACE '05). Valencia: ACM, pp. 429–433. Available at: https://doi.org/10.1145/1178477.1178573.

Spronck, P., Ponsen, M., Sprinkhuizen-Kuyper, I. and Postma, E. (2006) 'Adaptive game AI with dynamic scripting', Machine Learning, 63(3), pp. 217–248. Available at: https://doi.org/10.1007/s10994-006-6205-6.

Wallner, S., Pichlmair, M., Hecher, M. and Wimmer, M. (2015) 'Modeling Routinization in Games: An Information Theory Approach', in Proceedings of the 2015 Annual Symposium on Computer-Human Interaction in Play (CHI PLAY '15). London: ACM, pp. 727–732. Available at: https://doi.org/10.1145/2793107.2810286.

Worcester Polytechnic Institute (2011) Obsidian: Development of a Multiplayer Game with Adaptive Enemy Intelligence. Major Qualifying Project. Worcester Polytechnic Institute. Available at: https://digitalcommons.wpi.edu/mqp-all/2786/ (Accessed: 14 October 2026).

Yannakakis, G.N., Spronck, P., Loiacono, D. and André, E. (2013) 'Player Modeling', in Lucas, S.M., Mateas, M., Preuss, M., Spronck, P. and Togelius, J. (eds) Artificial and Computational Intelligence in Games. Dagstuhl Follow-Ups, 6. Wadern: Schloss Dagstuhl, pp. 45–59. Available at: https://doi.org/10.4230/DFU.Vol6.12191.45.
