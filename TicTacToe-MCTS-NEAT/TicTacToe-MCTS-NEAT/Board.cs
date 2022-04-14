using System.Collections.Generic;
using System;
using HelperSpace;
using BackGammonUser;

namespace GAME
{
    public abstract class Action
    {
        public abstract MessageType messageType
        {
            get;
        }

        public virtual float GetScore(State state) { return 1; } // defualt score for choosing turn

        public abstract override string ToString();

        public abstract override bool Equals(object obj);

        public abstract string ProtocolInformation();

        public abstract Action Copy();
    }

    public abstract class State
    {
        public abstract MessageType messageType
        {
            get;
        }

        public virtual bool IsChanceState() => false;

        public abstract State Copy();

        public abstract List<Action> GetLegalActions(State prevState);

        public abstract State Move(State prevState, Action action);

        public abstract float[] GetNetworkInput();

        public abstract bool IsGameOver();

        public abstract int GameResult();

        public abstract Action RandomPick(List<Action> legalActions, Random rnd);
        
        public abstract int RandomPick(int listSize, Random rnd);

        public abstract string ProtocolInformation();

        public abstract override bool Equals(object obj);

        public abstract override string ToString();
    }
     
    public class BackGammonChoiceState : State
    {
        public override MessageType messageType
        {
            get
            {
                return MessageType.ChoiceState;
            }
        }

        private Action[] allActions = new Action[]
                                           { new Dice(1, 1), new Dice(2, 2), new Dice(3, 3), new Dice(4, 4), new Dice(5, 5), new Dice(6, 6), new Dice(1, 2), new Dice(1, 3),
                                             new Dice(1, 4), new Dice(1, 5), new Dice(1, 6), new Dice(2, 3), new Dice(2, 4), new Dice(2, 5), new Dice(2, 6), new Dice(3, 4),
                                             new Dice(3, 5), new Dice(3, 6), new Dice(4, 5), new Dice(4, 6), new Dice(5, 6) };
        
        public sbyte[] board;
        public List<byte> myPieces;
        public List<byte> enemyPieces;
        public byte myEatenCount;
        public byte enemyEatenCount;

        public BackGammonChoiceState()
        {
            board = new sbyte[] { 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, -5, 0, 0, 0, 3, 0, 5, 0, 0, 0, 0, -2 };
            myPieces = new List<byte>() { 0, 11, 16, 18 };
            enemyPieces = new List<byte>() { 5, 7, 12, 23 };
            myEatenCount = 0;
            enemyEatenCount = 0;
        }

        public BackGammonChoiceState(sbyte[] board, byte myEatenCount, byte enemyEatenCount)
        {
            this.myEatenCount = myEatenCount;
            this.enemyEatenCount = enemyEatenCount;

            this.board = board;
            myPieces = new List<byte>();
            enemyPieces = new List<byte>();

            for (byte i = 0; i < board.Length; i++)
            {
                if (board[i] > 0)
                    myPieces.Add(i);
                else if (board[i] < 0)
                    enemyPieces.Add(i);
            }
        }

        public BackGammonChoiceState(sbyte[] board, byte myEatenCount, byte enemyEatenCount, List<byte> myPieces, List<byte> enemyPieces)
        {
            this.board = board;
            this.myEatenCount = myEatenCount;
            this.enemyEatenCount = enemyEatenCount;
            this.myPieces = myPieces;
            this.enemyPieces = enemyPieces;
        }

        public BackGammonChoiceState(BackGammonChoiceState copy)
        {
            this.board = new sbyte[24];
            for (byte i = 0; i < board.Length; i++)
                this.board[i] = copy.board[i];

            this.myPieces = new List<byte>(copy.myPieces.Capacity);
            for (int i = 0; i < copy.myPieces.Count; i++)
                myPieces.Add(copy.myPieces[i]);


            this.enemyPieces = new List<byte>(copy.enemyPieces.Capacity);
            for (int i = 0; i < copy.enemyPieces.Count; i++)
                enemyPieces.Add(copy.enemyPieces[i]);

            this.myEatenCount = copy.myEatenCount;
            this.enemyEatenCount = copy.enemyEatenCount;
        }

        public override State Copy()
        {
            return new BackGammonChoiceState(this);
        }

        public bool IsEndGame()
        {
            if (myEatenCount == 0 && (myPieces.Count == 0 || myPieces[0] >= 18))
                return true;
            return false;
        }

        public void RotateBoard()
        {
            sbyte[] newBoard = new sbyte[24];
            for (int i = 0; i < 24; i++)
                newBoard[i] = (sbyte)-board[23 - i];

            board = newBoard;

            byte temp = enemyEatenCount;
            enemyEatenCount = myEatenCount;
            myEatenCount = temp;

            List<byte> newEnemyPositions = new List<byte>();
            for (int i = myPieces.Count - 1; i >= 0; i--)
                newEnemyPositions.Add((byte)(23 - myPieces[i]));

            List<byte> newMyPositions = new List<byte>();
            for (int i = enemyPieces.Count - 1; i >= 0; i--)
                newMyPositions.Add((byte)(23 - enemyPieces[i]));

            enemyPieces = newEnemyPositions;
            myPieces = newMyPositions;
        }

        public Action GetStartingAction(Random rnd)
        {
            return allActions[HelperMethods.RandomValue(6, allActions.Length, rnd)];
        }

        public override List<Action> GetLegalActions(State prevState)
        {
            return new List<Action>(allActions);
        }

        public override State Move(State prevState, Action action)
        {
            return new BackGammonChanceState(action);
        }

        public override float[] GetNetworkInput()
        {
            float[] networkInput = new float[26];
            for (int i = 0; i < 24; i++)
                networkInput[i] = board[i];
            networkInput[24] = myEatenCount;
            networkInput[25] = enemyEatenCount;
            return networkInput;
        }

        public override bool IsGameOver()
        {
            // no need to check my pieces, since the guy who wins will always be the opposite of current turn ( other guy won then now its my turn )
            if (enemyPieces.Count != 0 || enemyEatenCount != 0)
                return false;
            
            return true;
        }

        public override int GameResult()
        {
            //return -1;

            int count = 0;
            for (int i = 0; i < myPieces.Count; i++)
                count += board[myPieces[i]];

            if (count + myEatenCount == 15) // i lost without getting any pieces out
                return -2; // technically could also be -3 but since it rarely happens leave it...

            return -1; // i always lost if the game is over
        }

        public int ActualGameScoreResult()
        {
            int countOfPieces = 0;
            for (int i = 0; i < myPieces.Count; i++)
                countOfPieces += board[myPieces[i]];

            if (countOfPieces + myEatenCount == 15) // did enemy get out with pieces?
            {
                // he didn't, is there an eaten piece / a piece at my last 6 point?
                if (myEatenCount == 0 && myPieces[0] >= 6)
                    return 2; // a gammon win
                return 3; // a backgammon win
            }
            return 1; // just a regular win
        }

        public override Action RandomPick(List<Action> legalActions, Random rnd)
        {
            if (HelperMethods.RandomValue(0, 6, rnd) == 0) // pick from doubles?
                return legalActions[HelperMethods.RandomValue(0, 6, rnd)];
            else // pick from non doubles
                return legalActions[HelperMethods.RandomValue(6, 21, rnd)];
        }

        public override int RandomPick(int listSize, Random rnd)
        {
            if (HelperMethods.RandomValue(0, 6, rnd) == 0) // pick from doubles?
                return HelperMethods.RandomValue(0, 6, rnd);
            else
                return HelperMethods.RandomValue(6, 21, rnd);
        }

        public override bool Equals(object obj)
        {
            if (obj is BackGammonChoiceState)
            {
                BackGammonChoiceState other = (BackGammonChoiceState)obj;

                if (myPieces.Count != other.myPieces.Count || enemyPieces.Count != other.enemyPieces.Count ||
                    myEatenCount != other.myEatenCount || enemyEatenCount != other.enemyEatenCount)
                    return false;

                for (int i = 0; i < 24; i++)
                    if (board[i] != other.board[i])
                        return false;

                for (int i = 0; i < myPieces.Count; i++)
                    if (myPieces[i] != other.myPieces[i])
                        return false;

                for (int i = 0; i < enemyPieces.Count; i++)
                    if (enemyPieces[i] != other.enemyPieces[i])
                        return false;

                return true;
            }

            return false;
        }

