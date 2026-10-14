<!-- generated from FYP_Project.docx, edit the .docx instead -->

# Adaptive Boss AI: Markov Chain-Based Player Behaviour Modelling for Personalised Combat Encounters in Action Games

Philip Filippenko, T00259086

BSc (Hons) in Computing, Munster Technological University

## 1. Introduction

Boss fights are a crucial element of action games, and almost every boss follows a fixed, pre-designed pattern. After several tries, the player memorises this pattern and defeats the boss because they know it, rather than through their own decision-making during the fight. The player can also skip this learning and watch walkthrough videos of the boss fight, so the only thing they need to do is repeat a solution found by another person, e.g. a certain spot where the player can hide from the boss's attacks. Thus, in both scenarios the boss fight ends with one solution that any player can easily copy. As a consequence, the boss stops feeling like a real opponent and the fight itself becomes repetitive.

This project tries to create a boss that can adapt its behaviour to the particular player, so that the fight cannot be reduced to one universal solution. The system tracks the player's actions during the fight, predicts their next action based on the history of their actions, and chooses a counteraction. The second mechanic deals with players who exploit the game environment. If the player repeatedly uses a certain spot for hiding or attacking, the boss destroys this spot at the beginning of the next attempt. Thus, the player can see that the boss reacts to their tactics.

The prototype is created in Unity as a boss encounter in a PSX-style action game.

## 2. Background and Related Work

Developing a model of the player based on their in-game actions, so-called player modelling or opponent modelling, is a well-researched subject in game AI. Bakkes, Spronck and van Lankveld (2012) introduce four different methods of player behaviour modelling (action, tactic, strategy and player profile modelling) and show how they can be applied to adaptive game AI. Previously, Bakkes, Spronck and van den Herik (2009) showed that incorporating opponent modelling into adaptive AI results in significantly higher efficiency of that AI in a real-time strategy game.

Yannakakis, Spronck, Loiacono and André (2013) give an overview of this field. They define player modelling as the study of computational models of players in games. They also separate models by their input: data from gameplay, from the player's body (for example physiological signals) and from the game context. This project uses gameplay data only, so every input to the model is an action the player takes during the fight.

The study of Wallner, Pichlmair, Hecher and Wimmer (2015) is more relevant to this particular project. They use Markov chains and information theory to model routinisation, i.e. the process in which a player forms repeatable and predictable behaviour patterns through practice. The study supports the core concept of this project: the more a player plays, the more predictable their behaviour becomes, and a Markov chain can model this without the large number of samples required by most other machine learning techniques.

Gray, Zhu, Arigo, Forman and Ontañón (2020) use multi-armed bandits for player modelling rather than full machine learning models, mainly because it is difficult to gather enough training data about a particular player during a single play session. The current project faces the same issue. Lastly, Obsidian, an undergraduate project developed at Worcester Polytechnic Institute in 2011, created an adaptive boss fight in which the boss uses AI that learns during play. It shows that an adaptive boss can be implemented in an undergraduate project, but it does not combine per-player Markov prediction with visible changes to the environment, which is the focus of this project.

Adaptive game AI has also been built without a model of the individual player. Hunicke (2005) argues for dynamic difficulty adjustment, where the game changes values such as the supply of health and ammunition when the player struggles. This changes how hard the game is, but not how the enemy decides. Spronck, Ponsen, Sprinkhuizen-Kuyper and Postma (2006) went further with dynamic scripting, an online learning method in which enemies change the rules they use during play, based on how well those rules worked. They also added a way to scale the AI's difficulty to the player's skill. Both approaches adapt to how well the player is doing, while this project adapts to what the player is likely to do next.

## 3. Problem Statement

There are two problems behind the need for this work. The first problem is that a boss controlled by a fixed behaviour tree or finite state machine makes the same decisions for every player in a particular situation. This means that a solution found by any one player will be equally helpful for everyone who adopts it. The second problem is that when the level design is fixed, an exploit such as a spot the boss cannot reach can make part of the fight trivial once it is found, and the game has no way to respond to it.

## 4. Research Question and Hypothesis

Research question: How much would an adaptive boss that selects its moves based on a Markov model of each player's gameplay behaviour, together with observable changes to the environment when a player keeps exploiting it, decrease the transferability of strategies between players and increase the differences between individual strategies, compared to a static finite state machine boss of similar overall difficulty? Here, similar overall difficulty means identical structural parameters (health, damage, attack timings and hit areas), so that action selection is the only independent variable.

