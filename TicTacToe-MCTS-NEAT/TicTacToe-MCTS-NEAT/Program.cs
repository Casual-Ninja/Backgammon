using GAME;
using HelperSpace;
using MCTS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using TicTacToe_MCTS_NEAT;

namespace User
{
    class Program
    {
        private static string neuralNetworkPath = "D:\\BackGammonNNPath\\Network1";
        private static string neuralNetworkGameCountPath = "D:\\BackGammonNNPath\\GameCount1";

        static void Main(string[] args)
        {
            //BackGammonChoiceState start = new BackGammonChoiceState(
            //                              new sbyte[] { 1, -2, 0, 1, 2, -5, 0, -2, -2, 0, 0, 0, -3, 0, 2, 0, 0, 0, 3, -1, 4, 2, 0, 0 },
            //                              0, 0);

            //BackGammonChoiceState start = new BackGammonChoiceState(
            //                              new sbyte[] { 2, -1, 0, 0, 0, -5, 0, -3, 0, 0, 0, 4, -4, 0, 0, 0, 3, 0, 4, 1, 0, 0, 1, -2 },
            //                              0, 0);

            ////start.RotateBoard();

            BackGammonChoiceState start = new BackGammonChoiceState();

            Console.WriteLine(start);
            BackGammonChanceState startDice = new BackGammonChanceState(new Dice(2, 4));

            //List<GAME.Action> actions = startDice.GetLegalActions(start);

            //Console.WriteLine("The legal actions:");
            //Console.WriteLine();

            ////Console.WriteLine(HelperMethods.ListToString(actions));

            //foreach (GAME.Action action in actions)
            //{
            //    Console.WriteLine(action + " score: " + action.GetScore(start));
            //}


            //Console.WriteLine(HelperMethods.ListToString(startDice.GetLegalActions(start)));

            MCTSNode parentStartNode = new MCTSNode(start, MCTSNode.CHyperParam);

            MCTSNode startNode = new MCTSNode(startDice, parentStartNode);

            Console.WriteLine(parentStartNode);

            Stopwatch sw = new Stopwatch();

            sw.Start();
            //MCTSNode bestMove = startNode.BestActionInTimeMultiThreading(50000, 8);

            MCTSNode bestMove = startNode.BestAction(200000);
            //MCTSNode bestMove = startNode.BestActionInTime(1000, sw);

            sw.Stop();
            Console.WriteLine(sw.Elapsed);
            Console.WriteLine(MCTSNode.sumOfAmount + " " + MCTSNode.statesChecked + " " + (MCTSNode.sumOfAmount / (float)MCTSNode.statesChecked));

            //((BackGammonChoiceState)bestMove.GetState()).RotateBoard();
            //Console.WriteLine(bestMove);
            //Console.WriteLine("Difference count: " + BackGammonChanceState.VersionDifferenceCount);

            //MCTSNode.CHyperParam *= 0.1f;
            //Console.WriteLine(MCTSNode.CHyperParam);

            //PlayAgainstAi(10000, false, 8); // play against ai where he has X seconds to think of a move, and he gets Y threads
            //FindBestHyperParamater(20, 10000, 0.1f, 2f, 1f, new Random());

            //CheckAiPerformanceForDieWithMultiThread(10000, 4, 6, 1);

            //Console.WriteLine(BackGammonChanceAction.isBigger);


            //AiTester.FindBestValueForHyperParamater(0.1f, 2.05f, 0.1f ,20000, 1521);

            // with 200,000 ai tester found:
            // 0.1f = 1
            // 0.2f = 3.8
            // 0.3f = 4.8
            // 0.4f = 5.8
            // 0.5f = 4.9
            // 0.6f = 5.8
            // 0.7f = 4.8 
            // 0.8f = 4.8
            // 0.9f = 4.8
            // 1.0f = 4.8
            // 1.1f = 4.8 
            // 1.2f = 4.8
            // 1.3f = 4.8
            // 1.4f = 4.8
            // 1.5f = 4.8
            // 1.6f = 4.8
            // 1.7f = 4.8
            // 1.8f = 4.8
            // 1.9f = 4.8 (didn't update this...)

            // 200,000 with a corrected random child pick in BestChild ai tester found:
            // 0.1f = 2.8
            // 0.2f = 7.6
            // 0.3f = 5.7
            // 0.4f = 4.8
            // 0.5f = 4.8
            // 0.6f = 4.8
            // 0.7f = 4.8
            // 0.8f = 4.8
            // 0.9f = 4.8
            // 1.0f = 4.8
            // 1.1f = 4.8
            // 1.2f = 4.8
            // 1.3f = 5.8
            // 1.4f = 4.8
            // 1.5f = 4.8
            // 1.6f = 4.8
            // 1.7f = 5.8
            // 1.8f = 4.8
            // 1.9f = 4.8
            // 2.0f = 4.8
        }

