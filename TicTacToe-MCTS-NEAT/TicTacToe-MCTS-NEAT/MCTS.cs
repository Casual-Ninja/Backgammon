using GAME;
using HelperSpace;
using System.Collections.Generic;
using System.Threading;

namespace MCTS
{
    public class MCTSNode
    {
        private State state;
        private MCTSNode parent;
        private MCTSNode[] children;

        private int numberOfVisits;
        private int score;

        private List<Action> untriedActions;
        private System.Random rnd;

        private int threadingSimulationCount;
        private float threadingSimulationTime;

        public const float CHyperParamDefault = 1.414213f;

        private float cHyperParam;

        public static float CHyperParam = 1.414213f;

        public State GetState() { return this.state; }

        public MCTSNode(State state, float cHyperParam)
        {
            this.state = state;
            this.cHyperParam = cHyperParam;
        }

        public MCTSNode(State state, MCTSNode parent)
        {
            this.state = state;
            this.parent = parent;
            this.cHyperParam = parent.cHyperParam;


            this.numberOfVisits = 0;
            this.score = 0;
            this.untriedActions = GetLegalActions();
            this.children = new MCTSNode[untriedActions.Count];
        }

        public List<GAME.Action> GetLegalActions()
        {
            return this.state.GetLegalActions(parent.state);
        }

