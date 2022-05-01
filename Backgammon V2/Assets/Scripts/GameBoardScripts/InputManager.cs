using GAME;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;

    [SerializeField] private GameObject undoButton;
    
    private int selectedIndex = -2;

    private List<Action> legalActions;
    
    private BackGammonChanceAction currentlyMadeActions;
    List<(sbyte, sbyte)> allowedMoves = new List<(sbyte, sbyte)>();
    private List<byte> diesLeft;
    private List<int> highlightedIndexes;

    private bool waitingForInput = false;

    private void Awake()
    {
        instance = this;
    }

    private void SetHighlightAllowedPointsFromIndex(int indexFrom, bool highlight)
    {
        List<int> pointIndexes = new List<int>();
        foreach ((sbyte, sbyte) move in allowedMoves)
        {
            if (move.Item1 == indexFrom)
            {
                if (move.Item2 == -1)
                    pointIndexes.Add(25);
                else
                    pointIndexes.Add(move.Item2);
            }
        }
        BoardViewManager.instance.HighlighPoints(highlight, pointIndexes);
    }

    public IEnumerator PressedOnPoint(int boardIndex)
    {
        if (waitingForInput && BoardViewManager.instance.IsStationary) // only get input when i need and after everything finished moving...
        {
            if (selectedIndex == -2) // trying to select the pip to move
            {
                foreach ((sbyte, sbyte) move in allowedMoves)
                {
                    if (move.Item1 == boardIndex)
                    {
                        selectedIndex = boardIndex;
                        BoardViewManager.instance.HighlightPips(false, highlightedIndexes); // remove all highlights
                        BoardViewManager.instance.HighlightPips(true, new List<int>() { move.Item1}); // except the selected piece
                        SetHighlightAllowedPointsFromIndex(selectedIndex, true);
                        break;
                    }
                }
            }
            else // meaning i already selected a pip
            {
                SetHighlightAllowedPointsFromIndex(selectedIndex, false);
                foreach ((sbyte, sbyte) move in allowedMoves)
                {
                    if (move.Item1 == selectedIndex && move.Item2 == boardIndex)
                    {
                        currentlyMadeActions.AddToAction(((sbyte)selectedIndex, (sbyte)boardIndex));

                        // stop the highlight of the selected piece
                        BoardViewManager.instance.HighlightPips(false, new List<int>() { move.Item1 });
                        
                        waitingForInput = false;
                        yield return StartCoroutine(BoardViewManager.instance.DoMove(selectedIndex, boardIndex, true));
                        waitingForInput = true;

                        if (Mathf.Abs(selectedIndex - boardIndex) <= 6)
                        {
                            diesLeft.Remove((byte)Mathf.Abs(selectedIndex - boardIndex));
                        }
                        else
                        {
                            int minDieValue = 24 - selectedIndex;
                            // either i went out from enemy home board
                            // or enemy entered, which isn't possible since its my input (so first option)

                            int minIndex = -1;
                            for (int i = 0; i < diesLeft.Count; i++)
                            {
                                if (diesLeft[i] >= minDieValue)
                                {
                                    if (minIndex == -1)
                                        minIndex = i;
                                    else if (diesLeft[i] < diesLeft[minIndex])
                                        minIndex = i;
                                }
                            }
                            diesLeft.RemoveAt(minIndex);
                        }

                        CalculateAllowedMoves(BoardViewManager.instance.currentState, diesLeft);
                        break;
                    }
                }
                if (allowedMoves.Count != 0)
                {
                    BoardViewManager.instance.HighlightPips(true, highlightedIndexes);
                }
                selectedIndex = -2;
            }
        }
        else
        {
            print($"Pressed but: waiting -> {waitingForInput} isStationary -> {BoardViewManager.instance.IsStationary}");
        }
    }

    private void CalculateAllowedMoves(BackGammonChoiceState choiceState, List<byte> diesLeft)
    {
        allowedMoves.Clear();
        if (choiceState.myEatenCount == 0 && choiceState.myPieces.Count == 0)
        {
            waitingForInput = false;
            return; // game finished...
        }

        highlightedIndexes.Clear();

        if (diesLeft.Count != 0) // i still have dies i haven't used
        {
            allowedMoves.AddRange(BackGammonChanceState.GetLegalActionsOfOneDie(diesLeft[0], choiceState)); // check allowed moves of first die

            if (diesLeft.Count > 1 && diesLeft[0] != diesLeft[1]) // its not double...
                allowedMoves.AddRange(BackGammonChanceState.GetLegalActionsOfOneDie(diesLeft[1], choiceState));
        }

        for (int i = allowedMoves.Count - 1; i >= 0; i--)
        {
            bool exists = false;
            foreach (BackGammonChanceAction action in this.legalActions)
            {
                if (action.ContainsMovement(allowedMoves[i]))
                {
                    exists = true;
                    break;
                }
            }
            if (exists || diesLeft.Count == 1)
            {
                if (highlightedIndexes.Contains(allowedMoves[i].Item1) == false)
                    highlightedIndexes.Add(allowedMoves[i].Item1);
            }
            else
                allowedMoves.RemoveAt(i); // isn't actually an allowed move (happens if i will be able to move less than max because of this)
        }


        print(choiceState);
        print("Legal Actions: " + HelperSpace.HelperMethods.ListToString(legalActions));
        print("Options: " + HelperSpace.HelperMethods.ListToString(allowedMoves));

        print("Dies left: " + HelperSpace.HelperMethods.ListToString(diesLeft));
    }

    private IEnumerator UndoButton()
    {
        if (currentlyMadeActions.Count > 0)
        {
            (BackGammonChoiceState, List<byte>) newState = BoardViewManager.instance.UndoMove();
            yield return StartCoroutine(BoardViewManager.instance.InitializeNewState(newState.Item1));

            currentlyMadeActions = new BackGammonChanceAction(currentlyMadeActions.indexes, currentlyMadeActions.Count - 1);
            
            if (selectedIndex != -2)
            {
                SetHighlightAllowedPointsFromIndex(selectedIndex, false);
                selectedIndex = -2;
            }

            diesLeft = new List<byte>(newState.Item2);

            CalculateAllowedMoves(newState.Item1, diesLeft);

            BoardViewManager.instance.HighlightPips(true, highlightedIndexes);
        }
    }

    public void PressedUndo()
    {
        StartCoroutine(UndoButton());
    }

    public IEnumerator GetInput(BackGammonChoiceState choiceState, BackGammonChanceState chanceState, BackGammonChanceAction insertedInput)
    {
        //this.legalActions = chanceState.GetLegalActions(choiceState);
        this.legalActions = chanceState.GetLegalActionsWithDuplicates(choiceState);
        //print("Try new legal options: " + HelperSpace.HelperMethods.ListToString(chanceState.GetLegalActionsWithDuplicates(choiceState)));

        yield return StartCoroutine(HelperSpace.HelperMethods.WaitXFrames(2));
        yield return new WaitUntil(() => BoardViewManager.instance.IsStationary);

        undoButton.SetActive(true);
        BoardViewManager.instance.ResetUndoMoves();

        yield return StartCoroutine(BoardViewManager.instance.InitializeNewState(choiceState));

        print("Started getting input");
        waitingForInput = true;
        highlightedIndexes = new List<int>();


        print("Finished initialising!");

        if (chanceState.Dice1 == chanceState.Dice2)
        {
            diesLeft = new List<byte>() { chanceState.Dice1, chanceState.Dice1, chanceState.Dice1, chanceState.Dice1 };
        }
        else
        {
            diesLeft = new List<byte>() { chanceState.Dice1, chanceState.Dice2 };
        }

        currentlyMadeActions = new BackGammonChanceAction();

        CalculateAllowedMoves(choiceState, diesLeft);

        BoardViewManager.instance.HighlightPips(true, highlightedIndexes);

        yield return new WaitUntil(() => currentlyMadeActions.Count == ((BackGammonChanceAction)this.legalActions[0]).Count);

        insertedInput.indexes = new (sbyte, sbyte)[4];
        insertedInput.Count = currentlyMadeActions.Count;
        for (int i = 0; i < currentlyMadeActions.Count; i++)
        {
            insertedInput.indexes[i] = currentlyMadeActions.indexes[i];
        }
        waitingForInput = false;
        undoButton.SetActive(false);
    }
}