        private static void CheckAiPerformanceForDie(int simulationCount, byte die1, byte die2)
        {
            BackGammonChoiceState start = new BackGammonChoiceState();

            BackGammonChanceState startDice = new BackGammonChanceState(new Dice(die1, die2));

            MCTSNode parentOfStart = new MCTSNode(start, MCTSNode.CHyperParam);

            MCTSNode startNode = new MCTSNode(startDice, parentOfStart);

            Stopwatch sw = new Stopwatch();

            sw.Start();
            MCTSNode bestMove = startNode.BestAction(simulationCount);

            sw.Stop();
            Console.WriteLine(sw.Elapsed);

            ((BackGammonChoiceState)bestMove.GetState()).RotateBoard();

            Console.WriteLine(bestMove);
        }

        private static void CheckAiPerformanceForDieWithMultiThread(int simulationCount, byte die1, byte die2, int threadCount, params int[] seeds)
        {
            BackGammonChoiceState start = new BackGammonChoiceState();

            BackGammonChanceState startDice = new BackGammonChanceState(new Dice(die1, die2));

            MCTSNode parentOfStart = new MCTSNode(start, MCTSNode.CHyperParam);

            MCTSNode startNode = new MCTSNode(startDice, parentOfStart);

            Stopwatch sw = new Stopwatch();

            sw.Start();
            MCTSNode bestMove = startNode.BestActionMultiThreading(simulationCount, threadCount, seeds);

            sw.Stop();
            Console.WriteLine(sw.Elapsed);

            ((BackGammonChoiceState)bestMove.GetState()).RotateBoard();

            Console.WriteLine(bestMove);
        }

        private static void PlayAgainstAi(float MsToThink, bool AiStart, int threads)
        {
            Random rnd = new Random();

            BackGammonChoiceState start = new BackGammonChoiceState();

            //BackGammonChoiceState start = new BackGammonChoiceState(
            //                              new sbyte[] { -2, -1, -2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            //                              0, 0);

            //start.RotateBoard();

            BackGammonChanceState startDice = new BackGammonChanceState(start.GetStartingAction(rnd));

            //BackGammonChanceState startDice = new BackGammonChanceState(new Dice(5, 6));

            MCTSNode parentStartNode = new MCTSNode(start, MCTSNode.CHyperParam);

            MCTSNode startNode = new MCTSNode(startDice, parentStartNode);

            bool isComputer = AiStart;

            while (true)
            {
                if (parentStartNode.GetState().IsGameOver())
                {
                    if (isComputer)
                        Console.WriteLine("Player won!");
                    else
                        Console.WriteLine("Computer won!");
                    break;
                }

                Console.WriteLine("-----------");
                PrintState(parentStartNode.GetState(), startNode.GetState(), AiStart != isComputer);

                if (isComputer == false)
                {
                    BackGammonChanceAction action = GetActionInput(startNode.GetState(), startNode.GetPreviousState());

                    Console.WriteLine(action);

                    State newState = startNode.GetState().Move(parentStartNode.GetState(), action);

                    parentStartNode = startNode.GetChild(newState);
                }
                else
                {
                    List<GAME.Action> legalActions = startNode.GetLegalActions();

                    if (legalActions.Count == 1) // ai has only 1 option, so just do it
                    {
                        //parentStartNode = new MCTSNode(startNode.GetState().Move(parentStartNode.GetState(), legalActions[0]));

                        parentStartNode = startNode.GetRandomChild(rnd); // just picks the only child
                    }
                    else
                    {
                        MCTSNode bestMove = startNode.BestActionInTimeMultiThreading(MsToThink, threads);

                        //parentStartNode = new MCTSNode(new BackGammonChoiceState((BackGammonChoiceState)bestMove.GetState()));

                        parentStartNode = bestMove;
                    }
                    
                }
                //startDice = new BackGammonChanceState(parentStartNode.GetState().RandomPick(parentStartNode.GetState().GetLegalActions(null), rnd));

                //startNode = new MCTSNode(startDice, parentStartNode);
                startNode = parentStartNode.GetStateDefinedRandomChild(rnd);

                parentStartNode.DeleteInfoOfUpperTree(); // no need to remmember info on moves not available anymore


                //Console.WriteLine(parentStartNode.GetType());
                //Console.WriteLine(startNode.GetType());

                isComputer = !isComputer;
            }
            Console.ReadLine();
        }

