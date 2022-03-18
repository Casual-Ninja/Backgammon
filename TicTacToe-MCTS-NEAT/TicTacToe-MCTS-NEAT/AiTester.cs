using System;
using GAME;
using MCTS;

namespace TicTacToe_MCTS_NEAT
{
    class AiTester
    {
        private const float preferedPlayScore = 1;
        private const float alternativePlayScore = 0.9f;

        public static void FindBestValueForHyperParamater(int simulationCount)
        {
            float actualHyperParamater = MCTSNode.CHyperParam;

            for (float mult = 0.1f; mult <= 2; mult += 0.1f)
            {
                MCTSNode.CHyperParam = actualHyperParamater * mult;
                Console.WriteLine(MCTSNode.CHyperParam);
                float score = TestAi(simulationCount);

                Console.WriteLine($"Mult: {mult} score: {score}");
            }

            MCTSNode.CHyperParam = actualHyperParamater;
        }

        public static float TestAi(int simulationCount)
        {
            float score = 0;

            score += TestCase1(simulationCount);
            score += TestCase2(simulationCount);
            score += TestCase3(simulationCount);
            score += TestCase4(simulationCount);
            score += TestCase5(simulationCount);
            score += TestCase6(simulationCount);
            score += TestCase7(simulationCount);
            score += TestCase8(simulationCount);
            score += TestCase9(simulationCount);
            score += TestCase10(simulationCount);
            score += TestCase11(simulationCount);
            score += TestCase12(simulationCount);
            score += TestCase13(simulationCount);
            score += TestCase14(simulationCount);
            score += TestCase15(simulationCount);

            return score;
        }

        private static float TestCase1(int simulationCount)
        {
            BackGammonChoiceState preferredPlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, -1, -4, 0, -3, 0, 0, -1, 5, -4, 0, 0, 0, 3, 0, 5, 0, 0, 0, 0, -2 },
                                          0, 0);