        /// <summary>
        /// Gets one random child from the untried actions and adds it to children array.
        /// </summary>
        /// <param name="rnd">The random class to use.</param>
        /// <returns>Returns the new child that was added</returns>
        private MCTSNode Expand(System.Random rnd)
        {
            // get random action and remove it from list
            int index = HelperMethods.GetRandomIndexFromList(this.untriedActions, rnd);
            Action action = untriedActions[index];
            untriedActions.RemoveAt(index);

            // Get the resulting state of the action picked
            State nextState = this.state.Move(parent.state, action);
            // create a new node for that state
            MCTSNode childNode = new MCTSNode(nextState, this);

            // add the new node to the children array in the correct postion.
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] == null)
                {
                    if (index == 0)
                    {
                        children[i] = childNode;
                        break;
                    }
                    index--;
                }
            }

            return childNode; // return the new node
        }

        private bool IsTerminalNode()
        {
            return this.state.IsGameOver();
        }

        /// <summary>
        /// Does the "Simulation" part of the algorithm.
        /// </summary>
        /// <param name="rnd">The random class to use.</param>
        /// <returns>Returns the game result of the simulation.</returns>
        private int RollOut(System.Random rnd)
        {
            State parentState = this.parent.state;
            State rollOutState = this.state;
            int mult = -1;
            while (rollOutState.IsGameOver() == false) // while i haven't reached end of game
            {
                // pick an action (soft play == random moves)
                //Action action = RolloutPolicySoftPlay(rollOutState.GetLegalActions(parentState), rollOutState, rnd);

                // pick an action (hard play == calculated moves)
                Action action = RolloutPolicyHardPlay(rollOutState.GetLegalActions(parentState), rollOutState, parentState, rnd);
                

                State temp = rollOutState;

                // go to the next state
                rollOutState = rollOutState.Move(parentState, action);
                parentState = temp;

                // i only want to change sign of result every two steps, as you can think of
                // every pair of chance and choice states being one overall part of a turn.
                // so every two states do i want to "flip" the result to reflect the correct player.
                if (rollOutState.IsChanceState())
                    mult = -mult;
            }
            // game result is not relative to who played, and so need to multiply by mult
            return rollOutState.GameResult() * mult;
        }

        /// <summary>
        /// Does the "Simulation" part of the algorithm.
        /// </summary>
        /// <param name="parentState">Parent of the starting state.</param>
        /// <param name="rollOutState">the state to start from.</param>
        /// <param name="rnd">The random class to use.</param>
        /// <returns>Returns the game result of the simulation.</returns>
        private int RollOut(State parentState, State rollOutState, int mult, System.Random rnd)
        {
            while (rollOutState.IsGameOver() == false) // while i haven't reached end of game
            {
                // pick an action (soft play == random moves)
                //Action action = RolloutPolicySoftPlay(rollOutState.GetLegalActions(parentState), rollOutState, rnd);

                // pick an action (hard play == calculated moves)
                Action action = RolloutPolicyHardPlay(rollOutState.GetLegalActions(parentState), rollOutState, parentState, rnd);


                State temp = rollOutState;

                // go to the next state
                rollOutState = rollOutState.Move(parentState, action);
                parentState = temp;

                // i only want to change sign of result every two steps, as you can think of
                // every pair of chance and choice states being one overall part of a turn.
                // so every two states do i want to "flip" the result to reflect the correct player.
                if (rollOutState.IsChanceState())
                    mult = -mult;
            }
            // game result is not relative to who played, and so need to multiply by mult
            return rollOutState.GameResult() * mult;
        }

        /// <summary>
        /// Does the "Simulation" part of the algorithm only for depth 2.
        /// </summary>
        /// <param name="rnd">The random class to use.</param>
        /// <returns>Returns the game result of the simulation.</returns>
        private (State parentState, State rollOutState, int mult, bool finishedEarly) RollOutDepth2(System.Random rnd)
        {
            State parentState = this.parent.state;
            State rollOutState = this.state;
            int mult = -1;
            int depth = 0;
            while (rollOutState.IsGameOver() == false && depth < 2) // while i haven't reached end of game
            {
                // pick an action (soft play == random moves)
                //Action action = RolloutPolicySoftPlay(rollOutState.GetLegalActions(parentState), rollOutState, rnd);

                // pick an action (hard play == calculated moves)
                Action action = RolloutPolicyHardPlay(rollOutState.GetLegalActions(parentState), rollOutState, parentState, rnd);


                State temp = rollOutState;

                // go to the next state
                rollOutState = rollOutState.Move(parentState, action);
                parentState = temp;

                // i only want to change sign of result every two steps, as you can think of
                // every pair of chance and choice states being one overall part of a turn.
                // so every two states do i want to "flip" the result to reflect the correct player.
                if (rollOutState.IsChanceState())
                    mult = -mult;

                depth++;
            }
            // game result is not relative to who played, and so need to multiply by mult
            return (parentState, rollOutState, mult, depth < 2);
        }

        /// <summary>
        /// Back propagates the result back up the tree.
        /// </summary>
        /// <param name="result">The result of simulation relative to who won.</param>
        private void BackPropagate(int result)
        {
            this.numberOfVisits += 1;
            this.score += System.Math.Max(0, result);

            if (parent != null) // if i didn't reach the top
            {
                // i only want to change sign of result every two steps, as you can think of
                // every pair of chance and choice states being one overall part of a turn.
                // so every two states do i want to "flip" the result to reflect the correct player.
                // (just like i did in rollout)
                if (state.IsChanceState())
                    parent.BackPropagate(-result);
                else
                    parent.BackPropagate(result);
            }
        }

        private bool IsFullyExpanded()
        {
            return untriedActions.Count == 0;
        }

        /// <summary>
        /// Calculates the value of the node using UCT.
        /// </summary>
        /// <param name="child">The node to calculate its value.</param>
        /// <returns>Returns UCT of the node.</returns>
        private float CalculateUCT(MCTSNode child)
        {
            return ((float)child.score / child.numberOfVisits) +  // exploitation part
                   cHyperParam * (float)System.Math.Sqrt(System.Math.Log(numberOfVisits) / child.numberOfVisits); // exploration part
        }

        /// <summary>
        /// Returns the child to pick in Selection part of MCTS algorithm
        /// </summary>
        /// <param name="rnd">The random class to use.</param>
        /// <returns>If child is a die, returns the child randomly. 
        /// otherwise, returns child with highest UCT score.</returns>
        private MCTSNode BestChild(System.Random rnd)
        {
            // pick a random starting best child (picks the die randomly based on chance of the die happening)
            MCTSNode bestChild = children[state.RandomPick(children.Length, rnd)];

            if (children[0].state.IsChanceState()) // if its chance state, return the random child as its the dice
                return bestChild;

            float bestScore = CalculateUCT(bestChild); // calculate UCT for the current best child

            // go over all children, and find the one with highest UCT score
            for (int i = 0; i < this.children.Length; i++)
            {
                MCTSNode child = children[i];
                float score = CalculateUCT(child);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestChild = child;
                }
            }

            return bestChild;
        }

        /// <summary>
        /// Returns the Action to pick in Simulation part of MCTS algorithm 
        /// </summary>
        /// <param name="possibleMoves">The possible moves allowed from this state.</param>
        /// <param name="state">The state to move from.</param>
        /// <param name="rnd">The random class to use.</param>
        /// <returns>Returns the random action picked.</returns>
        private Action RolloutPolicySoftPlay(List<Action> possibleMoves, State state, System.Random rnd)
        {
            // pick a random action.
            // if its for picking moves, its uniformally random.
            // if its for picking die, its based on the chance of that die happening.
            return state.RandomPick(possibleMoves, rnd);
        }

        private Action RolloutPolicyHardPlay(List<Action> possibleMoves, State state, State prevState, System.Random rnd)
        {
            if (state.IsChanceState() == false) // if its chance state still pick randomly
                return state.RandomPick(possibleMoves, rnd);

            // if its not chance state, choose state corresponding to score
            float[] scores = new float[possibleMoves.Count];
            float sumOfScores = 0;
            for (int i = 0; i < possibleMoves.Count; i++)
            {
                scores[i] = possibleMoves[i].GetScore(state, prevState);
                sumOfScores += scores[i];
            }
            float randomValue = HelperMethods.RandomValue(0, sumOfScores, rnd);
            for (int i = 0; i < scores.Length - 1; i++)
            {
                randomValue -= scores[i];
                if (randomValue <= 0)
                    return possibleMoves[i];
            }
            return possibleMoves[possibleMoves.Count - 1];
        }

        /// <summary>
        /// Selection and Expansion Part of MCTS algorithm.
        /// </summary>
        /// <param name="rnd">The random class to use.</param>
        /// <returns>A node that is either terminal or not fully expanded.</returns>
        private MCTSNode TreePolicy(System.Random rnd)
        {
            MCTSNode currentNode = this;
            
            // go down tree untill hitting a terminal node
            while (currentNode.IsTerminalNode() == false)
            {
                if (currentNode.IsFullyExpanded() == false) // if its not fully expanded, expand it.
                    return currentNode.Expand(rnd);
                else
                    currentNode = currentNode.BestChild(rnd); // return best child, using the UCB formula
            }
            return currentNode; // return current node
        }

        public MCTSNode BestAction(int simulationCount)
        {
            System.Random rnd = new System.Random();
            for (int i = 0; i < simulationCount; i++)
            {
                MCTSNode v = TreePolicy(rnd);
                v.BackPropagate(v.RollOut(rnd));
            }

            MCTSNode bestChild = children[0];
            for (int i = 1; i < children.Length; i++)
            {
                if (children[i].numberOfVisits > bestChild.numberOfVisits)
                    bestChild = children[i];
            }

            System.Console.WriteLine($"{bestChild.score} / {bestChild.numberOfVisits} == " +
                                     $"{(float)bestChild.score / bestChild.numberOfVisits}");

            return bestChild;
        }

        public MCTSNode BestAction(int simulationCount, int seed)
        {
            System.Random rnd = new System.Random(seed);
            for (int i = 0; i < simulationCount; i++)
            {
                MCTSNode v = TreePolicy(rnd);
                v.BackPropagate(v.RollOut(rnd));
            }

            MCTSNode bestChild = children[0];
            for (int i = 1; i < children.Length; i++)
            {
                if (children[i].numberOfVisits > bestChild.numberOfVisits)
                    bestChild = children[i];
            }

            return bestChild;
        }

        private void CalculateTreeNumbers()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            long timeSpentInTree = 0;
            long timeSpentInRollOut = 0;

            for (int i = 0; i < threadingSimulationCount; i++)
            {
                sw.Restart();
                MCTSNode v = TreePolicy(rnd);
                sw.Stop();
                timeSpentInTree += sw.ElapsedMilliseconds;

                sw.Restart();
                int rollOutAnswer = v.RollOut(rnd);
                timeSpentInRollOut += sw.ElapsedMilliseconds;

                sw.Restart();
                v.BackPropagate(rollOutAnswer);
                sw.Stop();
                timeSpentInTree += sw.ElapsedMilliseconds;
            }

            System.Console.WriteLine($"Time spent in Tree {timeSpentInTree}, Time spent in Roll out: {timeSpentInRollOut}");
            System.Console.WriteLine($"Precentage of time spent in Tree: {(double)timeSpentInTree / (timeSpentInTree + timeSpentInRollOut)}");
        }

        /// <summary>
        /// Runs the MCTS algorithm for a specified amount of time.
        /// </summary>
        private void CalculateTreeNumbersWithTime()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            int count = 0;
            while (sw.ElapsedMilliseconds < threadingSimulationTime)
            {
                MCTSNode v = TreePolicy(rnd);
                v.BackPropagate(v.RollOut(rnd));
                count++;
            }
            sw.Stop();
            System.Console.WriteLine("Rollouts: " + count);
        }

        public MCTSNode BestActionMultiThreading(int simulationCount, int threadCount, params int[] seeds)
        {
            Thread[] threads = new Thread[threadCount - 1];
            MCTSNode[] workedOnNodes = new MCTSNode[threadCount - 1];

            for (int i = 0; i < threadCount - 1; i++)
            {
                MCTSNode newParentNode = new MCTSNode(parent.state.Copy(), MCTSNode.CHyperParam);
                MCTSNode newStartNode = new MCTSNode(state.Copy(), newParentNode);
                newStartNode.threadingSimulationCount = simulationCount;
                int seed = seeds.Length > i ? seeds[i] : HelperMethods.GetRandomSeed();
                newStartNode.rnd = new System.Random(seed);

                workedOnNodes[i] = newStartNode;
                Thread newThread = new Thread(new ThreadStart(newStartNode.CalculateTreeNumbers));
                threads[i] = newThread;
                newThread.Start();
            }

            threadingSimulationCount = simulationCount;
            int seed2 = seeds.Length > threadCount - 1 ? seeds[threadCount - 1] : HelperMethods.GetRandomSeed();
            rnd = new System.Random(seed2);

            int[] oldVisits = null;

            if (children != null)
            {
                oldVisits = new int[children.Length];
                for (int i = 0; i < children.Length; i++)
                    oldVisits[i] = children[i] == null ? 0 : children[i].numberOfVisits;
            }

            System.Console.WriteLine("Created the threads");

            CalculateTreeNumbers();

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            System.Console.WriteLine("All threads finished");

            foreach (MCTSNode tree in workedOnNodes)
            {
                //AddValuesOfWholeTree(tree);
                AddValuesOfFirstBranches(tree);
            }

            MCTSNode bestChild = children[0];
            int totalVisits = 0;
            int newVisits = 0;

            for (int i = 0; i < children.Length; i++)
            {
                totalVisits += children[i].numberOfVisits;
                if (oldVisits == null)
                    newVisits += children[i].numberOfVisits;
                else
                    newVisits += children[i].numberOfVisits - oldVisits[i];

                if (children[i].numberOfVisits > bestChild.numberOfVisits)
                    bestChild = children[i];
            }

            System.Console.WriteLine("Total: " + totalVisits + " New Visits: " + newVisits);

            System.Console.WriteLine("Best Wins: " + bestChild.score + " Best Visits: " + bestChild.numberOfVisits);
            return bestChild;
        }

        /// <summary>
        /// Calculates the best move using the MCTS algorithm in a set amount of time and threads.
        /// </summary>
        /// <param name="maxTime">The max amount of time allowed to calculate in milliseconds.</param>
        /// <param name="threadCount">The amount of threads to use.</param>
        /// <returns>Returns the next node that ai thought will be the best next state.</returns>
        public MCTSNode BestActionInTimeMultiThreading(float maxTime, int threadCount)
        {
            Thread[] threads = new Thread[threadCount - 1];
            MCTSNode[] workedOnNodes = new MCTSNode[threadCount - 1];

            // start all the threads
            for (int i = 0; i < threadCount - 1; i++)
            {
                // make a copy of the needed nodes
                MCTSNode newParentNode = new MCTSNode(parent.state.Copy(), MCTSNode.CHyperParam);
                MCTSNode newStartNode = new MCTSNode(state.Copy(), newParentNode);
                // set the parameters for the thread (max time of search and rnd to use)
                newStartNode.threadingSimulationTime = maxTime;
                newStartNode.rnd = new System.Random();

                // start the thread, and remember it
                workedOnNodes[i] = newStartNode;
                Thread newThread = new Thread(new ThreadStart(newStartNode.CalculateTreeNumbersWithTime));
                threads[i] = newThread;
                newThread.Start();
            }

            
            threadingSimulationTime = maxTime;
            rnd = new System.Random();

            System.Console.WriteLine("Created the threads");

            // calculate in this thread as well
            CalculateTreeNumbersWithTime();

            // wait for everyone to finish
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            System.Console.WriteLine("All threads finished");

            // add up the values from all the threads
            foreach (MCTSNode tree in workedOnNodes)
            {
                AddValuesOfFirstBranches(tree);
            }

            // find the best child
            MCTSNode bestChild = children[0];
            
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].numberOfVisits > bestChild.numberOfVisits)
                    bestChild = children[i];
            }

            System.Console.WriteLine("Best Wins: " + bestChild.score + " Best Visits: " + bestChild.numberOfVisits);
            return bestChild;
        }

        public int GetIndexOfChild(MCTSNode child)
        {
            if (children == null)
                return -1;

            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].Equals(child))
                    return i;
            }

            return -1;
        }

        private void AddValuesOfFirstBranches(MCTSNode otherTree)
        {
            this.score += otherTree.score;
            this.numberOfVisits += otherTree.numberOfVisits;
            for (int i = 0; i < otherTree.children.Length; i++)
            {
                if (otherTree.children[i] != null)
                {
                    if (children[i] == null)
                    {
                        // create the child
                        int count = 0;
                        for (int j = 0; j < i; j++)
                        {
                            if (children[j] != null)
                                count++;
                        }
                        int index = i - count;

                        State nextState = this.state.Move(parent.state, untriedActions[index]);
                        children[i] = new MCTSNode(nextState, this);

                        untriedActions.RemoveAt(index);
                    }

                    children[i].score += otherTree.children[i].score;
                    children[i].numberOfVisits += otherTree.children[i].numberOfVisits;
                }
            }
        }

        private void AddValuesOfWholeTree(MCTSNode otherTree)
        {
            this.score += otherTree.score;
            this.numberOfVisits += otherTree.numberOfVisits;
            for (int i = 0; i < otherTree.children.Length; i++)
            {
                if (otherTree.children[i] != null)
                {
                    if (children[i] == null)
                    {
                        // create the child
                        int count = 0;
                        for (int j = 0; j < i; j++)
                        {
                            if (children[j] != null)
                                count++;
                        }
                        int index = i - count;

                        State nextState = this.state.Move(parent.state, untriedActions[index]);
                        children[i] = new MCTSNode(nextState, this);

                        untriedActions.RemoveAt(index);
                    }
                    children[i].AddValuesOfWholeTree(otherTree.children[i]);
                }
            }
        }

        // --- MCTS Tree lock MULTITHREAD ---

        private void CalculateValuesGlobalLock(object rndObj)
        {
            System.Random rnd = (System.Random)rndObj;

            System.Console.WriteLine("Calculating using global mutex lock");
            for (int i = 0; i < threadingSimulationCount; i++)
            {
                MCTSNode endNode;

                (State, State, int, bool) values;

                lock (this)
                {
                    endNode = TreePolicy(rnd);

                    values = endNode.RollOutDepth2(rnd);

                    if (values.Item4)
                    {
                        endNode.BackPropagate(values.Item2.GameResult() * values.Item3);
                        continue;
                    }
                }

                int result = RollOut(values.Item1, values.Item2, values.Item3, rnd);

                lock (this)
                {
                    endNode.BackPropagate(result);
                }
            }
        }

        private void CalculateValuesGlobalLockInTime(object rndObj)
        {
            System.Random rnd = (System.Random)rndObj;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            int count = 0;
            while (sw.ElapsedMilliseconds < threadingSimulationTime)
            {
                MCTSNode endNode;

                (State, State, int, bool) values;

                lock (this)
                {
                    endNode = TreePolicy(rnd);

                    values = endNode.RollOutDepth2(rnd);

                    if (values.Item4)
                    {
                        endNode.BackPropagate(values.Item2.GameResult() * values.Item3);
                        continue;
                    }
                }

                int result = RollOut(values.Item1, values.Item2, values.Item3, rnd);

                lock (this)
                {
                    endNode.BackPropagate(result);
                }

                count++;
            }
            sw.Stop();
            
            System.Console.WriteLine("Rollouts using global mutex: " + count);
        }


        public MCTSNode BestActionMultiThreadingGlobalLock(int simulationCount, int threadCount, params int[] seeds)
        {
            Thread[] threads = new Thread[threadCount - 1];

            threadingSimulationCount = simulationCount;

            for (int i = 0; i < threadCount - 1; i++)
            {
                int seed = seeds.Length > i ? seeds[i] : HelperMethods.GetRandomSeed();
                
                Thread newThread = new Thread(new ParameterizedThreadStart(CalculateValuesGlobalLock));
                threads[i] = newThread;
                newThread.Start(new System.Random(seed));
            }

            int[] oldVisits = null;

            if (children != null)
            {
                oldVisits = new int[children.Length];
                for (int i = 0; i < children.Length; i++)
                    oldVisits[i] = children[i] == null ? 0 : children[i].numberOfVisits;
            }

            System.Console.WriteLine("Created the threads");

            int seed2 = seeds.Length > threadCount - 1 ? seeds[threadCount - 1] : HelperMethods.GetRandomSeed();
            CalculateValuesGlobalLock(new System.Random(seed2));

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            System.Console.WriteLine("All threads finished");

            MCTSNode bestChild = children[0];
            int totalVisits = 0;
            int newVisits = 0;

            for (int i = 0; i < children.Length; i++)
            {
                totalVisits += children[i].numberOfVisits;
                if (oldVisits == null)
                    newVisits += children[i].numberOfVisits;
                else
                    newVisits += children[i].numberOfVisits - oldVisits[i];

                if (children[i].numberOfVisits > bestChild.numberOfVisits)
                    bestChild = children[i];
            }

            System.Console.WriteLine("Total: " + totalVisits + " New Visits: " + newVisits);

            System.Console.WriteLine("Best Wins: " + bestChild.score + " Best Visits: " + bestChild.numberOfVisits);
            return bestChild;
        }


        public MCTSNode BestActionInTimeMultiThreadingGlobalLock(float maxTime, int threadCount)
        {
            Thread[] threads = new Thread[threadCount - 1];

            threadingSimulationTime = maxTime;

            // start all the threads
            for (int i = 0; i < threadCount - 1; i++)
            {
                Thread newThread = new Thread(new ParameterizedThreadStart(CalculateValuesGlobalLockInTime));
                threads[i] = newThread;
                newThread.Start(new System.Random());
            }

            System.Console.WriteLine("Created the threads");

            // calculate in this thread as well
            CalculateValuesGlobalLockInTime(new System.Random());

            // wait for everyone to finish
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            System.Console.WriteLine("All threads finished");

            // find the best child
            MCTSNode bestChild = children[0];

            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].numberOfVisits > bestChild.numberOfVisits)
                    bestChild = children[i];
            }

            System.Console.WriteLine("With the chance values of: " + BackGammonChanceAction.PrintValues());
            System.Console.WriteLine("Best Wins: " + bestChild.score + " Best Visits: " + bestChild.numberOfVisits);
            return bestChild;
        }

        // --- MCTS Tree lock MULTITHREAD ---


        public override string ToString()
        {
            return state.ToString();
        }
    }
}