        public override string ToString()
        {
            int pad = 3;
            string s = "Board:\r\n";

            List<int> count = new List<int>();
            int max = 0;

            for (int i = 0; i < board.Length / 2; i++)
            {
                if (i == board.Length / 4 - 1)
                    s += (i).ToString().PadRight(pad * 2);
                else
                    s += (i).ToString().PadRight(pad);
                count.Add(board[i]);
                max = Math.Max(Math.Abs(board[i]), max);
            }

            s += "\r\n";

            for (int i = -1; i < board.Length / 2; i++)
            {
                s += ('=').ToString().PadRight(pad);
            }

            s += "\r\n";

            for (int i = 0; i < max; i++)
            {
                for (int j = 0; j < count.Count; j++)
                {
                    if (count[j] > 0)
                    {
                        s += "O".PadRight(pad);
                        count[j] -= 1;
                    }
                    else if (count[j] < 0)
                    {
                        s += "X".PadRight(pad);
                        count[j] += 1;
                    }
                    else
                    {
                        s += "".PadRight(pad);
                    }

                    if (j == count.Count / 2 - 1)
                    {
                        s += "|".PadRight(pad);
                    }
                }

                s += "\r\n";
            }

            s += "\r\n";
            count.Clear();

            for (int i = board.Length - 1; i >= board.Length / 2; i--)
            {
                count.Add(board[i]);
                max = Math.Max(Math.Abs(board[i]), max);
            }

            while (max > 0)
            {
                for (int i = 0; i < count.Count; i++)
                {
                    if (Math.Abs(count[i]) == max)
                    {
                        if (count[i] > 0)
                        {
                            s += "O".PadRight(pad);
                            count[i] -= 1;
                        }
                        else if (count[i] < 0)
                        {
                            s += "X".PadRight(pad);
                            count[i] += 1;
                        }
                    }
                    else
                    {
                        s += "".PadRight(pad);
                    }

                    if (i == count.Count / 2 - 1)
                    {
                        s += "|".PadRight(pad);
                    }
                }

                max--;
                s += "\r\n";
            }

            for (int i = board.Length; i >= board.Length / 2; i--)
            {
                s += ("=").ToString().PadRight(pad);
            }

            s += "\r\n";

            for (int i = board.Length - 1; i >= board.Length / 2; i--)
            {
                if (i == board.Length / 4 * 3)
                    s += (i).ToString().PadRight(pad * 2);
                else
                    s += (i).ToString().PadRight(pad);
            }

            s += "\r\n";

            s += "My pieces: " + HelperMethods.ListToString(myPieces) + "\r\n";

            s += "Enemy pieces: " + HelperMethods.ListToString(enemyPieces) + "\r\n";

            s += $"Eaten == [{myEatenCount},{enemyEatenCount}]";
            return s;            
        }

        public override string ProtocolInformation()
        {
            string info = "";

            for (int i = 0; i < board.Length; i++)
                info += board[i].ToString() + ",";
            
            info += myEatenCount.ToString() + "/" + enemyEatenCount.ToString();

            return info; // will look like: "1,0,-2,5...,0/1"
        }

        public static BackGammonChoiceState PorotocolInformation(string state)
        {
            Console.WriteLine(state);
            sbyte[] board = new sbyte[24];

            string[] stateParts = state.Split(',');

            for (int i = 0; i < 24; i++)
                board[i] = sbyte.Parse(stateParts[i]);

            byte myEatenCount = byte.Parse(stateParts[24][0].ToString());
            byte enemyEatenCount = byte.Parse(stateParts[24][2].ToString());

            return new BackGammonChoiceState(board, myEatenCount, enemyEatenCount);
        }
    }

    public class Dice : Action
    {
        public byte dice1 { get; private set; }
        public byte dice2 { get; private set; }

        public override MessageType messageType
        {
            get
            {
                return MessageType.ChoiceAction;
            }
        }

        public Dice(byte dice1, byte dice2)
        {
            this.dice1 = dice1;
            this.dice2 = dice2;
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is Dice)
            {
                Dice other = (Dice)obj;
                if (other.dice1 == dice1 && other.dice2 == dice2)
                    return true;
            }
            return false;
        }

        public override Action Copy()
        {
            return new Dice(dice1, dice2);
        }
        
        public override string ProtocolInformation()
        {
            return $"{dice1.ToString()}{dice2.ToString()}";
        }

        public static Dice PorotocolInformation(string action)
        {
            byte dice1 = byte.Parse(action[0].ToString());
            byte dice2 = byte.Parse(action[1].ToString());

            return new Dice(dice1, dice2);
        }