            BackGammonChoiceState alternativePlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, -1, 5, -4, 0, 0, 0, 3, 0, 5, 0, 0, 0, -1, -1 },
                                          0, 0);

            return GetScore(simulationCount, 1, 2, preferredPlay, alternativePlay);
        }
        private static float TestCase2(int simulationCount)
        {
            BackGammonChoiceState preferredPlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, -2, -4, 0, -2, 0, 0, 0, 5, -5, 0, 0, 0, 3, 0, 5, 0, 0, 0, 0, -2 },
                                          0, 0);

            return GetScore(simulationCount, 1, 3, preferredPlay);
        }
        private static float TestCase3(int simulationCount)
        {
            BackGammonChoiceState preferredPlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -3, -1, 0, 0, 5, -4, 0, 0, 0, 3, 0, 5, 0, 0, 0, -1, -1 },
                                          0, 0);

            BackGammonChoiceState alternativePlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, -1, -4, 0, -3, -1, 0, 0, 5, -4, 0, 0, 0, 3, 0, 5, 0, 0, 0, 0, -2 },
                                          0, 0);

            return GetScore(simulationCount, 1, 4, preferredPlay, alternativePlay);
        }
        private static float TestCase4(int simulationCount)
        {
            BackGammonChoiceState preferredPlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -4, 0, 0, 0, 5, -4, 0, 0, 0, 3, 0, 5, 0, 0, 0, -1, -1 },
                                          0, 0);

            BackGammonChoiceState alternativePlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, -1, -4, 0, -4, 0, 0, 0, 5, -4, 0, 0, 0, 3, 0, 5, 0, 0, 0, 0, -2 },
                                          0, 0);

            return GetScore(simulationCount, 1, 5, preferredPlay, alternativePlay);
        }
        private static float TestCase5(int simulationCount)
        {
            BackGammonChoiceState preferredPlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, -2, -2, 0, 0, 0, 5, -4, 0, 0, 0, 3, 0, 5, 0, 0, 0, 0, -2 },
                                          0, 0);

            return GetScore(simulationCount, 1, 6, preferredPlay);
        }
        private static float TestCase6(int simulationCount)
        {
            BackGammonChoiceState preferredPlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, -1, 5, -4, 0, 0, 0, 3, 0, 5, 0, -1, 0, 0, -1 },
                                          0, 0);

            BackGammonChoiceState alternativePlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -3, 0, -1, -1, 5, -3, 0, 0, 0, 3, 0, 5, 0, 0, 0, 0, -2 },
                                          0, 0);

            return GetScore(simulationCount, 2, 3, preferredPlay, alternativePlay);
        }
        private static float TestCase7(int simulationCount)
        {
            BackGammonChoiceState preferredPlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, -2, 0, -4, 0, -2, 0, 0, 0, 5, -5, 0, 0, 0, 3, 0, 5, 0, 0, 0, 0, -2 },
                                          0, 0);

            return GetScore(simulationCount, 2, 4, preferredPlay);
        }
        private static float TestCase8(int simulationCount)
        {
            BackGammonChoiceState preferredPlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -4, 0, 0, 0, 5, -4, 0, 0, 0, 3, 0, 5, 0, 0, -1, 0, -1 },
                                          0, 0);

            BackGammonChoiceState alternativePlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -4, 0, 0, -1, 5, -3, 0, 0, 0, 3, 0, 5, 0, 0, 0, 0, -2 },
                                          0, 0);

            return GetScore(simulationCount, 2, 5, preferredPlay, alternativePlay);
        }
        private static float TestCase9(int simulationCount)
        {
            BackGammonChoiceState preferredPlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, -1, 5, -4, 0, 0, 0, 3, -1, 5, 0, 0, 0, 0, -1 },
                                          0, 0);

            BackGammonChoiceState alternativePlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, -1, -5, 0, -3, 0, 0, 0, 5, -4, 0, 0, 0, 3, 0, 5, 0, 0, 0, 0, -2 },
                                          0, 0);

            return GetScore(simulationCount, 2, 6, preferredPlay, alternativePlay);
        }
        private static float TestCase10(int simulationCount)
        {
            BackGammonChoiceState preferredPlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -3, -1, -1, 0, 5, -3, 0, 0, 0, 3, 0, 5, 0, 0, 0, 0, -2 },
                                          0, 0);

            BackGammonChoiceState alternativePlay1 = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -3, -1, 0, 0, 5, -4, 0, 0, 0, 3, 0, 5, 0, -1, 0, 0, -1 },
                                          0, 0);

            BackGammonChoiceState alternativePlay2 = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -3, 0, -1, 0, 5, -4, 0, 0, 0, 3, 0, 5, -1, 0, 0, 0, -1 },
                                          0, 0);

            return GetScore(simulationCount, 3, 4, preferredPlay, alternativePlay1, alternativePlay2);
        }
        private static float TestCase11(int simulationCount)
        {
            BackGammonChoiceState preferredPlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, -2, 0, 0, -4, 0, -2, 0, 0, 0, 5, -5, 0, 0, 0, 3, 0, 5, 0, 0, 0, 0, -2 },
                                          0, 0);

            return GetScore(simulationCount, 3, 5, preferredPlay);
        }
        private static float TestCase12(int simulationCount)
        {
            BackGammonChoiceState preferredPlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -3, 0, -1, 0, 5, -4, 0, 0, 0, 3, -1, 5, 0, 0, 0, 0, -1 },
                                          0, 0);

            BackGammonChoiceState alternativePlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, -5, 0, -1, 0, 3, 0, 5, 0, 0, 0, 0, -1 },
                                          0, 0);

            return GetScore(simulationCount, 3, 6, preferredPlay, alternativePlay);
        }
        private static float TestCase13(int simulationCount)
        {
            BackGammonChoiceState preferredPlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -4, 0, 0, 0, 5, -4, 0, 0, 0, 3, 0, 5, -1, 0, 0, 0, -1 },
                                          0, 0);

            BackGammonChoiceState alternativePlay1 = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -4, -1, 0, 0, 5, -3, 0, 0, 0, 3, 0, 5, 0, 0, 0, 0, -2 },
                                          0, 0);

            BackGammonChoiceState alternativePlay2 = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, -5, 0, -1, 0, 3, 0, 5, 0, 0, 0, 0, -1 },
                                          0, 0);

            return GetScore(simulationCount, 4, 5, preferredPlay, alternativePlay1, alternativePlay2);
        }
        private static float TestCase14(int simulationCount)
        {
            BackGammonChoiceState preferredPlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -3, -1, 0, 0, 5, -4, 0, 0, 0, 3, -1, 5, 0, 0, 0, 0, -1 },
                                          0, 0);

            BackGammonChoiceState alternativePlay1 = new BackGammonChoiceState(
                                          new sbyte[] { 2, -2, 0, 0, 0, -4, 0, -2, 0, 0, 0, 5, -5, 0, 0, 0, 3, 0, 5, 0, 0, 0, 0, -2 },
                                          0, 0);

            BackGammonChoiceState alternativePlay2 = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, -5, -1, 0, 0, 3, 0, 5, 0, 0, 0, 0, -1 },
                                          0, 0);

            return GetScore(simulationCount, 4, 6, preferredPlay, alternativePlay1, alternativePlay2);
        }
        private static float TestCase15(int simulationCount)
        {
            BackGammonChoiceState preferredPlay = new BackGammonChoiceState(
                                          new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, -6, 0, 0, 0, 3, 0, 5, 0, 0, 0, 0, -1 },
                                          0, 0);

            return GetScore(simulationCount, 5, 6, preferredPlay);
        }


        private static float GetScore(int simulationCount, byte die1, byte die2, params State[] wantedBoards)
        {
            State aiPlayedBoard = GetBestMove(simulationCount, die1, die2);

            if (aiPlayedBoard.Equals(wantedBoards[0]))
            {
                Console.WriteLine($"{die1}-{die2}: {preferedPlayScore}");
                return preferedPlayScore;
            }

            for (int i = 1; i < wantedBoards.Length; i++)
            {
                if (aiPlayedBoard.Equals(wantedBoards[i]))
                {
                    Console.WriteLine($"{die1}-{die2}: {alternativePlayScore}");
                    return alternativePlayScore;
                }
            }
            Console.WriteLine($"{die1}-{die2}: 0");
            return 0;
        }

        private static State GetBestMove(int simulationCount, byte die1, byte die2)
        {
            BackGammonChoiceState start = new BackGammonChoiceState();

            BackGammonChanceState startDice = new BackGammonChanceState(new Dice(die1, die2));

            MCTSNode parentOfStart = new MCTSNode(start, MCTSNode.CHyperParam);

            MCTSNode startNode = new MCTSNode(startDice, parentOfStart);

            MCTSNode bestMove = startNode.BestAction(simulationCount);

            //Console.WriteLine(bestMove.GetState());

            return bestMove.GetState();
        }
    }
}
