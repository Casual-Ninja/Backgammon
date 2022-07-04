using GAME;
using MCTS;
using System;
using System.Diagnostics;
using TicTacToe_MCTS_NEAT;
using System.Collections.Generic;

namespace User
{
    class Program
    {
        static void Main(string[] args)
        {
            CheckGetNextStatesMethod();
            return;

            //FindBestValues();
            //return;

            BackGammonChoiceState start = new BackGammonChoiceState(
                                          new sbyte[] { 1, -3, -1, 1, 0, -3, 0, -2, 0, 0, -2, 3, -3, 0, 0, 0, 0, 0, 4, 0, 3, 0, 3, -1 },
                                          0, 0);

            //BackGammonChoiceState start = new BackGammonChoiceState(
            //                              new sbyte[] { 2, -1, 0, 0, 0, -5, 0, -3, 0, 0, 0, 4, -4, 0, 0, 0, 3, 0, 4, 1, 0, 0, 1, -2 },
            //                              0, 0);

            ////start.RotateBoard();


            //BackGammonChoiceState start = new BackGammonChoiceState();

            //start.RotateBoard();

            BackGammonChanceState startDice = new BackGammonChanceState(new BackGammonChoiceAction(3, 4));

            

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

            Console.WriteLine("Legal Actions: " + HelperSpace.HelperMethods.ListToString(startDice.GetLegalActions(start)));

            Console.WriteLine("How many iterations?");
            int simulationCount = GetIntegerInput();
            Console.WriteLine("How many threads?");
            int threadCount = GetIntegerInput();

            Stopwatch sw = new Stopwatch();
            
            sw.Start();

            MCTSNode bestMove = startNode.BestActionMultiThreading(simulationCount, threadCount);
            //MCTSNode bestMove = startNode.BestActionMultiThreadingGlobalLock(simulationCount, threadCount);

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

        private static string valuesPath = "ChanceValues\\Values";
        private static string evolutionCountPath = "ChanceValues\\evolutionCount";

        public static void FindBestValues()
        {
            Random rnd = new Random();

            ChanceActionValues[] options = new ChanceActionValues[20];



            int evolutionCount = 0;

            if (SaveLoad.PathExists(evolutionCountPath))
            {
                object loadedData; 
                SaveLoad.LoadData<int>(evolutionCountPath, out loadedData);
                evolutionCount = (int)loadedData;
                Console.WriteLine("Loaded evolution count");
            }
            
            

            if (SaveLoad.PathExists(valuesPath))
            {
                object loadedData;
                SaveLoad.LoadData<ChanceActionValues[]>(valuesPath, out loadedData);
                options = (ChanceActionValues[])loadedData;

                ChanceActionValues[] newOptionsLength = new ChanceActionValues[15];
                for (int i = 0; i < newOptionsLength.Length; i++)
                {
                    newOptionsLength[i] = options[i];
                }

                options = newOptionsLength;

                Console.WriteLine("Loaded the current options");
            }
            else
            {
                FillArrayWithRandomValues(options, rnd);
                Console.WriteLine("Using new options");
            }

            

            while (true)
            {
                BattleArenaPhase(options, 200, 4, rnd);

                ChanceActionValues valueChanged = new ChanceActionValues(options[0]);
                MutateValues(valueChanged, rnd);
                options[options.Length / 2] = valueChanged;

                options[options.Length - 1] = GetRandomValue(rnd);

                for (int i = options.Length / 2 + 1; i < options.Length - 1; i++)
                {
                    if (rnd.Next(2) == 0)
                    {
                        ChanceActionValues value = new ChanceActionValues(options[rnd.Next(options.Length / 2 + 1)]);
                        MutateValues(value, rnd);
                        options[i] = value;
                    }
                    else
                    {
                        ChanceActionValues value = new ChanceActionValues(options[rnd.Next(options.Length / 2 + 1)], options[rnd.Next(options.Length / 2 + 1)], rnd);
                        options[i] = value;
                    }
                }

                evolutionCount++;

                SaveLoad.SaveData(evolutionCountPath, evolutionCount);
                SaveLoad.SaveData(valuesPath, options);
                SaveLoad.SaveAllDataToDiskInThread();
                Console.WriteLine("Saved the current options");
            }
        }

        private static void BattleArenaPhase(ChanceActionValues[] values, float timePerTurn, int threadsPerTurn, Random rnd)
        {
            int[] scores = new int[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    if (j == i)
                        continue;

                    Console.WriteLine($"-----    Starting the battle between [ {i} vs {j} ]    -----");
                    // positive score means i won, negative means j won
                    int score = Battle(values[i], values[j], timePerTurn, threadsPerTurn, rnd);

                    score += Battle(values[i], values[j], timePerTurn, threadsPerTurn, rnd);

                    score += Battle(values[i], values[j], timePerTurn, threadsPerTurn, rnd);

                    score += Battle(values[i], values[j], timePerTurn, threadsPerTurn, rnd);

                    score += Battle(values[i], values[j], timePerTurn, threadsPerTurn, rnd);
                    //int score = rnd.Next(1, 4);

                    if (score > 0)
                        scores[i] += score;
                    else
                        scores[j] += Math.Abs(score);
                }
            }

            HelperSpace.HelperMethods.QuickSort(scores, values);

            Console.Write("Scores:");
            for (int i = 0; i < scores.Length; i++)
            {
                Console.Write(scores[i] + " ");
            }
            Console.WriteLine();

            // flip array so now the best are first
            HelperSpace.HelperMethods.FlipArray(values);
        }

