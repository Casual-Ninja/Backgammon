using GAME;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardViewManager : MonoBehaviour
{
    public static BoardViewManager instance { get; private set; }

    [SerializeField] public Camera privateCamera;

    public Camera Camera { get { return privateCamera; } }

    [SerializeField] private BoardZone topLeft, topRight, bottomRight, bottomLeft;
    [SerializeField] private Transform insideBoard, visualPipHolder;
    [SerializeField] private Transform positiveEnd, negativeEnd, positiveEaten, negativeEaten;
    [SerializeField] private VisualDie[] allDice;
    [SerializeField] private GameObject pointPrefabUp, pointPrefabDown, pipPrefab, visualPipPrefab;

    [SerializeField] private Color32 pointColor1, pointColor2;
    [SerializeField] private Color32 pipColor1, pipColor2;

    private Stack<UndoStateSave> movesMade = new Stack<UndoStateSave>();

    private VisualPip[] positiveVisualPips;
    private VisualPip[] negativeVisualPips;

    public bool InBoardTransition { get; private set; }

    public BackGammonChoiceState currentState { get; private set; }

    private Vector2 pipSize; // changes based on values I set and the screen size

    [SerializeField] private Dictionary<VisualPip, bool> pipsMoving;

    public void ChangePipMoveValue(VisualPip thePip, bool moving)
    {
        if (moving)
        {
            if (pipsMoving.ContainsKey(thePip) == false)
                pipsMoving.Add(thePip, true);
        }
        else
        {
            if (pipsMoving.ContainsKey(thePip))
                pipsMoving.Remove(thePip);
        }
    }

    public bool IsStationary { get { return pipsMoving.Count == 0; } }

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        float size = Mathf.Min(topLeft.GetZoneHeight() / 5f, topLeft.GetZoneWidth());

        positiveEnd.GetComponent<HorizontalLayoutGroup>().spacing = -size / 2f;
        negativeEnd.GetComponent<HorizontalLayoutGroup>().spacing = -size / 2f;

        pipsMoving = new Dictionary<VisualPip, bool>();

        pipSize = new Vector2(size, size);

        print(pipSize);

        InitializeAllPoints();

        //BackGammonChoiceState cs = new BackGammonChoiceState(
        //    new sbyte[] { -2, 0, 0, -2, 0, -4, 0, -1, 3, 0, 0, 7, -3, 0, 0, 0, 0, 0, 2, 2, 0, -1, -1, -1 }, 0, 0);
        //BackGammonChanceState chanceState = new BackGammonChanceState(new Dice(3, 6));

        //StartCoroutine(CheckInput(cs, chanceState));

        //InitializeDeafualtPips();
        //HighlighPoints(true, new List<int>() { 0, 1, 5, 6, 16, 20, 21, 25 });
    }

    private IEnumerator CheckInput(BackGammonChoiceState cs, BackGammonChanceState chanceState)
    {
        yield return StartCoroutine(InitializeNewState(cs));
        BackGammonChanceAction output = new BackGammonChanceAction();
        StartCoroutine(SetDiceValues(chanceState));
        StartCoroutine(InputManager.instance.GetInput(cs, chanceState, output));
    }

    private void DestroyAllInvisiblePips(params BoardZone[] zones)
    {
        foreach (BoardZone zone in zones)
        {
            zone.DestroyAllPips();
        }
        foreach (Transform pip in positiveEnd)
            Destroy(pip.gameObject);
        foreach (Transform pip in negativeEnd)
            Destroy(pip.gameObject);
        foreach (Transform pip in positiveEaten)
            Destroy(pip.gameObject);
        foreach (Transform pip in negativeEaten)
            Destroy(pip.gameObject);
    }

    private void DestroyAllVisualPips()
    {
        foreach (Transform pip in visualPipHolder)
        {
            Destroy(pip.gameObject);
        }
    }

    private void ForceUpdateCanvas()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(topLeft.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(topRight.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(bottomRight.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(bottomLeft.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(positiveEaten.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(negativeEaten.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(visualPipHolder.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(positiveEnd.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(negativeEnd.GetComponent<RectTransform>());
    }

    private void InitializeAllPoints()
    {
        for (int i = 0; i < 6; i++)
        {
            Color32 currColor = (i % 2 == 0) ? pointColor1 : pointColor2;

            topLeft.GetPoint(i).ChangeColor(currColor);
            topRight.GetPoint(i).ChangeColor(currColor);
            bottomRight.GetPoint(i).ChangeColor(currColor);
            bottomLeft.GetPoint(i).ChangeColor(currColor);

            int j = i; // since the event uses pointers...


            insideBoard.GetChild(0).GetComponent<BoardZone>().GetPoint(j).GetButton().onClick.AddListener(
                                    () => StartCoroutine(InputManager.instance.PressedOnPoint(j)));

            insideBoard.GetChild(1).GetComponent<BoardZone>().GetPoint(j).GetButton().onClick.AddListener(
                                    () => StartCoroutine(InputManager.instance.PressedOnPoint(6 + j)));

            insideBoard.GetChild(2).GetComponent<BoardZone>().GetPoint(j).GetButton().onClick.AddListener(
                                    () => StartCoroutine(InputManager.instance.PressedOnPoint(12 + j)));

            insideBoard.GetChild(3).GetComponent<BoardZone>().GetPoint(j).GetButton().onClick.AddListener(
                                    () => StartCoroutine(InputManager.instance.PressedOnPoint(18 + j)));
        }

        negativeEaten.parent.GetComponent<Button>().onClick.AddListener(
                                    () => StartCoroutine(InputManager.instance.PressedOnPoint(-1)));

        positiveEnd.GetComponent<Button>().onClick.AddListener(() => StartCoroutine(InputManager.instance.PressedOnPoint(-1)));
        //negativeEnd.GetComponent<Button>().onClick.AddListener(() => StartCoroutine(InputManager.instance.PressedOnPoint(-1)));

        ForceUpdateCanvas();
    }

    private void CorrectAllPointText()
    {
        // set so that the numbers on the pips are correct after the movement
        foreach (byte index in currentState.myPieces)
        {
            int zone = index / 6;

            BoardZone boardZone = insideBoard.GetChild(zone).GetComponent<BoardZone>();
            BoardPoint parentPoint = boardZone.GetPoint(index - (zone * 6));
            parentPoint.SetCorrectNumberText();
        }
        foreach (byte index in currentState.enemyPieces)
        {
            int zone = index / 6;
            BoardZone boardZone = insideBoard.GetChild(zone).GetComponent<BoardZone>();
            BoardPoint parentPoint = boardZone.GetPoint(index - (zone * 6));
            parentPoint.SetCorrectNumberText();
        }
    }

    private void InstantiateVisualPips()
    {
        if (positiveVisualPips == null)
        {
            positiveVisualPips = new VisualPip[15];
            negativeVisualPips = new VisualPip[15];

            for (int i = 0; i < positiveVisualPips.Length; i++)
            {
                positiveVisualPips[i] = Instantiate(visualPipPrefab, visualPipHolder).GetComponent<VisualPip>();
                positiveVisualPips[i].SetColor(pipColor1);
                positiveVisualPips[i].SetSize(pipSize);

                negativeVisualPips[i] = Instantiate(visualPipPrefab, visualPipHolder).GetComponent<VisualPip>();
                negativeVisualPips[i].SetColor(pipColor2);
                negativeVisualPips[i].SetSize(pipSize);
            }
        }

        foreach (VisualPip pip in positiveVisualPips)
        {
            pip.SetHighlight(false);
        }
        foreach (VisualPip pip in negativeVisualPips)
            pip.SetHighlight(false);
    }

    private IEnumerator InitializePipStateV2(BackGammonChoiceState pipsState)
    {
        InstantiateVisualPips();

        this.currentState = pipsState;

        pipsMoving.Clear();

        int positiveFinishedCount = 15 - pipsState.myEatenCount;
        int negativeFinishedCount = 15 - pipsState.enemyEatenCount;

        int positiveVisualIndex = 0;
        int negativeVisualIndex = 0;

        int wantedPipCount;
        int currentPipCount;

        for (int i = 0; i < pipsState.board.Length; i++)
        {
            int zoneIndex = i / 6;

            BoardPoint point = insideBoard.GetChild(zoneIndex).GetComponent<BoardZone>().GetPoint(i - (zoneIndex * 6));
            wantedPipCount = Mathf.Abs(pipsState.board[i]);
            currentPipCount = point.PipCount();

            
            for (int j = Mathf.Max(wantedPipCount, currentPipCount); j > 0; j--)
            {
                if (j <= wantedPipCount) // i should have this pip
                {
                    VisualPip wantedPip;
                    if (pipsState.board[i] > 0)
                    {
                        wantedPip = positiveVisualPips[positiveVisualIndex];
                        positiveVisualIndex++;
                        positiveFinishedCount--;
                    }
                    else
                    {
                        wantedPip = negativeVisualPips[negativeVisualIndex];
                        negativeVisualIndex++;
                        negativeFinishedCount--;
                    }

                    if (j > currentPipCount) // do I need to instantiate a pip
                    {
                        RectTransform newPip = Instantiate(pipPrefab, point.GetPipHolder()).GetComponent<RectTransform>();
                        newPip.sizeDelta = pipSize;
                        wantedPip.SetInvisiblePip(newPip);
                    }
                    else // already have the invisible pip instantiated here
                    {
                        wantedPip.SetInvisiblePip(point.GetPip(j - 1));
                    }
                }
                else // i shouldn't have this pip
                {
                    if (j <= currentPipCount) // this pip is instantiated, need to destroy
                    {
                        Destroy(point.GetPip(j - 1).gameObject);
                    }
                }
            }
        }

        wantedPipCount = pipsState.myEatenCount;
        currentPipCount = positiveEaten.childCount;
        for (int i = Mathf.Max(wantedPipCount, currentPipCount); i > 0; i--)
        {
            if (i <= wantedPipCount) // i want to have this pip
            {
                VisualPip wantedPip = positiveVisualPips[positiveVisualIndex];
                positiveVisualIndex++;

                if (i > currentPipCount) // do i need to instantiate this pip
                {
                    RectTransform newPip = Instantiate(pipPrefab, positiveEaten).GetComponent<RectTransform>();
                    newPip.sizeDelta = pipSize;
                    wantedPip.SetInvisiblePip(newPip);
                }
                else // i dont...
                {
                    wantedPip.SetInvisiblePip(positiveEaten.GetChild(i - 1));
                }
            }
            else // i dont want this pip
            {
                if (i <= currentPipCount) // but i have it currently
                {
                    Destroy(positiveEaten.GetChild(i - 1).gameObject);
                }
            }
        }

        wantedPipCount = pipsState.enemyEatenCount;
        currentPipCount = negativeEaten.childCount;
        for (int i = Mathf.Max(wantedPipCount, currentPipCount); i > 0; i--)
        {
            if (i <= wantedPipCount) // i want to have this pip
            {
                VisualPip wantedPip = negativeVisualPips[negativeVisualIndex];
                negativeVisualIndex++;

                if (i > currentPipCount) // do i need to instantiate this pip
                {
                    RectTransform newPip = Instantiate(pipPrefab, negativeEaten).GetComponent<RectTransform>();
                    newPip.sizeDelta = pipSize;
                    wantedPip.SetInvisiblePip(newPip);
                }
                else // i dont...
                {
                    wantedPip.SetInvisiblePip(negativeEaten.GetChild(i - 1));
                }
            }
            else // i dont want this pip
            {
                if (i <= currentPipCount) // but i have it currently
                {
                    Destroy(negativeEaten.GetChild(i - 1).gameObject);
                }
            }
        }

        wantedPipCount = positiveFinishedCount;
        currentPipCount = positiveEnd.childCount;
        for (int i = Mathf.Max(wantedPipCount, currentPipCount); i > 0; i--)
        {
            if (i <= wantedPipCount) // do i want to have this pip
            {
                VisualPip wantedPip = positiveVisualPips[positiveVisualIndex];
                positiveVisualIndex++;

                if (i > currentPipCount) // i dont have this pip, so instantiate
                {
                    RectTransform newPip = Instantiate(pipPrefab, positiveEnd).GetComponent<RectTransform>();
                    newPip.sizeDelta = pipSize;
                    wantedPip.SetInvisiblePip(newPip);
                }
                else // already have this pip
                {
                    wantedPip.SetInvisiblePip(positiveEnd.GetChild(i - 1));
                }
            }
            else // i dont want this pip
            {
                if (i <= currentPipCount) // but i have it currently
                {
                    Destroy(positiveEnd.GetChild(i - 1).gameObject);
                }
            }
        }

        wantedPipCount = negativeFinishedCount;
        currentPipCount = negativeEnd.childCount;
        for (int i = Mathf.Max(wantedPipCount, currentPipCount); i > 0; i--)
        {
            if (i <= wantedPipCount) // do i want to have this pip
            {
                VisualPip wantedPip = negativeVisualPips[negativeVisualIndex];
                negativeVisualIndex++;

                if (i > currentPipCount) // i dont have this pip, so instantiate
                {
                    RectTransform newPip = Instantiate(pipPrefab, negativeEnd).GetComponent<RectTransform>();
                    newPip.sizeDelta = pipSize;
                    wantedPip.SetInvisiblePip(newPip);
                }
                else // already have this pip
                {
                    wantedPip.SetInvisiblePip(negativeEnd.GetChild(i - 1));
                }
            }
            else // i dont want this pip
            {
                if (i <= currentPipCount) // but i have it currently
                {
                    Destroy(negativeEnd.GetChild(i - 1).gameObject);
                }
            }
        }

        ForceUpdateCanvas();

        yield return new WaitForEndOfFrame();

        foreach (VisualPip pip in positiveVisualPips)
            pip.GoToInvisiblePip();
        foreach (VisualPip pip in negativeVisualPips)
            pip.GoToInvisiblePip();

        CorrectAllPointText();
    }

    public IEnumerator InitializeDeafualtPips()
    {
        yield return StartCoroutine(InitializePipStateV2(new BackGammonChoiceState()));
    }

    public IEnumerator InitializeNewState(BackGammonChoiceState newState)
    {
        if (newState.Equals(currentState) == false)
        {
            BackGammonChoiceState newStateCopy = new BackGammonChoiceState(newState);
            print("They are different!");
            print("Current board:");
            print(currentState);
            print("NewBoard:");
            print(newStateCopy);

            yield return StartCoroutine(InitializePipStateV2(newStateCopy));
        }
        yield return 0;
    }

    private void SetDiceValues(params byte[] dice)
    {
        if (dice.Length == 2)
        {
            allDice[0].SetEnabled(false);
            allDice[3].SetEnabled(false);

            allDice[1].SetEnabled(true);
            allDice[1].SetDieValue(dice[0]);
            allDice[1].SetUsed(false);

            allDice[2].SetEnabled(true);
            allDice[2].SetDieValue(dice[1]);
            allDice[2].SetUsed(false);
        }
        else // == 4
        {
            for (int i = 0; i < 4; i++)
            {
                allDice[i].SetEnabled(true);
                allDice[i].SetUsed(false);
                allDice[i].SetDieValue(dice[i]);
            }
        }
    }

    public IEnumerator SetDiceValues(BackGammonChanceState chanceState)
    {
        yield return StartCoroutine(HelperSpace.HelperMethods.WaitXFrames(2));
        yield return new WaitUntil(() => IsStationary);

        if (chanceState.Dice1 == chanceState.Dice2)
            SetDiceValues(chanceState.Dice1, chanceState.Dice1, chanceState.Dice1, chanceState.Dice1);
        else
            SetDiceValues(chanceState.Dice1, chanceState.Dice2);
    }

    private void UsedDie(int dieUsed)
    {
        foreach (VisualDie die in allDice)
        {
            if (die.GetDieValue() == dieUsed && die.IsEnabled() && die.IsUsed() == false)
            {
                die.SetUsed(true);
                break;
            }
        }
    }

    private void MinDieUsed(int minDieUsed)
    {
        int minDie = 6;
        foreach (VisualDie die in allDice)
        {
            if (die.GetDieValue() >= minDieUsed && die.IsEnabled() && die.IsUsed() == false)
            {
                minDie = Mathf.Min(die.GetDieValue(), minDie);
            }
        }

        UsedDie(minDie);
    }

    private void DieUsedInMove(int indexFrom, int indexTo, bool isPositivePlayer)
    {
        if (isPositivePlayer) // positive player: 0 -> 23
        {
            if (indexFrom == -1)
                UsedDie(indexTo + 1);
            else if (indexTo == -1)
                MinDieUsed(24 - indexFrom);
            else
                UsedDie(indexTo - indexFrom);
        }
        else // negative player: 23 -> 0
        {
            if (indexFrom == -1)
                UsedDie(24 - indexTo);
            else if (indexTo == -1)
                MinDieUsed(indexFrom + 1);
            else
                UsedDie(indexFrom - indexTo);
        }
        //if (Mathf.Abs(indexTo - indexFrom) <= 6)
        //    UsedDie(Mathf.Abs(indexTo - indexFrom));
        //else
        //{
        //    // either i went out from enemy home board
        //    // or enemy entered

        //    if (indexFrom == -1) // enemy entered
        //    {
        //        UsedDie(24 - indexTo);
        //    }
        //    else // i went out from enemy home board
        //    {
        //        int minDie = 7;
        //        for (int i = 0; i < allDice.Length; i++)
        //        {
        //            if (allDice[i].IsEnabled() && allDice[i].IsUsed() == false && allDice[i].GetDieValue() < minDie)
        //            {
        //                minDie = allDice[i].GetDieValue();
        //            }
        //        }
        //        UsedDie(minDie);
        //    }
        //}
    }

    public void HighlightPips(bool highlight, List<int> indexes)
    {
        foreach (int index in indexes)
        {
            if (index == -1)
            {
                positiveEaten.GetChild(positiveEaten.childCount - 1).GetComponent<InvisiblePip>().visualPip.SetHighlight(highlight);
            }
            else
            {
                int zone = index / 6;
                BoardPoint theWantedPoint = insideBoard.GetChild(zone).GetComponent<BoardZone>().GetPoint(index - (zone * 6));
                theWantedPoint.SetPipHighlight(highlight);
            }
        }
    }

    public void HighlighPoints(bool highlight, List<int> indexes)
    {
        foreach (int index in indexes)
        {
            if (index == 25)
            {
                if (highlight)
                {
                    print("Name: " + positiveEnd.name);
                    positiveEnd.GetComponent<HighlighShader>().UseHighlighShader();
                }
                else
                    positiveEnd.GetComponent<HighlighShader>().UseDefaultShader();
            }
            else
            {
                int zone = index / 6;

                BoardPoint point = insideBoard.GetChild(zone).GetComponent<BoardZone>().GetPoint(index - (zone * 6));
                point.SetPointHighlight(highlight);
            }
        }
    }

    public IEnumerator DoMove(int indexFrom, int indexTo, bool isPositivePlayer)
    {
        yield return StartCoroutine(HelperSpace.HelperMethods.WaitXFrames(2)); // wait 2 frames so that IsStationary has time to refresh...
        yield return new WaitUntil(() => IsStationary);

        BackGammonChoiceState currentStateCopy = (BackGammonChoiceState)currentState.Copy();
        List<(byte, bool, bool)> dice = new List<(byte, bool, bool)>();
        foreach (VisualDie die in allDice)
        {
            dice.Add(((byte)die.GetDieValue(), die.IsEnabled(), die.IsUsed()));
        }
        movesMade.Push(new UndoStateSave(currentStateCopy, dice));

        DieUsedInMove(indexFrom, indexTo, isPositivePlayer);

        BoardPoint previousPoint = null;

        Transform invisiblePip = null;
        Transform newParent = null;

        int valueToAdd = isPositivePlayer ? 1 : -1;
        if (indexFrom == -1) // im entering from bar
        {
            if (isPositivePlayer)
            {
                invisiblePip = positiveEaten.GetChild(positiveEaten.childCount - 1);
                currentState.myEatenCount -= 1;
            }
            else
            {
                invisiblePip = negativeEaten.GetChild(negativeEaten.childCount - 1);
                currentState.enemyEatenCount -= 1;
            }
        }
        else
        {
            int zone = indexFrom / 6;
            BoardZone boardZone = insideBoard.GetChild(zone).GetComponent<BoardZone>();
            previousPoint = boardZone.GetPoint(indexFrom - (zone * 6));
            

            int childIndex = previousPoint.PipCount() - 1;

            invisiblePip = previousPoint.GetPip(childIndex);
            currentState.board[indexFrom] -= (sbyte)valueToAdd;
        }

        int newSiblingIndex = 0;
        if (indexTo == -1)
        {
            invisiblePip.GetComponent<InvisiblePip>().SetTextAbsolute("");
            if (isPositivePlayer)
                newParent = positiveEnd;
            else
                newParent = negativeEnd;
        }
        else
        {
            int zone = indexTo / 6;

            BoardZone boardZone = insideBoard.GetChild(zone).GetComponent<BoardZone>();
            BoardPoint newPoint = boardZone.GetPoint(indexTo - (zone * 6));
            newParent = newPoint.GetPipHolder();

            //newSiblingIndex = boardZone.GetYPosition() > 0 ? parentPoint.PipCount() : 0;
            newSiblingIndex = newPoint.PipCount();

            if (currentState.board[indexTo] == 0 || (currentState.board[indexTo] / Mathf.Abs(currentState.board[indexTo])) == valueToAdd)
            {
                // i didn't eat anyone
                currentState.board[indexTo] += (sbyte)valueToAdd;
            }
            else
            {
                // i ate someone
                if (isPositivePlayer)
                {
                    currentState.enemyEatenCount++;
                    newPoint.GetPip(0).SetParent(negativeEaten);
                }
                else
                {
                    currentState.myEatenCount++;
                    newPoint.GetPip(0).SetParent(positiveEaten);
                }

                currentState.board[indexTo] = (sbyte)valueToAdd;
            }
        }
        
        invisiblePip.SetParent(newParent);
        invisiblePip.SetSiblingIndex(newSiblingIndex);

        invisiblePip.GetComponent<InvisiblePip>().SetTextAbsolute("");
        if (previousPoint != null)
            previousPoint.SetCorrectNumberText();

        currentState.myPieces.Clear();
        currentState.enemyPieces.Clear();

        for (int i = 0; i < 24; i++)
        {
            if (currentState.board[i] > 0)
            {
                currentState.myPieces.Add((byte)i);
            }
            else if (currentState.board[i] < 0)
            {
                currentState.enemyPieces.Add((byte)i);
            }
        }

        //// set so that the numbers on the pips are correct before the end of movement
        //foreach (byte index in currentState.myPieces)
        //{
        //    int zone = index / 6;

        //    BoardZone boardZone = insideBoard.GetChild(zone).GetComponent<BoardZone>();
        //    BoardPoint parentPoint = boardZone.GetPoint(index - (zone * 6));
        //    parentPoint.SetCorrectNumberTextAtStartOfMovement();
        //}
        //foreach (byte index in currentState.enemyPieces)
        //{
        //    int zone = index / 6;
        //    BoardZone boardZone = insideBoard.GetChild(zone).GetComponent<BoardZone>();
        //    BoardPoint parentPoint = boardZone.GetPoint(index - (zone * 6));
        //    parentPoint.SetCorrectNumberTextAtStartOfMovement();
        //}

        yield return StartCoroutine(HelperSpace.HelperMethods.WaitXFrames(2)); // wait 2 frames so that IsStationary has time to refresh...
        yield return new WaitUntil(() => IsStationary);

        CorrectAllPointText();
    }

    public IEnumerator DoMoves(BackGammonChanceAction action, bool isPositivePlayer)
    {
        for (int i = 0; i < action.Count; i++)
        {
            (sbyte, sbyte) movement = action.indexes[i];

            if (isPositivePlayer)
                yield return StartCoroutine(DoMove(movement.Item1, movement.Item2, isPositivePlayer));
            else
            {
                int from = movement.Item1 == -1 ? -1 : 23 - movement.Item1;
                int to = movement.Item2 == -1 ? -1 : 23 - movement.Item2;
                yield return StartCoroutine(DoMove(from, to, isPositivePlayer));
            }
        }
    }

    public (BackGammonChoiceState, List<byte>) UndoMove()
    {
        UndoStateSave getToState = movesMade.Pop();
        List<byte> newDiesLeft = new List<byte>();
        for (int i = 0; i < 4; i++)
        {
            allDice[i].SetEnabled(getToState.dice[i].Item2);
            if (getToState.dice[i].Item2)
            {
                allDice[i].SetDieValue(getToState.dice[i].Item1);
                allDice[i].SetUsed(getToState.dice[i].Item3);

                if (getToState.dice[i].Item3 == false)
                    newDiesLeft.Add(getToState.dice[i].Item1);

            }
        }
        return (getToState.state, newDiesLeft);
    }

    public void ResetUndoMoves()
    {
        movesMade.Clear();
    }

    private class UndoStateSave
    {
        public BackGammonChoiceState state;
        public List<(byte, bool, bool)> dice;

        public UndoStateSave(BackGammonChoiceState state, List<(byte, bool, bool)> dice)
        {
            this.state = state;
            this.dice = dice;
        }
    }
}