        public override string ToString()
        {
            return $"d1: {dice1} d2: {dice2}";
        }
    }

    public class BackGammonChanceState : State
    {
        private Dice dice;

        public byte Dice1 { get { return dice.dice1; } }
        public byte Dice2 { get { return dice.dice2; } }

        public override MessageType messageType
        {
            get { return MessageType.ChanceState; }
        }

        public BackGammonChanceState(Action action)
        {
            this.dice = (Dice)action;
        }

        public override State Copy()
        {
            return new BackGammonChanceState(new Dice(dice.dice1, dice.dice2));
        }

        public override bool IsChanceState() => true;


        public static int VersionDifferenceCount = 0;
        public override List<Action> GetLegalActions(State prevState)
        {
            List<Action> legalActions = GetLegalActionsInside(prevState);
            if (legalActions.Count == 0)
                legalActions.Add(new BackGammonChanceAction());

            //List<Action> legalActionsV2 = GetLegalActionsInsideV2(prevState);
            //if (legalActionsV2.Count == 0)
            //    legalActionsV2.Add(new BackGammonChanceAction());

            //if (HelperMethods.HoldSameItems(legalActions, legalActionsV2) == false)
            //{
            //    Console.WriteLine("The board with the problem:\n" + prevState);
            //    Console.WriteLine("Dice: " + dice);
            //    Console.WriteLine($"Current legal actions {legalActions.Count}:\n{HelperMethods.ListToString(legalActions)}");
            //    Console.WriteLine($"new legal actions {legalActionsV2.Count}:\n{HelperMethods.ListToString(legalActionsV2)}");

            //    throw new Exception("They are not the same!");
            //    VersionDifferenceCount++;
            //}

            //List<(BackGammonChoiceState, Action)> newStates = new List<(BackGammonChoiceState, Action)>();
            //foreach (Action action in legalActions)
            //{
            //    BackGammonChoiceState checkNewState = (BackGammonChoiceState)Move(prevState, action);
            //    bool found = false;
            //    foreach ((BackGammonChoiceState, Action) newState in newStates)
            //        if (checkNewState.Equals(newState.Item1))
            //        {
            //            found = true;
            //            Console.WriteLine("There is duplicate: " + action + " other action: " + newState.Item2);
            //            break;
            //        }

            //    if (!found)
            //        newStates.Add((checkNewState, action));
            //}

            return legalActions;
        }

        public bool IsLegalMove(State prevState, Action action)
        {
            if (action is BackGammonChanceAction == false)
                return false;

            List<Action> legalActions = GetLegalActions(prevState);

            State resultingState = Move(prevState, action);

            for (int i = 0; i < legalActions.Count; i++)
            {
                if (Move(prevState, legalActions[i]).Equals(resultingState))
                    return true;
            }
            return false;
        }

        private List<Action> GetLegalActionsInside(State prevState)
        {
            BackGammonChoiceState state = new BackGammonChoiceState((BackGammonChoiceState)prevState);
            List<Action> legalActions = new List<Action>();

            // Supposedly finished all the double options, didn't yet do non double bearing off...
            if (dice.dice1 == dice.dice2) // this is a double!
            {
                byte dCount = 4;

                if (state.myEatenCount > 0) // i need to remove from eaten pieces
                {
                    if (state.board[dice.dice1 - 1] < -1)
                        return legalActions; // return empty list, since i cant move at all
                    else
                    {
                        dCount -= Math.Min((byte)4, state.myEatenCount);

                        switch (state.board[dice.dice1 - 1]) // what is on the position i want to enter?
                        {
                            case -1: // an enemy piece
                                state.board[dice.dice1 - 1] = (sbyte)(4 - dCount);
                                state.enemyPieces.Remove((byte)(dice.dice1 - 1)); // remove it since it no longer going to be there
                                for (int i = 0; i < state.myPieces.Count; i++) // add this piece to pieces list
                                    if (state.myPieces[i] >= dice.dice1) // if i passed the current index
                                    {
                                        state.myPieces.Insert(i, (byte)(dice.dice1 - 1));
                                        break;
                                    }
                                break;
                            case 0: // empty 
                                state.board[dice.dice1 - 1] = (sbyte)(4 - dCount);
                                for (int i = 0; i < state.myPieces.Count; i++) // add this piece to pieces list
                                    if (state.myPieces[i] >= dice.dice1) // if i passed the current index
                                    {
                                        state.myPieces.Insert(i, (byte)(dice.dice1 - 1));
                                        break;
                                    }
                                break;
                            default: // i have my pieces
                                state.board[dice.dice1 - 1] += (sbyte)(4 - dCount);
                                break;
                        }
                    }
                }

                // list of all towers able to be used, and also the amount of times they can be used...
                //List<(byte, byte)> towerTable = new List<(byte, byte)>();

                List<byte> towersListV2 = new List<byte>();

                Dictionary<int, List<Action>> towerActionsDict = new Dictionary<int, List<Action>>();

                int maxPlay = 0;
                foreach (byte b in state.myPieces)
                {
                    byte steps = 0;
                    for (byte i = (byte)(b + dice.dice1); i < 24 && state.board[i] >= -1 && steps < dCount; i += dice.dice1)
                        steps += (byte)state.board[b];
                    if (steps != 0)
                    {
                        maxPlay += steps;
                        steps = Math.Min(steps, dCount);
                        //towerTable.Add((b, steps));

                        for (int i = 1; i <= steps; i++)
                        {
                            towerActionsDict.Add(i * 100 + b, GetActions((sbyte)b, (byte)i, dice.dice1, (byte)state.board[b], state));
                            towersListV2.Add(b);
                        }
                    }
                }


                maxPlay = Math.Min(maxPlay, dCount);

                maxPlay += 4 - dCount; // the max number of moves possible

                List<List<(byte, byte)>> towerMoves2 = TowerMovesListV2(towersListV2, dCount);

                legalActions = GetActionList(towerMoves2, towerActionsDict);
                if (legalActions.Count == 0)
                    legalActions.Add(new BackGammonChanceAction(new List<(sbyte, sbyte)>(4)));

                if (dCount != 4)
                {
                    foreach (Action action in legalActions)
                    {
                        BackGammonChanceAction ca = (BackGammonChanceAction)action;

                        for (int i = 0; i < 4 - dCount; i++)
                            ca.AddToAction((-1, (sbyte)(dice.dice1 - 1)));
                    }
                }
                else // there is a chance, that i could have taken out a piece but didn't
                {
                    BackGammonChanceAction preBearingAction = new BackGammonChanceAction();

                    // move all pieces to the enemy board, so that i can start bearing off...
                    while (state.myPieces[0] < 18)
                    {
                        int indexFrom = state.myPieces[0];
                        int indexTo = indexFrom + dice.dice1;
                        if (state.board[indexFrom] >= dCount || state.board[indexTo] < -1)
                            return legalActions;

                        dCount -= (byte)state.board[indexFrom];
                        state.myPieces.RemoveAt(0);

                        if (state.board[indexTo] > 0)
                        {
                            state.board[indexTo] += state.board[indexFrom];
                        }
                        else
                        {
                            state.board[indexTo] = state.board[indexFrom];
                            HelperMethods.InsertToList(state.myPieces, (byte)indexTo);
                        }

                        for (int j = 0; j < state.board[indexFrom]; j++)
                            preBearingAction.AddToAction(((sbyte)indexFrom, (sbyte)indexTo));

                        state.board[indexFrom] = 0;
                    }
                    

                    // if im here, it means all my pieces are in the enemy base, and i have at least 1 more action to make
                    // i need to find all options where at least 1 piece got off the board (otherwise it will be a duplicate of the already existing actions)

                    List<BackGammonChanceAction> endGameActions = EndGameDoubleActions(state.board, dice.dice1, dCount);

                    foreach (BackGammonChanceAction act in endGameActions)
                    {
                        if (act.Count + preBearingAction.Count == maxPlay) // it is equal length (so either i did a duplicate move, or i beared off)
                        {
                            if (act.IsBearingOff()) // only if i beared off the board do i know its not a duplicate move
                            {
                                act.AddToAction(preBearingAction);
                                legalActions.Add(act);
                            }
                        }
                        else if (act.Count + preBearingAction.Count > maxPlay) // if its bigger, i know i beared off from the board 100% so no need to check that
                        {
                            act.AddToAction(preBearingAction);
                            legalActions.Clear();
                            legalActions.Add(act);
                            maxPlay = act.Count;
                        }
                    }
                }
            }
            else // not a double, find moves regularly
            {
                if (state.myEatenCount == 0)
                {
                    Regular2PieceMove(legalActions, state, dice);
                }
                else if (state.myEatenCount >= 2)
                {
                    if (state.board[dice.dice1 - 1] >= -1)
                    {
                        if (state.board[dice.dice2 - 1] >= -1)
                            legalActions.Add(new BackGammonChanceAction(new List<(sbyte, sbyte)>(2)
                                            { (-1, (sbyte)(dice.dice1 - 1)), (-1, (sbyte)(dice.dice2 - 1))}));
                        else
                            legalActions.Add(new BackGammonChanceAction(new List<(sbyte, sbyte)>(1)
                                            { (-1, (sbyte)(dice.dice1 - 1))}));
                    }
                    else if (state.board[dice.dice2 - 1] >= -1)
                        legalActions.Add(new BackGammonChanceAction(new List<(sbyte, sbyte)>(1)
                                            { (-1, (sbyte)(dice.dice2 - 1))}));
                }
                else // i have 1 eaten
                {
                    bool added = false;
                    void RegularOneEaten(byte die1, byte die2, bool isFirst)
                    {
                        if (state.board[die1 - 1] >= -1)
                        {
                            // i can only move with the piece that entered if its clear && (im first || at least one enter option isn't clear)
                            if (state.board[die1 + die2 - 1] >= -1 && (isFirst || state.board[die1 - 1] <= -1 || state.board[die2 - 1] <= -1))
                            {
                                if (!added)
                                    legalActions.Clear();

                                legalActions.Add(new BackGammonChanceAction(new List<(sbyte, sbyte)>(2)
                                                { (-1, (sbyte)(die1 - 1)), ((sbyte)(die1 - 1), (sbyte)(die1 + die2 -1))}));
                                added = true;
                            }

                            foreach (byte b in state.myPieces)
                            {
                                if (b + die2 >= 24)
                                    break;
                                if (b != die1 - 1 && state.board[b + die2] >= -1)
                                {
                                    if (!added)
                                        legalActions.Clear();

                                    legalActions.Add(new BackGammonChanceAction(new List<(sbyte, sbyte)>(2)
                                                    { (-1, (sbyte)(die1 - 1)), ((sbyte)b, (sbyte)(b + die2))}));
                                    added = true;
                                }
                            }
                            if (!added)
                                legalActions.Add(new BackGammonChanceAction(new List<(sbyte, sbyte)>(1)
                                            { (-1, (sbyte)(die1 - 1))}));
                        }
                    }
                    RegularOneEaten(dice.dice1, dice.dice2, true);
                    RegularOneEaten(dice.dice2, dice.dice1, false);
                }
            }

            return legalActions;
        }

        // currently is a copy of "GetLegalActionsInside"
        private List<Action> GetLegalActionsInsideV2(State prevState)
        {
            BackGammonChoiceState state = new BackGammonChoiceState((BackGammonChoiceState)prevState);
            List<Action> legalActions = new List<Action>();

            // Supposedly finished all the double options, didn't yet do non double bearing off...
            if (dice.dice1 == dice.dice2) // this is a double!
            {
                byte dCount = 4;

                if (state.myEatenCount > 0) // i need to remove from eaten pieces
                {
                    if (state.board[dice.dice1 - 1] < -1)
                        return legalActions; // return empty list, since i cant move at all
                    else
                    {
                        dCount -= Math.Min((byte)4, state.myEatenCount);

                        switch (state.board[dice.dice1 - 1]) // what is on the position i want to enter?
                        {
                            case -1: // an enemy piece
                                state.board[dice.dice1 - 1] = (sbyte)(4 - dCount);
                                state.enemyPieces.Remove((byte)(dice.dice1 - 1)); // remove it since it no longer going to be there
                                for (int i = 0; i < state.myPieces.Count; i++) // add this piece to pieces list
                                    if (state.myPieces[i] >= dice.dice1) // if i passed the current index
                                    {
                                        state.myPieces.Insert(i, (byte)(dice.dice1 - 1));
                                        break;
                                    }
                                break;
                            case 0: // empty 
                                state.board[dice.dice1 - 1] = (sbyte)(4 - dCount);
                                for (int i = 0; i < state.myPieces.Count; i++) // add this piece to pieces list
                                    if (state.myPieces[i] >= dice.dice1) // if i passed the current index
                                    {
                                        state.myPieces.Insert(i, (byte)(dice.dice1 - 1));
                                        break;
                                    }
                                break;
                            default: // i have my pieces
                                state.board[dice.dice1 - 1] += (sbyte)(4 - dCount);
                                break;
                        }
                    }
                }

                // list of all towers able to be used, and also the amount of times they can be used...
                //List<(byte, byte)> towerTable = new List<(byte, byte)>();

                List<byte> towersListV2 = new List<byte>();

                Dictionary<int, List<Action>> towerActionsDict = new Dictionary<int, List<Action>>();

                int maxPlay = 0;
                foreach (byte b in state.myPieces)
                {
                    byte steps = 0;
                    for (byte i = (byte)(b + dice.dice1); i < 24 && state.board[i] >= -1 && steps < dCount; i += dice.dice1)
                        steps += (byte)state.board[b];
                    if (steps != 0)
                    {
                        maxPlay += steps;
                        steps = Math.Min(steps, dCount);
                        //towerTable.Add((b, steps));

                        for (int i = 1; i <= steps; i++)
                        {
                            towerActionsDict.Add(i * 100 + b, GetActions((sbyte)b, (byte)i, dice.dice1, (byte)state.board[b], state));
                            towersListV2.Add(b);
                        }
                    }
                }


                maxPlay = Math.Min(maxPlay, dCount);

                maxPlay += 4 - dCount; // the max number of moves possible

                List<List<(byte, byte)>> towerMoves2 = TowerMovesListV2(towersListV2, dCount);

                legalActions = GetActionList(towerMoves2, towerActionsDict);
                if (legalActions.Count == 0)
                    legalActions.Add(new BackGammonChanceAction(new List<(sbyte, sbyte)>(4)));

                if (dCount != 4)
                {
                    foreach (Action action in legalActions)
                    {
                        BackGammonChanceAction ca = (BackGammonChanceAction)action;

                        for (int i = 0; i < 4 - dCount; i++)
                            ca.AddToAction((-1, (sbyte)(dice.dice1 - 1)));
                    }
                }
                else // there is a chance, that i could have taken out a piece but didn't
                {
                    BackGammonChanceAction preBearingAction = new BackGammonChanceAction();

                    // move all pieces to the enemy board, so that i can start bearing off...
                    while (state.myPieces[0] < 18)
                    {
                        int indexFrom = state.myPieces[0];
                        int indexTo = indexFrom + dice.dice1;
                        if (state.board[indexFrom] >= dCount || state.board[indexTo] < -1)
                            return legalActions;

                        dCount -= (byte)state.board[indexFrom];
                        state.myPieces.RemoveAt(0);

                        if (state.board[indexTo] > 0)
                        {
                            state.board[indexTo] += state.board[indexFrom];
                        }
                        else
                        {
                            state.board[indexTo] = state.board[indexFrom];
                            HelperMethods.InsertToList(state.myPieces, (byte)indexTo);
                        }

                        for (int j = 0; j < state.board[indexFrom]; j++)
                            preBearingAction.AddToAction(((sbyte)indexFrom, (sbyte)indexTo));

                        state.board[indexFrom] = 0;
                    }


                    // if im here, it means all my pieces are in the enemy base, and i have at least 1 more action to make
                    // i need to find all options where at least 1 piece got off the board (otherwise it will be a duplicate of the already existing actions)

                    List<BackGammonChanceAction> endGameActions = EndGameDoubleActions(state.board, dice.dice1, dCount);

                    foreach (BackGammonChanceAction act in endGameActions)
                    {
                        if (act.Count + preBearingAction.Count == maxPlay) // it is equal length (so either i did a duplicate move, or i beared off)
                        {
                            if (act.IsBearingOff()) // only if i beared off the board do i know its not a duplicate move
                            {
                                act.AddToAction(preBearingAction);
                                legalActions.Add(act);
                            }
                        }
                        else if (act.Count + preBearingAction.Count > maxPlay) // if its bigger, i know i beared off from the board 100% so no need to check that
                        {
                            act.AddToAction(preBearingAction);
                            legalActions.Clear();
                            legalActions.Add(act);
                            maxPlay = act.Count;
                        }
                    }
                }
            }
            else // not a double, find moves regularly
            {
                if (state.myEatenCount == 0)
                {
                    Regular2PieceMove(legalActions, state, dice);
                }
                else if (state.myEatenCount >= 2)
                {
                    if (state.board[dice.dice1 - 1] >= -1)
                    {
                        if (state.board[dice.dice2 - 1] >= -1)
                            legalActions.Add(new BackGammonChanceAction(new List<(sbyte, sbyte)>(2)
                                            { (-1, (sbyte)(dice.dice1 - 1)), (-1, (sbyte)(dice.dice2 - 1))}));
                        else
                            legalActions.Add(new BackGammonChanceAction(new List<(sbyte, sbyte)>(1)
                                            { (-1, (sbyte)(dice.dice1 - 1))}));
                    }
                    else if (state.board[dice.dice2 - 1] >= -1)
                        legalActions.Add(new BackGammonChanceAction(new List<(sbyte, sbyte)>(1)
                                            { (-1, (sbyte)(dice.dice2 - 1))}));
                }
                else // i have 1 eaten
                {
                    bool added = false;
                    void RegularOneEaten(byte die1, byte die2, bool isFirst)
                    {
                        if (state.board[die1 - 1] >= -1)
                        {
                            // i can only move with the piece that entered if its clear && (im first || at least one enter option isn't clear)
                            if (state.board[die1 + die2 - 1] >= -1 && (isFirst || state.board[die1 - 1] <= -1 || state.board[die2 - 1] <= -1))
                            {
                                if (!added)
                                    legalActions.Clear();

                                legalActions.Add(new BackGammonChanceAction(new List<(sbyte, sbyte)>(2)
                                                { (-1, (sbyte)(die1 - 1)), ((sbyte)(die1 - 1), (sbyte)(die1 + die2 -1))}));
                                added = true;
                            }

                            foreach (byte b in state.myPieces)
                            {
                                if (b + die2 >= 24)
                                    break;
                                if (b != die1 - 1 && state.board[b + die2] >= -1)
                                {
                                    if (!added)
                                        legalActions.Clear();

                                    legalActions.Add(new BackGammonChanceAction(new List<(sbyte, sbyte)>(2)
                                                    { (-1, (sbyte)(die1 - 1)), ((sbyte)b, (sbyte)(b + die2))}));
                                    added = true;
                                }
                            }
                            if (!added)
                                legalActions.Add(new BackGammonChanceAction(new List<(sbyte, sbyte)>(1)
                                            { (-1, (sbyte)(die1 - 1))}));
                        }
                    }
                    RegularOneEaten(dice.dice1, dice.dice2, true);
                    RegularOneEaten(dice.dice2, dice.dice1, false);
                }
            }

            return legalActions;
        }

        private void Regular2PieceMove(List<Action> legalActions, BackGammonChoiceState state, Dice dice)
        {
            bool isEndGame = state.myPieces[0] >= 18;
            bool addedTwo = false;

            bool[] willCreateDuplicate = new bool[state.myPieces.Count]; // everything is false

            // go over all the pieces, and move each time from a different die
            // then go over all the pieces after that piece and move it with the other die

            // there are some duplicates this way:
            // 1 - don't move twice from same index in both die options
            // 2 - moving twice with the same piece, can be done with (index + 5 + 6, index + 6 + 5)
            // but its only a duplicate if the position index + 5 && index + 6 >= 0
            // 3 - if (index + 5, and (index + 1) + 6) both get the pieces out, then it can create a duplicates

            void OneDie(BackGammonChoiceState currState, int j, byte die, (sbyte, sbyte) prevMove, bool isFirstPiece, bool isEndGame2)
            {
                for (; j < currState.myPieces.Count; j++)
                {
                    sbyte currIndex = (sbyte)currState.myPieces[j];
                    if (currIndex + die < 24)
                    {
                        isFirstPiece = false;
                        if (currState.board[currIndex + die] >= -1)
                        {
                            if (!addedTwo)
                                legalActions.Clear();
                            legalActions.Add(new BackGammonChanceAction(new (sbyte, sbyte)[] { prevMove, (currIndex, (sbyte)(currIndex + die)) }));
                            addedTwo = true;
                        }
                    }
                    else if (currIndex + die == 24) // can always leave with this...
                    {
                        if (isEndGame2)
                        { 
                            if (!addedTwo)
                                legalActions.Clear();
                            legalActions.Add(new BackGammonChanceAction(new (sbyte, sbyte)[] { prevMove, (currIndex, -1) }));
                            addedTwo = true;
                        }
                        break;
                    }
                    else
                    {
                        if (isEndGame2 && isFirstPiece)
                        {
                            if (!addedTwo)
                                legalActions.Clear();
                            legalActions.Add(new BackGammonChanceAction(new (sbyte, sbyte)[] { prevMove, (currIndex, -1) }));
                            addedTwo = true;
                        }
                        break;
                    }
                }
                if (!addedTwo)
                {
                    legalActions.Add(new BackGammonChanceAction(new (sbyte, sbyte)[] { prevMove }));
                }
            }

            // here is first loop for die 1
            for (int i = 0; i < state.myPieces.Count; i++)
            {
                if (state.myPieces[i] + dice.dice1 < 24)
                {
                    int addToIndex = (state.board[state.myPieces[i]] == 1) ? 1 : 0;
                    if (state.board[state.myPieces[i] + dice.dice1] >= -1)
                    {
                        // can move this piece
                        bool isEndGame2 = isEndGame ||
                            (i == 0 && state.board[state.myPieces[0]] == 1 && state.myPieces[0] + dice.dice1 >= 18 && (state.myPieces.Count == 1 || state.myPieces[1] >= 18));

                        willCreateDuplicate[i] = state.board[state.myPieces[i] + dice.dice1] >= 0;

                        if (state.board[state.myPieces[i] + dice.dice1] > 0)
                            OneDie(state, i + addToIndex, dice.dice2, ((sbyte)state.myPieces[i], (sbyte)(state.myPieces[i] + dice.dice1)), i == 0, isEndGame2);
                        else
                        {
                            int indexToRemove = HelperMethods.InsertToList(state.myPieces, i, (byte)(state.myPieces[i] + dice.dice1));
                            OneDie(state, i + addToIndex, dice.dice2, ((sbyte)state.myPieces[i], (sbyte)(state.myPieces[i] + dice.dice1)), i == 0, isEndGame2);
                            state.myPieces.RemoveAt(indexToRemove);
                        }
                    }
                }
                else
                {
                    if (isEndGame && (i == 0 || state.myPieces[i] + dice.dice1 == 24))
                    {
                        int addToIndex = (state.board[state.myPieces[i]] == 1) ? 1 : 0;
                        OneDie(state, i + addToIndex, dice.dice2, ((sbyte)state.myPieces[i], -1), i == 0, true);
                    }
                    break;
                }
            }

            // second loop for die 2, here we make sure we don't do duplicates
            for (int i = 0; i < state.myPieces.Count; i++)
            {
                bool isFirstPiece = i == 0 && state.board[state.myPieces[0]] == 1 && (state.myPieces.Count == 1 || state.myPieces[i + 1] <= state.myPieces[0] + dice.dice2);
                if (state.myPieces[i] + dice.dice2 < 24)
                {
                    if (state.board[state.myPieces[i] + dice.dice2] == 0)
                    {
                        // can move this piece and it may create a duplicate as im not eating a piece
                        // check if it will create a duplicate
                        bool isEndGame2 = isEndGame ||
                            (i == 0 && state.board[state.myPieces[0]] == 1 && state.myPieces[0] + dice.dice2 >= 18 && (state.myPieces.Count == 1 || state.myPieces[1] >= 18));

                        if (willCreateDuplicate[i] == false)
                        {
                            int indexToRemove = HelperMethods.InsertToList(state.myPieces, i, (byte)(state.myPieces[i] + dice.dice2));

                            OneDie(state, i + 1, dice.dice1, ((sbyte)state.myPieces[i], (sbyte)(state.myPieces[i] + dice.dice2)), isFirstPiece, isEndGame2);

                            state.myPieces.RemoveAt(indexToRemove);
                        }
                        else
                        {
                            OneDie(state, i + 1, dice.dice1, ((sbyte)state.myPieces[i], (sbyte)(state.myPieces[i] + dice.dice2)), isFirstPiece, isEndGame2);
                        }
                    }
                    else if (state.board[state.myPieces[i] + dice.dice2] > 0)
                    {
                        // can move this piece and it may create a duplicate as im not eating a piece
                        // check if it will create a duplicate
                        bool isEndGame2 = isEndGame ||
                            (i == 0 && state.board[state.myPieces[0]] == 1 && state.myPieces[0] + dice.dice2 >= 18 && (state.myPieces.Count == 1 || state.myPieces[1] >= 18));
                        if (willCreateDuplicate[i] == false)
                        {
                            OneDie(state, i + 1, dice.dice1, ((sbyte)state.myPieces[i], (sbyte)(state.myPieces[i] + dice.dice2)), i == 0 && state.board[state.myPieces[0]] == 1, isEndGame2);
                        }
                        else
                        {
                            int addIndex = HelperMethods.RemoveFromList(state.myPieces, i, (byte)(state.myPieces[i] + dice.dice2));
                            
                            OneDie(state, i + 1, dice.dice1, ((sbyte)state.myPieces[i], (sbyte)(state.myPieces[i] + dice.dice2)), i == 0 && state.board[state.myPieces[0]] == 1, isEndGame2);

                            state.myPieces.Insert(addIndex, (byte)(state.myPieces[i] + dice.dice2));
                        }
                    }
                    else if (state.board[state.myPieces[i] + dice.dice2] == -1)
                    {
                        // can move this piece and it wont create a duplicate as im eating a piece
                        bool isEndGame2 = isEndGame ||
                            (i == 0 && state.board[state.myPieces[0]] == 1 && state.myPieces[0] + dice.dice2 >= 18 && (state.myPieces.Count == 1 || state.myPieces[1] >= 18));

                        int indexToRemove = HelperMethods.InsertToList(state.myPieces, i, (byte)(state.myPieces[i] + dice.dice2));

                        OneDie(state, i + 1, dice.dice1, ((sbyte)state.myPieces[i], (sbyte)(state.myPieces[i] + dice.dice2)), i == 0 && state.board[state.myPieces[0]] == 1, isEndGame2);

                        state.myPieces.RemoveAt(indexToRemove);
                    }
                }
                else
                {
                    // if both die get this piece out, then both die will get the next piece out, and so it will create a duplicate
                    if (isEndGame && (i == 0 || state.myPieces[i] + dice.dice2 == 24) && (state.myPieces[i] + dice.dice1 < 24))
                        OneDie(state, i + 1, dice.dice1, ((sbyte)state.myPieces[i], -1), isFirstPiece, true);
                    break;
                }
            }
        }
        
        private List<List<(byte, byte)>> TowerMovesListV2(List<byte> towers, byte dCount)
        {
            List<List<(byte, byte)>> towerMovesList = new List<List<(byte, byte)>>();
            if (dCount == 0 || towers.Count == 0)
                return towerMovesList;
            if (towers.Count <= dCount)
            {
                towerMovesList.Add(ListToTable(towers));
                return towerMovesList;
            }
            List<byte[]> movesList = new List<byte[]>();
            byte[] currentIndexes = new byte[dCount];

            TowerMovesListV2(towers, 0, movesList, currentIndexes, dCount);
            // now movesList is populated with correct moves
            foreach (byte[] list in movesList)
                towerMovesList.Add(ArrayToTable(list));

            return towerMovesList;
        }

        private List<(byte, byte)> ListToTable(List<byte> list)
        {
            List<(byte, byte)> table = new List<(byte, byte)>();
            if (list.Count == 0)
                return table;

            byte last = list[0];
            byte count = 0;
            
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == last)
                    count++;
                else
                {
                    table.Add((last, count));
                    last = list[i];
                    count = 1;
                }
            }

            table.Add((last, count));
            return table;
        }

        private List<(byte, byte)> ArrayToTable(byte[] array)
        {
            List<(byte, byte)> table = new List<(byte, byte)>();
            if (array.Length == 0)
                return table;

            byte last = array[0];
            byte count = 0;

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == last)
                    count++;
                else
                {
                    table.Add((last, count));
                    last = array[i];
                    count = 1;
                }
            }

            table.Add((last, count));
            return table;
        }

        /// <summary>
        /// Returns a list of all tower move combinations from the list of towers: (AABC, ABCD, BCDF)...
        /// </summary>
        /// <param name="towers">The list of all towers: (AABCDDDF)</param>
        /// <param name="startIndex">The index to start searching from in the towers list.</param>
        /// <param name="towerMovesList">The list to add the new tower move combination.</param>
        /// <param name="currentIndexes">The current indexes to set the new tower combination in.</param>
        /// <param name="dCount">The amount of dice left to use in this combination.</param>
        private void TowerMovesListV2(List<byte> towers, int startIndex, List<byte[]> towerMovesList, byte[] currentIndexes, byte dCount)
        {
            int i = startIndex + 1;
            if (dCount == 1)
            {
                byte[] newCurrIndexes = new byte[currentIndexes.Length];
                for (int j = 1; j < newCurrIndexes.Length; j++)
                    newCurrIndexes[j] = currentIndexes[j];

                newCurrIndexes[0] = towers[startIndex];

                towerMovesList.Add(newCurrIndexes);
                for (; i < towers.Count; i++)
                {
                    if (towers[i - 1] != towers[i])
                    {
                        newCurrIndexes = new byte[currentIndexes.Length];
                        for (int j = 1; j < newCurrIndexes.Length; j++)
                            newCurrIndexes[j] = currentIndexes[j];

                        newCurrIndexes[0] = towers[i];

                        towerMovesList.Add(newCurrIndexes);
                    }
                }   
            }
            else
            {
                currentIndexes[dCount - 1] = towers[startIndex];
                TowerMovesListV2(towers, i, towerMovesList, currentIndexes, (byte)(dCount - 1));
                for (; i <= towers.Count - dCount; i++)
                {
                    if (towers[i - 1] != towers[i])
                    {
                        currentIndexes[dCount - 1] = towers[i];
                        TowerMovesListV2(towers, i + 1, towerMovesList, currentIndexes, (byte)(dCount - 1));
                    }
                }
            }
        }

        private List<Action> GetActionList(List<List<(byte, byte)>> towerMovesList, Dictionary<int, List<Action>> actionsDict)
        {
            List<Action> actions = new List<Action>();

            HashSet<BackGammonChanceAction> existingActions = new HashSet<BackGammonChanceAction>();

            foreach (List<(byte, byte)> towerMoves in towerMovesList)
            {
                List<Action> currActions = actionsDict[(towerMoves[0].Item2 * 100) + towerMoves[0].Item1];

                for (int i = 1; i < towerMoves.Count - 1; i++)
                {
                    List<Action> addedActions = actionsDict[(towerMoves[i].Item2 * 100) + towerMoves[i].Item1];

                    List<Action> newActions = new List<Action>(currActions.Count * addedActions.Count);

                    for (int j = 0; j < currActions.Count; j++)
                    {
                        BackGammonChanceAction currAct = (BackGammonChanceAction)currActions[j];
                        for (int k = 0; k < addedActions.Count; k++)
                        {
                            newActions.Add(new BackGammonChanceAction(currAct, (BackGammonChanceAction)addedActions[k]));
                        }
                    }

                    currActions = newActions;
                }


                if (towerMoves.Count > 1)
                {
                    List<Action> addedActions = actionsDict[(towerMoves[towerMoves.Count - 1].Item2 * 100) + towerMoves[towerMoves.Count - 1].Item1];

                    for (int j = 0; j < currActions.Count; j++)
                    {
                        BackGammonChanceAction currAct = (BackGammonChanceAction)currActions[j];
                        for (int k = 0; k < addedActions.Count; k++)
                        {
                            BackGammonChanceAction newAct = new BackGammonChanceAction(currAct, (BackGammonChanceAction)addedActions[k]);

                            if (existingActions.Contains(newAct) == false)
                            {
                                existingActions.Add(newAct);
                                actions.Add(newAct);
                            }
                        }
                    }
                }
                else
                {
                    foreach (BackGammonChanceAction action in currActions)
                    {
                        if (existingActions.Contains(action) == false)
                        {
                            existingActions.Add(action);
                            actions.Add(action);
                        }
                    }
                }
            }

            return actions;
        }

        /// <summary>
        /// Returns the List of Actions that can be made from that index with that dice
        /// </summary>
        /// <param name="index">The index of the pieces that should be checked for actions</param>
        /// <param name="count">The Amount of actions that should be made with those pieces.</param>
        /// <param name="dice">The die that will be used</param>
        /// <param name="pieceCount">The amount of pieces on that index.</param>
        /// <param name="state">The state of the board.</param>
        /// <returns></returns>
        private List<Action> GetActions(sbyte index, byte count, byte dice, byte pieceCount, BackGammonChoiceState state)
        { // i always know that i can move at least once from that index...
            List<Action> lst = new List<Action>();

            if (count == 4)
            {
                // if count == 4 i can either:
                // 1 piece 4 moves
                // 1 piece 3 moves, 1 piece 1 move
                // 2 piece 1 move, 1 piece 2 move
                // 2 piece 2 moves
                // 4 piece 1 move

                sbyte nextIndex1 = (sbyte)(index + dice);

                if (pieceCount >= 4) // do i have 4 pieces (opt 5)
                    lst.Add(new BackGammonChanceAction(new (sbyte, sbyte)[]
                    { (index, nextIndex1), (index, nextIndex1), (index, nextIndex1), (index, nextIndex1)}));

                sbyte nextIndex2 = (sbyte)(nextIndex1 + dice);
                if (nextIndex2 < 24 && state.board[nextIndex2] >= -1) // can i move twice?
                {
                    if (pieceCount >= 2) // do i have 2 pieces (opt 4)
                    {
                        lst.Add(new BackGammonChanceAction(new (sbyte, sbyte)[]
                        { (index, nextIndex1), (index, nextIndex1), (nextIndex1, nextIndex2), (nextIndex1, nextIndex2) }));

                        if (pieceCount >= 3) // do i have 3 pieces (opt 3)
                            lst.Add(new BackGammonChanceAction(new (sbyte, sbyte)[]
                            { (index, nextIndex1), (index, nextIndex1), (index, nextIndex1), (nextIndex1, nextIndex2)}));
                    }

                    sbyte nextIndex3 = (sbyte)(nextIndex2 + dice);
                    if (nextIndex3 < 24 && state.board[nextIndex3] >= -1) // can i move thrice?
                    {
                        if (pieceCount >= 2) // do i have 2 pieces (opt 2)
                        {
                            lst.Add(new BackGammonChanceAction(new (sbyte, sbyte)[]
                            { (index, nextIndex1), (index, nextIndex1), (nextIndex1, nextIndex2), (nextIndex2, nextIndex3)}));
                        }

                        sbyte nextIndex4 = (sbyte)(nextIndex3 + dice);
                        if (nextIndex4 < 24 && state.board[nextIndex4] >= -1) // can i move 4 times? (opt 1)
                            lst.Add(new BackGammonChanceAction(new (sbyte, sbyte)[]
                                   { (index, nextIndex1), (nextIndex1, nextIndex2), (nextIndex2, nextIndex3), (nextIndex3, nextIndex4) }));
                    }
                }
                return lst;
            }
            else if (count == 3)
            {
                // if count == 3 i can either:
                // 1 piece 3 moves 
                // 1 piece 2 moves, 1 piece 1 move
                // 3 piece 1 move

                sbyte nextIndex1 = (sbyte)(index + dice);
                if (pieceCount >= 3) // do i have 3 pieces (opt 3)
                    lst.Add(new BackGammonChanceAction(new (sbyte, sbyte)[] { (index, nextIndex1), (index, nextIndex1), (index, nextIndex1), (0, 0) }, 3));

                sbyte nextIndex2 = (sbyte)(nextIndex1 + dice);

                if (nextIndex2 < 24 && state.board[nextIndex2] >= -1) // can i move twice?
                {
                    if (pieceCount >= 2) // do i have 2 pieces (opt 2)
                        lst.Add(new BackGammonChanceAction(new (sbyte, sbyte)[] { (index, nextIndex1), (index, nextIndex1), (nextIndex1, nextIndex2), (0, 0) }, 3));

                    sbyte nextIndex3 = (sbyte)(nextIndex2 + dice);
                    if (nextIndex3 < 24 && state.board[nextIndex3] >= -1) // can i do 3 steps (opt 1)
                        lst.Add(new BackGammonChanceAction(new (sbyte, sbyte)[] { (index, nextIndex1), (nextIndex1, nextIndex2), (nextIndex2, nextIndex3), (0, 0) }, 3));
                }
                return lst;
            }
            else if (count == 2)
            {
                // if count == 2 i can either:
                // 1 piece 2 moves
                // 2 piece 1 move
                sbyte nextIndex1 = (sbyte)(index + dice);
                if (pieceCount >= 2) // do i have at least 2 pieces (opt 2)
                    lst.Add(new BackGammonChanceAction(new (sbyte, sbyte)[] { (index, nextIndex1), (index, nextIndex1), (0, 0), (0, 0) }, 2));

                sbyte nextIndex2 = (sbyte)(nextIndex1 + dice);
                if (nextIndex2 < 24 && state.board[nextIndex2] >= -1) // can i move twice (opt 1)
                    lst.Add(new BackGammonChanceAction(new (sbyte, sbyte)[] { (index, nextIndex1), (nextIndex1, nextIndex2), (0, 0), (0, 0) }, 2));
                return lst;
            }

            // count == 1 (never smaller than 1 and never bigger than 4)
            lst.Add(new BackGammonChanceAction(new (sbyte, sbyte)[] { (index, (sbyte)(index + dice)), (0, 0), (0, 0), (0, 0) }, 1)); // only possible move (no check since count is accurate)
            return lst;
        }

        private List<BackGammonChanceAction> EndGameDoubleActions(sbyte[] board, byte die, int dieCount)
        {
            return EndGameDoubleActions(board, die, dieCount, 18, false);
        }

        /// <summary>
        /// Returns the actions to take from the end game state with a double die.
        /// </summary>
        /// <param name="board">The current state of the board.</param>
        /// <param name="die">The die that was rolled.</param>
        /// <param name="dieCount">The amount of moves left to do.</param>
        /// <param name="i">The current index to check for.</param>
        /// <param name="found">Whether or not i can continue checking after limit.</param>
        /// <returns></returns>
        private List<BackGammonChanceAction> EndGameDoubleActions(sbyte[] board, byte die, int dieCount, int i, bool found)
        {
            // the board is in the bearing off stage, return all actions that can now be taken
            List<BackGammonChanceAction> actions = new List<BackGammonChanceAction>();
            if (dieCount == 0) // if i dont have any moves left, return empty list
            {
                return actions;
            }
            
            // go over all the positions that can be moved no matter what
            for (; i < 24 - die; i++)
            {
                if (board[i] > 0)
                {
                    if (board[i + die] >= -1)
                    {
                        int maxUseAmount = Math.Min(dieCount, board[i]);

                        for (int j = 1; j <= maxUseAmount; j++)
                        {
                            sbyte[] newBoard = HelperMethods.CopyArray(board);

                            newBoard[i] -= (sbyte)j;
                            newBoard[i + die] = (sbyte)Math.Max(1, newBoard[i + die] + j);

                            bool currFound = j != board[i]; // if j != board[i] it means i didn't use all the die, so i cant search deeper

                            List<BackGammonChanceAction> nextActions = EndGameDoubleActions(newBoard, die, dieCount - j, i + 1, found || currFound);

                            List<(sbyte, sbyte)> currMoves = new List<(sbyte, sbyte)>();
                            for (int k = 0; k < j; k++)
                            {
                                currMoves.Add(((sbyte)i, (sbyte)(i + die)));
                            }

                            if (nextActions.Count == 0)
                            {
                                actions.Add(new BackGammonChanceAction(currMoves));
                            }
                            else
                            {
                                foreach (BackGammonChanceAction action in nextActions)
                                {
                                    action.AddToAction(currMoves);
                                    actions.Add(action);
                                }
                            }
                        }
                    }
                    found = true;
                }
            }

            i = 24 - die;

            if (board[i] > 0)
            {
                int maxUseAmount = Math.Min(dieCount, board[i]);

                sbyte[] newBoard = HelperMethods.CopyArray(board);

                newBoard[i] -= (sbyte)maxUseAmount;

                bool currFound = newBoard[i] != 0; // if newBoard[i] != 0 it means i used all the die, so i cant search deeper

                List<BackGammonChanceAction> nextActions = EndGameDoubleActions(newBoard, die, dieCount - maxUseAmount, i + 1, found || currFound);

                List<(sbyte, sbyte)> currMoves = new List<(sbyte, sbyte)>();
                for (int k = 0; k < maxUseAmount; k++)
                {
                    currMoves.Add(((sbyte)i, -1));
                }

                if (nextActions.Count == 0)
                {
                    actions.Add(new BackGammonChanceAction(currMoves));
                }
                else
                {
                    foreach (BackGammonChanceAction action in nextActions)
                    {
                        action.AddToAction(currMoves);
                        actions.Add(action);
                    }
                }

                found = true;
            }

            i += 1;

            for (; found == false && i < 24; i++)
            {
                if (board[i] > 0)
                {
                    int maxUseAmount = Math.Min(dieCount, board[i]);

                    sbyte[] newBoard = HelperMethods.CopyArray(board);

                    newBoard[i] -= (sbyte)maxUseAmount;

                    bool currFound = newBoard[i] != 0; // if newBoard != 0 it means i used all the die, so i cant search deeper

                    List<BackGammonChanceAction> nextActions = EndGameDoubleActions(newBoard, die, dieCount - maxUseAmount, i + 1, currFound);

                    List<(sbyte, sbyte)> currMoves = new List<(sbyte, sbyte)>();
                    for (int k = 0; k < maxUseAmount; k++)
                    {
                        currMoves.Add(((sbyte)i, -1));
                    }

                    if (nextActions.Count == 0)
                    {
                        actions.Add(new BackGammonChanceAction(currMoves));
                    }
                    else
                    {
                        foreach (BackGammonChanceAction action in nextActions)
                        {
                            action.AddToAction(currMoves);
                            actions.Add(action);
                        }
                    }

                    found = true;
                }
            }

            return actions;
        }

        public BackGammonChoiceState GetNewRotated(BackGammonChoiceState prevState)
        {
            return (BackGammonChoiceState)Move(prevState, new BackGammonChanceAction());
        }

        // Returns a new state = prevState + the action
        public override State Move(State prevState, Action action)
        {
            BackGammonChoiceState newState = (BackGammonChoiceState)RotateBoard((BackGammonChoiceState)prevState);

            for (int i = 0; i < ((BackGammonChanceAction)action).Count; i++)
            {
                (sbyte, sbyte) values = ((BackGammonChanceAction)action).indexes[i];
                CleanRotatedMove(newState, ((sbyte)(23 - values.Item1), (sbyte)(23 - values.Item2)));
            }

            return newState;
        }

        private BackGammonChoiceState MoveWithoutRotate(BackGammonChoiceState prevState, BackGammonChanceAction action)
        {
            BackGammonChoiceState newState = new BackGammonChoiceState(prevState);

            for (int i = 0; i < action.Count; i++)
            {
                (sbyte, sbyte) values = action.indexes[i];
                CleanMove(newState, values);
            }

            return newState;
        }

        private void CleanRotatedMove(BackGammonChoiceState state, (sbyte, sbyte) move)
        {
            void PlacePiece(byte finishIndex)
            {
                switch (state.board[finishIndex])
                {
                    case 0: // there is no one there
                        state.board[finishIndex] = -1;
                        HelperMethods.InsertToList(state.enemyPieces, finishIndex);
                        break;
                    case 1: // there is enemy there, need to remove him
                        state.board[finishIndex] = -1;
                        HelperMethods.InsertToList(state.enemyPieces, finishIndex);
                        state.myPieces.Remove(finishIndex);
                        state.myEatenCount += 1;
                        break;
                    default: // i already had a piece there
                        state.board[finishIndex] -= 1;
                        break;
                }
            }

            if (move.Item1 == 24) // move 2 cant be 24 since there is no way he can bear off if he just got in
            {
                state.enemyEatenCount -= 1;
                PlacePiece((byte)move.Item2);
            }
            else
            {
                state.board[move.Item1] += 1;
                if (state.board[move.Item1] == 0)
                    state.enemyPieces.Remove((byte)move.Item1);

                if (move.Item2 != 24) // im not bearing off
                {
                    // move 2 cant be 24 since there is no way he can bear off if he just got in
                    PlacePiece((byte)move.Item2);
                }
            }
        }

        private void CleanMove(BackGammonChoiceState state, (sbyte, sbyte) move)
        {
            void PlacePiece(byte finishIndex)
            {
                switch (state.board[finishIndex])
                {
                    case 0: // there is no one there
                        state.board[finishIndex] = 1;
                        HelperMethods.InsertToList(state.myPieces, finishIndex);
                        break;
                    case -1: // there is enemy there, need to remove him
                        state.board[finishIndex] = 1;
                        HelperMethods.InsertToList(state.myPieces, finishIndex);
                        state.enemyPieces.Remove(finishIndex);
                        state.enemyEatenCount += 1;
                        break;
                    default: // i already had a piece there
                        state.board[finishIndex] += 1;
                        break;
                }
            }

            if (move.Item1 == -1) // move 2 cant be -1 since there is no way he can bear off if he just got in
            {
                state.myEatenCount -= 1;
                PlacePiece((byte)move.Item2);
            }
            else
            {
                state.board[move.Item1] -= 1;
                if (state.board[move.Item1] == 0)
                    state.myPieces.Remove((byte)move.Item1);

                if (move.Item2 != -1) // im not bearing off
                {
                    // move 2 cant be -1 since there is no way he can bear off if he just got in
                    PlacePiece((byte)move.Item2);
                }
            }
        }

        private State RotateBoard(BackGammonChoiceState state)
        {
            sbyte[] newBoard = new sbyte[24];
            for (int i = 0; i < 24; i++)
                newBoard[i] = (sbyte)-state.board[23 - i];

            List<byte> newEnemyPositions = new List<byte>(state.myPieces.Count);
            for (int i = state.myPieces.Count - 1; i >= 0; i--)
                newEnemyPositions.Add((byte)(23 - state.myPieces[i]));

            List<byte> newMyPositions = new List<byte>(state.enemyPieces.Count);
            for (int i = state.enemyPieces.Count - 1; i >= 0; i--)
                newMyPositions.Add((byte)(23 - state.enemyPieces[i]));

            return new BackGammonChoiceState(newBoard, state.enemyEatenCount, state.myEatenCount, newMyPositions, newEnemyPositions);
        }

        public override float[] GetNetworkInput()
        {
            throw new Exception("Should never call network input on this!");
        }

        public override bool IsGameOver()
        {
            return false;
        }

        public override int GameResult() // this should never be called, but needs to be implemented
        {
            throw new System.Exception("Should never be called!");
        }

        public override Action RandomPick(List<Action> legalActions, Random rnd)
        {
            return HelperMethods.GetRandomFromList(legalActions, rnd);
        }

        public override int RandomPick(int listSize, Random rnd)
        {
            return HelperMethods.RandomValue(0, listSize, rnd);
        }

        public override bool Equals(object obj)
        {
            if (obj is BackGammonChanceState)
            {
                BackGammonChanceState other = (BackGammonChanceState)obj;
                if (other.dice.dice1 == dice.dice1 && other.dice.dice2 == dice.dice2)
                    return true;
            }
            return false;
        }

        public override string ProtocolInformation()
        {
            return $"{dice.dice1.ToString()}{dice.dice2.ToString()}";
        }

        public static BackGammonChanceState PorotocolInformation(string state)
        {
            byte dice1 = byte.Parse(state[0].ToString());
            byte dice2 = byte.Parse(state[1].ToString());

            return new BackGammonChanceState(new Dice(dice1, dice2));
        }

        public override string ToString()
        {
            return $"Dice: [{dice.dice1},{dice.dice2}]";
        }
    }

    public class BackGammonChanceAction : Action
    {
        private const float EatingScore = 1.2f;
        private const float BuildHouseScore = 1.15f;
        private const float DestroyHouseScore = 0.95f;

        public override MessageType messageType
        {
            get
            {
                return MessageType.ChanceAction;
            }
        }

        public (sbyte, sbyte)[] indexes;
        public int Count;

        public BackGammonChanceAction(List<(sbyte, sbyte)> indexes)
        {
            this.indexes = new (sbyte, sbyte)[4];
            for (Count = 0; Count < indexes.Count; Count++)
                this.indexes[Count] = indexes[Count];
        }

        public BackGammonChanceAction((sbyte, sbyte)[] indexes, int length)
        {
            this.indexes = indexes;
            this.Count = length;
        }

        public BackGammonChanceAction((sbyte, sbyte)[] indexes)
        {
            this.indexes = indexes;
            this.Count = indexes.Length;
        }

        public BackGammonChanceAction()
        {
            this.indexes = new (sbyte, sbyte)[4];
        }

        public BackGammonChanceAction(BackGammonChanceAction copy)
        {
            indexes = new (sbyte, sbyte)[4];
            for (Count = 0; Count < copy.Count; Count++)
                indexes[Count] = copy.indexes[Count];
        }

        public BackGammonChanceAction(BackGammonChanceAction act1, BackGammonChanceAction act2)
        {
            indexes = new (sbyte, sbyte)[4];

            if (act1.indexes[0].Item1 >= act2.indexes[act2.Count - 1].Item1)
            {
                int i = 0;
                for (; i < act2.Count; i++)
                    indexes[i] = act2.indexes[i];

                for (int j = 0; j < act1.Count; j++)
                    indexes[act2.Count + j] = act1.indexes[j];

                this.Count = act2.Count + act1.Count;

                return;
            }
            

            int index1 = 0;
            int index2 = 0;

            while (index1 < act1.Count && index2 < act2.Count)
            {
                if (act1.indexes[index1].Item1 <= act2.indexes[index2].Item1)
                {
                    indexes[Count] = act1.indexes[index1];
                    index1++;
                }
                else
                {
                    indexes[Count] = act2.indexes[index2];
                    index2++;
                }
                Count++;
            }
            while (index1 < act1.Count)
            {
                indexes[Count] = act1.indexes[index1];
                Count++;
                index1++;
            }
            while (index2 < act2.Count)
            {
                indexes[Count] = act2.indexes[index2];
                Count++;
                index2++;
            }
        }

        public void AddToAction(BackGammonChanceAction addAction)
        {
            // since the addAction actions are ordered, i can add them pretty fast
            if (addAction.Count != 0) // only need to change something if there is actually something to change
            {
                BackGammonChanceAction newAction = new BackGammonChanceAction(this, addAction);
                this.indexes = newAction.indexes;
                this.Count = newAction.Count;
            }
        }

        public void AddToAction(List<(sbyte, sbyte)> moves)
        {
            for (int i = 0; i < moves.Count; i++)
                AddToAction(moves[i]);
        }

        public void AddToAction((sbyte, sbyte) move)
        {
            for (int i = 0; i < Count; i++)
            {
                if (indexes[i].Item1 > move.Item1)
                {
                    (sbyte, sbyte) temp = move;
                    move = indexes[i];
                    indexes[i] = temp;
                }
            }
            indexes[Count] = move;
            Count++;
        }

        public bool IsBearingOff()
        {
            // go from end to start, since if im bearing off, the last one is probably also bearing off...
            for (int i = Count - 1; i >= 0; i--)
                if (indexes[i].Item2 == -1)
                    return true;
            return false;
        }

        public override float GetScore(State prevState)
        {
            return 1;
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is BackGammonChanceAction)
            {
                BackGammonChanceAction other = (BackGammonChanceAction)obj;

                if (other.Count != Count)
                    return false;

                other = new BackGammonChanceAction(other);

                // since its ordered, i can just check if the indexes are equal...
                for (int i = 0; i < Count; i++)
                {
                    for (int j = 0; j < other.Count; j++)
                    {
                        if (indexes[i].Item1 != other.indexes[j].Item1)
                            return false;
                        if (indexes[i].Item2 == other.indexes[j].Item2)
                        {
                            // removes index j
                            for (int k = j + 1; k < other.Count; k++)
                            {
                                other.indexes[k - 1] = other.indexes[k];
                            }
                            other.Count--;
                            break;
                        }
                    }
                }
                return other.Count == 0;
            }
            return false;
        }

        public override Action Copy()
        {
            return new BackGammonChanceAction(this);
        }

        public override int GetHashCode()
        {
            int code = 0;
            for (int i = 0; i < Count; i++)
            {
                //code = (code * 100) + indexes[i].Item1 + 2;
                code = (code << 5) + indexes[i].Item1 + 2;
            }
            return code;
        }

        public override string ProtocolInformation()
        {
            string s = "";
            if (Count == 0)
                return s;

            for (int i = 0; i < Count - 1; i++)
                s += indexes[i].Item1.ToString() + '/' + indexes[i].Item2.ToString() + ",";

            s += indexes[Count - 1].Item1.ToString() + '/' + indexes[Count - 1].Item2.ToString();
            return s;
        }

        public static BackGammonChanceAction PorotocolInformation(string action)
        {
            Console.WriteLine("string of action: " + action);
            string[] parts = action.Split(',');

            (sbyte, sbyte)[] indexes = new (sbyte, sbyte)[4];
            int count = 0;

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] != "")
                {
                    string[] indexParts = parts[i].Split('/');

                    (sbyte, sbyte) indexMove = (sbyte.Parse(indexParts[0]), sbyte.Parse(indexParts[1]));

                    indexes[count] = indexMove;
                    count++;
                }
            }

            return new BackGammonChanceAction(indexes, count);
        }

        public override string ToString()
        {
            string s = "";

            for (int i = 0; i < Count; i++)
            {
                s += $"{indexes[i].Item1}/{indexes[i].Item2} ";
            }

            return s;
        }
    }
    
    public struct IndexCounter
    {
        public sbyte index;
        public sbyte count;

        public IndexCounter(sbyte index, sbyte count)
        {
            this.index = index;
            this.count = count;
        }

        public IndexCounter(int index, int count)
        {
            this.index = (sbyte)index;
            this.count = (sbyte)count;
        }

        public IndexCounter(sbyte index, int count)
        {
            this.index = index;
            this.count = (sbyte)count;
        }

        public override string ToString()
        {
            return $"Index: {index} Count: {count}";
        }
    }
}