        private static int Battle(ChanceActionValues value1, ChanceActionValues value2, float timePerTurn, int threadsPerTurn, Random rnd)
        {
            BackGammonChoiceState state = new BackGammonChoiceState();
            
            BackGammonChanceState dice = new BackGammonChanceState(state.GetStartingAction(rnd));

            int turn = (rnd.Next(0, 2) * 2) - 1;

            MCTSNode parentNode = new MCTSNode(state, MCTSNode.CHyperParamDefault);

            MCTSNode currNode = new MCTSNode(dice, parentNode);

            while (parentNode.GetState().IsGameOver() == false)
            {
                turn = -turn;

                if (turn == 1)
                    BackGammonChanceAction.UseValues(value1);
                else
                    BackGammonChanceAction.UseValues(value2);

                parentNode = new MCTSNode(currNode.BestActionInTimeMultiThreadingGlobalLock(timePerTurn, threadsPerTurn).GetState(), MCTSNode.CHyperParamDefault);

                currNode = new MCTSNode(((BackGammonChoiceState)parentNode.GetState()).GetRandomNextState(rnd), parentNode);
            }

            return ((BackGammonChoiceState)parentNode.GetState()).ActualGameScoreResult() * turn;
        }

        private static void FillArrayWithRandomValues(ChanceActionValues[] values, Random rnd)
        {
            values[0] = new ChanceActionValues(); // default values (this wont ever change)

            for (int i = 1; i < values.Length; i++)
                values[i] = GetRandomValue(rnd);
        }

        private static ChanceActionValues GetRandomValue(Random rnd)
        {
            float houseScore = (float)rnd.NextDouble() * 3f;
            float regularMoveScore = (float)rnd.NextDouble() * 3f;
            float runningGameBackScore = (float)rnd.NextDouble() * 30f;
            return new ChanceActionValues(houseScore, regularMoveScore, runningGameBackScore);
        }

        private static void MutateValues(ChanceActionValues values, Random rnd)
        {
            values.houseScore += ((float)rnd.NextDouble() - 0.5f);
            values.regularMoveScore += ((float)rnd.NextDouble() - 0.5f);
            values.runningGameBackScore += ((float)rnd.NextDouble() - 0.5f) * 2f;
        }

