using GAME;
using MCTS;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace User
{
    class Program
    {
        static void Main(string[] args)
        {
            //BackGammonChoiceState start = new BackGammonChoiceState(
            //                              new sbyte[] { 0, -2, -3, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 0, 2 },
            //                              0, 0);

            //BackGammonChoiceState start = new BackGammonChoiceState(
            //                              new sbyte[] { 2, -1, 0, 0, 0, -5, 0, -3, 0, 0, 0, 4, -4, 0, 0, 0, 3, 0, 4, 1, 0, 0, 1, -2 },
            //                              0, 0);

            ////start.RotateBoard();

            BackGammonChoiceState start = new BackGammonChoiceState();

            //start.RotateBoard();

            BackGammonChanceState startDice = new BackGammonChanceState(new BackGammonChoiceAction(2, 4));

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

            Console.WriteLine("How many iterations?");
            int simulationCount = GetIntegerInput();

            Stopwatch sw = new Stopwatch();
            
            sw.Start();

            MCTSNode bestMove = startNode.BestActionMultiThreading(simulationCount, 1);
            //MCTSNode bestMove = startNode.BestActionInTimeMultiThreading(5000, 4);

            sw.Stop();
            Console.WriteLine(sw.Elapsed);
            ((BackGammonChoiceState)bestMove.GetState()).RotateBoard();
            Console.WriteLine(bestMove.GetState());

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

        public static int GetIntegerInput()
        {
            while (true)
            {
                string input = Console.ReadLine();
                int val;
                if (int.TryParse(input, out val))
                    return val;
            }

        }
    }
}