        private static BackGammonChanceAction GetActionInput(State currentState, State prevState)
        {
            BackGammonChanceAction action = new BackGammonChanceAction();

            List<GAME.Action> legalActions = currentState.GetLegalActions(prevState);

            bool first = true;
            while (IsLikeActionInLegalActions(prevState, currentState, legalActions, action) == false)
            {
                if (!first)
                {
                    Console.WriteLine("Cannot do that action...");
                    action = new BackGammonChanceAction();
                }
                first = false;
                for (int i = 0; i < ((BackGammonChanceAction)legalActions[0]).Count; i++)
                {
                    while (true)
                    {
                        Console.WriteLine("Enter action " + i);
                        string input = Console.ReadLine();

                        string[] split = input.Split('/');

                        if (split.Length == 2)
                        {
                            int indexFrom;
                            if (int.TryParse(split[0], out indexFrom))
                            {
                                int indexTo;

                                if (int.TryParse(split[1], out indexTo))
                                {
                                    action.AddToAction(((sbyte)(indexFrom), (sbyte)(indexTo)));
                                    break;
                                }
                            }

                        }
                        else
                        {
                            if (input == "legal")
                            {
                                Console.WriteLine("Legal Actions:");
                                foreach (GAME.Action act in legalActions)
                                    Console.WriteLine(act);
                            }
                        }
                        Console.WriteLine("Try to input action again");
                    }
                }
            }
            
            return action;
        }

        private static bool IsLikeActionInLegalActions(State prevState, State state, List<GAME.Action> legalActions, GAME.Action checkAction)
        {
            foreach (GAME.Action action in legalActions)
            {
                State checkNewState = state.Move(prevState, action);
                if (checkNewState.Equals(state.Move(prevState, checkAction)))
                    return true;
            }
            return false;
        }

        private static void PrintState(State choiceState, State chanceState, bool rotate)
        {
            if (!rotate)
                Console.WriteLine(choiceState);
            else
                Console.WriteLine(((BackGammonChanceState)chanceState).GetNewRotated((BackGammonChoiceState)choiceState));
            Console.WriteLine(chanceState);
        }

        private static void PrintCheckValues(BackGammonChanceState chanceState, BackGammonChoiceState choiceState)
        {
            List<GAME.Action> actions = chanceState.GetLegalActions(choiceState);

            Console.WriteLine("Actions count: " + actions.Count);

            foreach (GAME.Action action in actions)
            {
                Console.WriteLine(action);
            }
        }
    }

    public class ArenaFightInformation
    {
        private bool newValueStarts = false;
        public bool GetStartsValue()
        {
            newValueStarts = !newValueStarts;
            return !newValueStarts;
        }

        public float currentValue;
        public float newValue;

        public float currentScore;
        public float newScore;

        public int simulationsPerTurn;

        public ArenaFightInformation(float currentValue, float newValue, float currentScore, float newScore, int simulationsPerTurn)
        {
            this.currentValue = currentValue;
            this.newValue = newValue;
            this.currentScore = currentScore;
            this.newScore = newScore;
            this.simulationsPerTurn = simulationsPerTurn;
        }
    }
}