        private static ChanceActionValues Breed(ChanceActionValues value1, ChanceActionValues value2, Random rnd)
        {
            return new ChanceActionValues(value1, value2, rnd);
        }
        
        private static void CheckGetNextStatesMethod()
        {
            Console.WriteLine("How many games to play?");
            int games = GetIntegerInput();

            Random rnd = new Random();

            Stopwatch fastActionsSw = new Stopwatch();
            Stopwatch fastMovesSw = new Stopwatch();
            Stopwatch simpleSw = new Stopwatch();

            for (int i = 0; i < games; i++)
            {
                BackGammonChoiceState parentState = new BackGammonChoiceState();
                BackGammonChanceState currState = new BackGammonChanceState(parentState.GetStartingAction(rnd));

                while (parentState.IsGameOver() == false)
                {
                    fastActionsSw.Start();
                    List<GAME.Action> legalActions = currState.GetLegalActions(parentState);
                    fastActionsSw.Stop();

                    fastMovesSw.Start();
                    List<BackGammonChoiceState> resultingStates = new List<BackGammonChoiceState>(legalActions.Count);
                    for (int j = 0; j < legalActions.Count; j++)
                        resultingStates.Add((BackGammonChoiceState)currState.Move(parentState, legalActions[j]));
                    fastMovesSw.Stop();

                    simpleSw.Start();
                    List<BackGammonChoiceState> simpleResultingStates = SimpleGetAllNextStates(parentState, currState);
                    simpleSw.Stop();


                    if (HelperSpace.HelperMethods.HoldSameItems(resultingStates, simpleResultingStates) == false)
                    {
                        parentState.RotateBoard();
                        Console.WriteLine("The current state: " + parentState);
                        Console.WriteLine("The die: " + currState);

                        foreach (BackGammonChoiceState s in simpleResultingStates)
                        {
                            if (resultingStates.Contains(s) == false)
                            {
                                //s.RotateBoard();
                                Console.WriteLine("Doesn't show up in curr states: " + s);
                            }
                        }

                        foreach (BackGammonChoiceState s in resultingStates)
                        {
                            if (simpleResultingStates.Contains(s) == false)
                            {
                                //s.RotateBoard();
                                Console.WriteLine("Doesn't show up in simple states: " + s);
                            }
                        }

                        Console.WriteLine("Curr resulting states:" + HelperSpace.HelperMethods.ListToString(resultingStates));

                        Console.WriteLine("Simple resulting states:" + HelperSpace.HelperMethods.ListToString(simpleResultingStates));

                        

                        throw new Exception("Not holding the same items!");
                    }

                    parentState = simpleResultingStates[rnd.Next(simpleResultingStates.Count)];
                    currState = parentState.GetRandomNextState(rnd);
                }
            }

            Console.WriteLine("Finished playing " + games + " games, with times of:");
            Console.WriteLine($"Fast - Actions: {fastActionsSw.Elapsed} Moves: {fastMovesSw.Elapsed} Overall: {fastMovesSw.Elapsed + fastActionsSw.Elapsed} | Simple - {simpleSw.Elapsed}");
        }

        private static List<BackGammonChoiceState> SimpleGetAllNextStates(BackGammonChoiceState state, BackGammonChanceState dice)
        {
            BackGammonChoiceState copyState = new BackGammonChoiceState(state);

            List<BackGammonChoiceState> legalStates = new List<BackGammonChoiceState>();

            if (dice.Dice1 == dice.Dice2) // this is a double
            {
                legalStates = SimpleGetAllNextStatesDouble(copyState, dice.Dice1, 0, 0).Item2;
            }
            else // not a double
            {
                var result = SimpleGetAllNextStatesNonDouble(copyState, new byte[] { dice.Dice1, dice.Dice2 });

                var result2 = SimpleGetAllNextStatesNonDouble(copyState, new byte[] { dice.Dice2, dice.Dice1 });

                if (result.Item1 > result2.Item1)
                    legalStates = result.Item2;
                else if (result2.Item1 > result.Item1)
                    legalStates = result2.Item2;
                else
                {
                    legalStates = result.Item2;

                    foreach (BackGammonChoiceState checkState in result2.Item2)
                    {
                        if (legalStates.Contains(checkState) == false)
                            legalStates.Add(checkState);
                    }
                }
            }

            List<BackGammonChoiceState> actualList = new List<BackGammonChoiceState>();

            foreach (BackGammonChoiceState s in legalStates)
            {
                s.RotateBoard();
                if (actualList.Contains(s) == false)
                    actualList.Add(s);
            }

            return actualList;
        }