Hypothesis: In comparison with a static finite state machine boss, a boss that uses a Markov model for each player will show a statistically significant difference in (a) the number of attempts a playtester needs to beat the boss when using a strategy learned from another player's playthrough, and (b) the variety of action sequences used against the boss by different playtesters. To make sure that this difference does not simply come from the adaptive boss being stronger, both bosses are identical in their health, attacks and damage values, and only differ in action selection.

## 5. Proposed System Architecture

The system is divided into separate subsystems, so that the decision-making subsystem can later be reused by other bosses without rewriting it.

Telemetry engine. Logs the player's combat actions performed during the fight in a history buffer for that particular player. Each action belongs to a small list of predefined action types (for instance: light attack, heavy attack, dodge left, dodge right, move closer, move back, use a particular piece of cover).

Predictive layer. A low-order Markov chain is constructed from the history of the player's actions and estimates how likely each action is to be the player's next one, depending on their most recent actions. The model is constantly updated during the session and does not require any pre-existing dataset, which avoids the data problem highlighted by Gray et al. (2020). The chain is first-order. Older observations are weighted down with a decay factor (0.9 per new observation in the same row), so the model follows a change of strategy instead of being held back by the start of the fight. To avoid a large and sparse transition matrix, the model keeps two levels: detailed actions and five abstract categories (Attack, Side dodge, Retreat, Approach, Defend). Predictions blend both levels with a weight of n / (n + 3), where n is the amount of data for the current state: early in the fight the categories carry the prediction, and as data accumulates the detailed actions take over (back-off).

Response model. In addition to the chain, the boss keeps a table of how this player responds to each of its attacks (the first player action after an attack starts). In the developer's own test sessions the plain chain predicted the player's response at the moment of the boss's decision only 9% of the time, below chance (11%), because players react to the boss's wind-up rather than continue their own sequence; with the response table, correct predictions rose to 31 to 47% of decisions. The decision layer therefore scores each boss attack by the predicted response to that attack. These figures come from an early, unfinished prototype and only show the direction; final numbers will come from the bot tests and playtests.

Decision layer. A utility-based scoring function determines the boss's next action. The scoring function takes into account both the predicted action of the player and a counter value assigned to each boss action by the designer, so that the boss favours counteractions to the player's most likely next action rather than just following a script.

Environment response subsystem. Several objects in the arena (cover spots, high ground) are marked by the designer, and each object counts how many times the player uses it during an attempt. If this number exceeds a certain threshold, the object is flagged. At the beginning of the player's next attempt, before the fight starts, the boss performs a short animation directed at this object, and the object's model is replaced with a pre-made destroyed version. This replacement does not involve any physics calculations and has a negligible impact on performance.

## 6. Evaluation Methodology

Offline bot tests take place before any human playtesting. The bots are scripted and represent different play styles, for example an aggressive close-range bot and a cautious bot that keeps its distance. At this stage, two checks are conducted. The first is a determinism check, which confirms that, with a fixed random seed, the same input always produces the same sequence of boss decisions. The second is an ablation check, where the full adaptive boss system is compared to the same system without the predictive layer, so that it works as a static boss, using the same bots as input. This shows how much difference the adaptive layer actually makes before any playtesters are involved.

Human testing will use a counterbalanced within-subjects design, where every playtester fights both the adaptive boss and a static finite state machine boss of the same difficulty level. All playtesters will be split into two groups: one group fights the static boss first and the adaptive boss second, while in the other group the order is reversed. This takes care of the possible learning or warm-up effect between the two fights. The sample will be a convenience sample of friends and classmates. Only people who have played action or souls-like games before will be included, as the hypothesis is about experienced players. One extra condition will test strategy transfer directly: the playtester will be given a written or recorded strategy from another playtester's session against the same boss, and their results will be compared between the static and adaptive versions. The target sample is 10 to 15 playtesters. To measure strategy transfer, a playtester watches a recording of another playtester's successful attempt against the same boss version and then tries to repeat it; the number of attempts and the survival time are compared between the static and the adaptive boss.

