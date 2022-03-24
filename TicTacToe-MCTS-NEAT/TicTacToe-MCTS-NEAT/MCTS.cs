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

        private int initNumberOfVisits;
        private int initScore;


        private List<Action> untriedActions;
        private System.Random rnd;

        private int threadingSimulationCount;
        private float threadingSimulationTime;

        public const float CHyperParamDefault = 1.414213f;

        private float cHyperParam;

        public static float CHyperParam = 1.414213f;

        public State GetState() { return this.state; }

        public State GetPreviousState() { return (parent != null) ? parent.GetState() : null; }

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

        public MCTSNode(MCTSNode CopyTree, MCTSNode actualParent)
        {
            this.cHyperParam = CopyTree.cHyperParam;
            this.score = CopyTree.score;
            this.initScore = CopyTree.score;

            this.numberOfVisits = CopyTree.numberOfVisits;
            this.initNumberOfVisits = CopyTree.numberOfVisits;

            this.children = new MCTSNode[CopyTree.children.Length];

            untriedActions = new List<Action>();
            foreach (Action action in CopyTree.untriedActions)
                untriedActions.Add(action.Copy());

            this.state = CopyTree.state.Copy();
            this.parent = actualParent;

            for (int i = 0; i < CopyTree.children.Length; i++)
            {
                if (CopyTree.children[i] != null)
                {
                    children[i] = new MCTSNode(CopyTree.children[i], this);
                }
            }
        }

        public List<GAME.Action> GetLegalActions()
        {
            return this.state.GetLegalActions(parent.state);
        }

        private MCTSNode Expand(System.Random rnd)
        {
            int index = HelperMethods.GetRandomIndexFromList(this.untriedActions, rnd);
            Action action = untriedActions[index];
            untriedActions.RemoveAt(index);

            State nextState = this.state.Move(parent.state, action);
            MCTSNode childNode = new MCTSNode(nextState, this);

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
            //children.Add(childNode);
            return childNode;
        }

        private bool IsTerminalNode()
        {
            return this.state.IsGameOver();
        }

        private int RollOut(System.Random rnd)
        {
            State parentState = parent.state;
            State rollOutState = this.state;
            int mult = -1;
            while (rollOutState.IsGameOver() == false)
            {
                Action action = RolloutPolicySoftPlay(rollOutState.GetLegalActions(parentState), rollOutState, rnd);
                //Action action = RolloutPolicyHardPlay(rollOutState.GetLegalActions(parentState), rollOutState, parentState, rnd);

                State temp = rollOutState;

                rollOutState = rollOutState.Move(parentState, action);
                parentState = temp;

                if (rollOutState.IsChanceState())
                    mult = -mult;
            }
            return rollOutState.GameResult() * mult;
        }

        private void BackPropagate(int result)
        {
            this.numberOfVisits += 1;
            this.score += System.Math.Max(0, result);

            if (parent != null)
            {
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

        private float CalculateUCB(MCTSNode child)
        {
            return ((float)child.score / child.numberOfVisits) + cHyperParam * (float)System.Math.Sqrt(System.Math.Log(numberOfVisits) / child.numberOfVisits);
        }

        private MCTSNode BestChild(System.Random rnd)
        {
            MCTSNode bestChild = HelperMethods.GetRandomFromArray(children, rnd);

            if (children[0].state.IsChanceState()) // if its chance state, return it randomly
            {
                return bestChild;
            }

            float bestScore = CalculateUCB(bestChild);

            for (int i = 0; i < this.children.Length; i++)
            {
                MCTSNode child = children[i];
                float score = CalculateUCB(child);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestChild = child;
                }
            }

            return bestChild;
        }

        private Action RolloutPolicySoftPlay(List<Action> possibleMoves, State state, System.Random rnd)
        {
            // soft play, im not just using random on the list because if its the dice options, its not really supposed to be uniformaly random
            return state.RandomPick(possibleMoves, rnd);
        }

        private Action RolloutPolicyHardPlay(List<Action> possibleMoves, State state, State prevState, System.Random rnd)
        {
            if (state.IsChanceState()) // if its chance state still pick randomly
                return state.RandomPick(possibleMoves, rnd);

            // if its not chance state, chose state corresponding to score
            float[] scores = new float[possibleMoves.Count];
            float sumOfScores = 0;
            for (int i = 0; i < possibleMoves.Count; i++)
            {
                // score ranged between 0 - infinity
                scores[i] = possibleMoves[i].GetScore(prevState);
                sumOfScores += scores[i];
            }
            float randomValue = HelperMethods.RandomValue(0, sumOfScores, rnd);
            for (int i = 0; i < scores.Length; i++)
            {
                randomValue -= scores[i];
                if (randomValue <= 0)
                    return possibleMoves[i];
            }
            return null;
        }

        private MCTSNode TreePolicy(System.Random rnd)
        {
            MCTSNode currentNode = this;

            while (currentNode.IsTerminalNode() == false)
            {
                if (currentNode.IsFullyExpanded() == false)
                    return currentNode.Expand(rnd);
                else
                    currentNode = currentNode.BestChild(rnd);
            }
            return currentNode;
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
            System.Console.WriteLine("Rollouts: " + count);
        }

        public MCTSNode GetRandomChild(System.Random rndToUse)
        {
            while (IsFullyExpanded() == false)
            {
                Expand(rndToUse);
            }

            return HelperMethods.GetRandomFromArray(children, rndToUse);
        }

        public MCTSNode GetStateDefinedRandomChild(System.Random rndToUse)
        {
            while (IsFullyExpanded() == false)
            {
                Expand(rndToUse);
            }

            return children[state.RandomPick(children.Length, rndToUse)];
        }

        public MCTSNode GetChild(int index) { return children[index]; }

        public MCTSNode GetChild(State childState)
        {
            foreach (MCTSNode child in children)
            {
                if (child != null)
                {
                    if (childState.Equals(child.GetState()))
                        return child;
                }
            }
            return new MCTSNode(childState, this);
        }

        public void DeleteInfoOfUpperTree()
        {
            this.parent = null;
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
                AddValuesOfTree(tree);
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

        public MCTSNode BestActionInTime(float maxTime, System.Diagnostics.Stopwatch stopWatch)
        {
            rnd = new System.Random();
            stopWatch.Start();
            int count = 0;
            while (stopWatch.ElapsedMilliseconds < maxTime)
            {
                MCTSNode v = TreePolicy(rnd);
                v.BackPropagate(v.RollOut(rnd));
                count++;
            }

            MCTSNode bestChild = children[0];
            for (int i = 1; i < children.Length; i++)
                if (children[i].numberOfVisits > bestChild.numberOfVisits)
                    bestChild = children[i];
            System.Console.WriteLine("Rollouts: " + count);
            return bestChild;
        }

        public MCTSNode BestActionInTimeMultiThreading(float maxTime, int threadCount)
        {
            Thread[] threads = new Thread[threadCount - 1];
            MCTSNode[] workedOnNodes = new MCTSNode[threadCount - 1];

            for (int i = 0; i < threadCount - 1; i++)
            {
                MCTSNode newParentNode = new MCTSNode(parent.state.Copy(), MCTSNode.CHyperParam);
                MCTSNode newStartNode = new MCTSNode(state.Copy(), newParentNode);
                newStartNode.threadingSimulationTime = maxTime;
                newStartNode.rnd = new System.Random(HelperMethods.GetRandomSeed());

                workedOnNodes[i] = newStartNode;
                Thread newThread = new Thread(new ThreadStart(newStartNode.CalculateTreeNumbersWithTime));
                threads[i] = newThread;
                newThread.Start();
            }

            threadingSimulationTime = maxTime;
            rnd = new System.Random(HelperMethods.GetRandomSeed());


            int[] oldVisits = null;

            if (children != null)
            {
                oldVisits = new int[children.Length];
                for (int i = 0; i < children.Length; i++)
                    oldVisits[i] = children[i] == null ? 0 : children[i].numberOfVisits;
            }

            System.Console.WriteLine("Created the threads");

            CalculateTreeNumbersWithTime();

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            System.Console.WriteLine("All threads finished");

            foreach (MCTSNode tree in workedOnNodes)
            {
                AddValuesOfTree(tree);
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

            System.Console.WriteLine("Total: " + totalVisits + " New Vists: " + newVisits);

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

        private void AddValuesOfTree(MCTSNode otherTree)
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
                    children[i].AddValuesOfTree(otherTree.children[i]);
                }
            }
        }

        public override string ToString()
        {
            return state.ToString();
        }
    }
}