        private static (int, List<BackGammonChoiceState>) SimpleGetAllNextStatesDouble(BackGammonChoiceState state, byte die, int dieUsed, int indexStart)
        {
            if (dieUsed == 4)
                return (0, new List<BackGammonChoiceState>() { state } );

            if (state.myEatenCount > 0) // need to enter pieces
            {
                if (state.board[die - 1] <= -2)
                    return (0, new List<BackGammonChoiceState>() { state } );

                var board = new sbyte[24];
                for (int i = 0; i < board.Length; i++)
                    board[i] = state.board[i];

                var addValue = 0;
                if (board[die - 1] == -1)
                    addValue = 1;

                board[die - 1] = (sbyte)(Math.Max(board[die - 1], (sbyte)0) + 1);


                BackGammonChoiceState newState = new BackGammonChoiceState(board, (byte)(state.myEatenCount - 1), (byte)(state.enemyEatenCount + addValue));

                var result = SimpleGetAllNextStatesDouble(newState, die, dieUsed + 1, 0);

                return (result.Item1 + 1, result.Item2);
            }
            else
            {
                if (state.myPieces.Count == 0)
                    return (0, new List<BackGammonChoiceState>() { state });

                List<BackGammonChoiceState> nextStates = new List<BackGammonChoiceState>();

                int highestDepth = 0;
                bool canLeaveBoard = state.myPieces[0] >= 18;

                for (; indexStart < state.myPieces.Count; indexStart++)
                {
                    var index = state.myPieces[indexStart];
                    
                    if (index + die >= 24) // trying to leave the board
                    {
                        if (canLeaveBoard && (index + die == 24 || indexStart == 0))
                        {
                            var board = new sbyte[24];
                            for (var i = 0; i < board.Length; i++)
                                board[i] = state.board[i];
                            board[index]--;

                            BackGammonChoiceState newState = new BackGammonChoiceState(board, 0, state.enemyEatenCount);

                            var result = SimpleGetAllNextStatesDouble(newState, die, dieUsed + 1, indexStart);
                            if (result.Item1 > highestDepth)
                            {
                                highestDepth = result.Item1;
                                nextStates.Clear();
                            }
                            if (result.Item1 >= highestDepth)
                            {
                                nextStates.AddRange(result.Item2);
                            }
                        }
                        else
                            break;
                    }
                    else if (state.board[index + die] >= -1) // can i even move?
                    {
                        var board = new sbyte[24];
                        for (var i = 0; i < board.Length; i++)
                            board[i] = state.board[i];

                        var addValue = 0;
                        if (board[index + die] == -1)
                            addValue = 1;

                        board[index]--;
                        board[index + die] = (sbyte)(Math.Max(board[index + die], (sbyte)0) + 1);

                        BackGammonChoiceState newState = new BackGammonChoiceState(board, 0, (byte)(state.enemyEatenCount + addValue));

                        var result = SimpleGetAllNextStatesDouble(newState, die, dieUsed + 1, indexStart);
                        if (result.Item1 > highestDepth)
                        {
                            highestDepth = result.Item1;
                            nextStates.Clear();
                        }
                        if (result.Item1 >= highestDepth)
                        {
                            nextStates.AddRange(result.Item2);
                        }
                    }
                }

                if (nextStates.Count == 0) // no moves possible, so the next state is the same state
                {
                    nextStates.Add(state);
                    return (highestDepth, nextStates);
                }
                return (highestDepth + 1, nextStates);
            }
        }