All metrics are behavioural and will be recorded automatically by the telemetry engine rather than collected through questionnaires: number of attempts to win, survival time per attempt, frequency of each action type, and whether the player tries to use a destroyed exploit again after it has been removed. The results of both versions will be compared using a paired t-test, or a Wilcoxon signed-rank test if the data is not close to normally distributed, at a significance level of α = 0.05. Research ethics approval for the human testing phase will be requested as early as possible, and the requirements will be checked with the module coordinator before any playtesters are recruited. Variety of strategies is measured with the Shannon entropy of each player's action sequence and the Levenshtein distance between the action sequences of different players. Telemetry uses anonymous participant IDs; no names or personal data are stored, and participants agree to take part before playing.

### 6.1 Threats to Validity

Because the same person builds the boss and tests it, the following controls are used against experimenter bias. The test bots are scripted with fixed rules and frozen (tagged in version control) before the predictive layer is tuned, and are never changed to make the boss look better. The counter values in the decision layer are derived from a fixed rule before any testing (based on how each attack's hit area covers the position a player action leads to) and stay the same for every player. All playtesters receive the same written briefing, and the researcher stays a passive observer and gives no hints. Finally, a third baseline, a boss that only counts how often each action occurs without using the sequence, shows whether the Markov sequence itself is what improves the predictions.

## 7. Scope and Limitations

Exploitable objects are marked manually by the designer and are not detected automatically by the environment response subsystem. This keeps the subsystem achievable within the project timeline, but it means that it cannot work automatically on any level layout. The Markov model is limited to first or second order (over the list of actions above), so that it can learn from the limited data of a single playtesting session. Finally, the playtesters are a convenience sample, not a random sample, and this limitation will be stated clearly when the results are discussed.

## 8. Project Plan

Work for this phase (FYP Design) will follow the milestones of the module: a project plan, including the first part of the literature review and the scope of work; a project specification based on the architecture outlined above; and a formal design that explains the four subsystems (telemetry, predictive, decision and environment response). The offline bot tests (determinism and ablation checks) are scheduled before the formal design milestone, to make sure the decision system works before human playtesting in the second phase of the project.

## References

Bakkes, S.C.J., Spronck, P.H.M. and van den Herik, H.J. (2009) 'Opponent modelling for case-based adaptive game AI', Entertainment Computing, 1(1), pp. 27–37.

Bakkes, S.C.J., Spronck, P.H.M. and van Lankveld, G. (2012) 'Player behavioural modelling for video games', Entertainment Computing, 3(3), pp. 71–79.

Gray, R.C., Zhu, J., Arigo, D., Forman, E. and Ontañón, S. (2020) 'Player Modeling via Multi-Armed Bandits', in Proceedings of the 15th International Conference on the Foundations of Digital Games (FDG '20). Bugibba, Malta: ACM, pp. 1–8. Available at: https://arxiv.org/abs/2102.05264.

Hunicke, R. (2005) 'The case for dynamic difficulty adjustment in games', in Proceedings of the 2005 ACM SIGCHI International Conference on Advances in Computer Entertainment Technology (ACE '05). Valencia: ACM, pp. 429–433. Available at: https://doi.org/10.1145/1178477.1178573.

Spronck, P., Ponsen, M., Sprinkhuizen-Kuyper, I. and Postma, E. (2006) 'Adaptive game AI with dynamic scripting', Machine Learning, 63(3), pp. 217–248. Available at: https://doi.org/10.1007/s10994-006-6205-6.

Wallner, S., Pichlmair, M., Hecher, M. and Wimmer, M. (2015) 'Modeling Routinization in Games: An Information Theory Approach', in Proceedings of the 2015 Annual Symposium on Computer-Human Interaction in Play (CHI PLAY '15). London: ACM, pp. 727–732. Available at: https://doi.org/10.1145/2793107.2810286.

Worcester Polytechnic Institute (2011) Obsidian: Development of a Multiplayer Game with Adaptive Enemy Intelligence. Major Qualifying Project. Worcester Polytechnic Institute. Available at: https://digitalcommons.wpi.edu/mqp-all/2786/ (Accessed: 14 October 2026).

Yannakakis, G.N., Spronck, P., Loiacono, D. and André, E. (2013) 'Player Modeling', in Lucas, S.M., Mateas, M., Preuss, M., Spronck, P. and Togelius, J. (eds) Artificial and Computational Intelligence in Games. Dagstuhl Follow-Ups, 6. Wadern: Schloss Dagstuhl, pp. 45–59. Available at: https://doi.org/10.4230/DFU.Vol6.12191.45.