        private static (int, List<BackGammonChoiceState>) SimpleGetAllNextStatesNonDouble(BackGammonChoiceState state, byte[] dice)
        {
            if (dice.Length == 0)
                return (0, new List<BackGammonChoiceState>() { state });

            byte[] newDie = new byte[dice.Length - 1];
            for (int i = 1; i < dice.Length; i++)
                newDie[i - 1] = dice[i];

            if (state.myEatenCount > 0) // need to enter the board
            {
                if (state.board[dice[0] - 1] <= -2) // can't enter from the board
                    return (0, new List<BackGammonChoiceState>() { state });

                var board = new sbyte[24];
                for (int i = 0; i < board.Length; i++)
                    board[i] = state.board[i];

                var addValue = 0;
                if (board[dice[0] - 1] == -1)
                    addValue = 1;
                
                board[dice[0] - 1] = (sbyte)(Math.Max(board[dice[0] - 1], (sbyte)0) + 1);


                BackGammonChoiceState newState = new BackGammonChoiceState(board, (byte)(state.myEatenCount - 1), (byte)(state.enemyEatenCount + addValue));
                
                var result = SimpleGetAllNextStatesNonDouble(newState, newDie);

                return (result.Item1 + 1, result.Item2);
            }
            else
            {
                if (state.myPieces.Count == 0)
                    return (0, new List<BackGammonChoiceState>() { state });

                List<BackGammonChoiceState> nextStates = new List<BackGammonChoiceState>();

                int highestDepth = 0;
                bool canLeaveBoard = state.myPieces[0] >= 18;

                byte die = dice[0];

                for (int i = 0; i < state.myPieces.Count; i++)
                {
                    var index = state.myPieces[i];

                    if (index + die >= 24) // trying to leave the board
                    {
                        if (canLeaveBoard && (index + die == 24 || i == 0))
                        {
                            var board = new sbyte[24];
                            for (var j = 0; j < board.Length; j++)
                                board[j] = state.board[j];
                            board[index]--;

                            BackGammonChoiceState newState = new BackGammonChoiceState(board, 0, state.enemyEatenCount);

                            var result = SimpleGetAllNextStatesNonDouble(newState, newDie);

                            if (result.Item1 > highestDepth)
                            {
                                highestDepth = result.Item1;
                                nextStates.Clear();
                            }
                            if (result.Item1 >= highestDepth)
                            {
                                nextStates.AddRange(result.Item2);
                            }
                        }
                        else
                            break;
                    }
                    else if (state.board[index + die] >= -1) // can i even move?
                    {
                        var board = new sbyte[24];
                        for (var j = 0; j < board.Length; j++)
                            board[j] = state.board[j];

                        var addValue = 0;
                        if (board[index + die] == -1)
                            addValue = 1;

                        board[index]--;
                        board[index + die] = (sbyte)(Math.Max(board[index + die], (sbyte)0) + 1);

                        BackGammonChoiceState newState = new BackGammonChoiceState(board, 0, (byte)(state.enemyEatenCount + addValue));

                        var result = SimpleGetAllNextStatesNonDouble(newState, newDie);

                        if (result.Item1 > highestDepth)
                        {
                            highestDepth = result.Item1;
                            nextStates.Clear();
                        }
                        if (result.Item1 >= highestDepth)
                        {
                            nextStates.AddRange(result.Item2);
                        }
                    }
                }

                if (nextStates.Count == 0) // no moves possible, so the next state is the same state
                {
                    nextStates.Add(state);
                    return (highestDepth, nextStates);
                }
                return (highestDepth + 1, nextStates);
            }
        }
    }
}