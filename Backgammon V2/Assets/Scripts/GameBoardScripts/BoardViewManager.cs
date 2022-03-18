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
        float size = topLeft.GetZoneHeight() / 5f;

        pipsMoving = new Dictionary<VisualPip, bool>();

        pipSize = new Vector2(size, size);

        print(pipSize);

        InitializeAllPoints();

        //BackGammonChoiceState cs = new BackGammonChoiceState(
        //    new sbyte[] { -2, -2, -2, 0, -4, -2, 0, -1, 0, 0, 0, 0, 1, 0, 0, 0, 2, 0, 3, 2, 3, 2, 2, -1 }, 0, 1);
        //BackGammonChanceState chanceState = new BackGammonChanceState(new Dice(1, 2));

        //InitializeNewState(cs);
        //BackGammonChanceAction output = new BackGammonChanceAction();
        //StartCoroutine(InputManager.instance.GetInput(cs, chanceState, output));

        //InitializeDeafualtPips();
        //HighlighPoints(true, new List<int>() { 0, 1, 5, 6, 16, 20, 21, 25 });
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

    private void InitializePipState(BackGammonChoiceState pipsState)
    {
        this.currentState = pipsState;

        DestroyAllInvisiblePips(topLeft, topRight, bottomRight, bottomLeft);

        DestroyAllVisualPips();

        pipsMoving.Clear();

        for (int i = 0; i < pipsState.board.Length; i++)
        {
            int zoneIndex = i / 6;

            BoardPoint point = insideBoard.GetChild(zoneIndex).GetComponent<BoardZone>().GetPoint(i - (zoneIndex * 6));

            for (int j = 0; j < Mathf.Abs(pipsState.board[i]); j++)
            {
                GameObject newPip = Instantiate(pipPrefab, point.GetPipHolder());
                RectTransform invisibleRect = newPip.GetComponent<RectTransform>();
                invisibleRect.sizeDelta = pipSize;


                VisualPip newVisualPip = Instantiate(visualPipPrefab, visualPipHolder).GetComponent<VisualPip>();

                newVisualPip.ChangeColor((pipsState.board[i] > 0) ? pipColor1 : pipColor2); // set correct color
                newVisualPip.SetSize(pipSize); // set correct size
                newVisualPip.SetInvisiblePip(invisibleRect); // set the new position
            }
        }

        for (int i = 0; i < pipsState.myEatenCount; i++)
        {
            GameObject newPip = Instantiate(pipPrefab, positiveEaten);
            RectTransform invisibleRect = newPip.GetComponent<RectTransform>();
            invisibleRect.sizeDelta = pipSize;

            VisualPip newVisualPip = Instantiate(visualPipPrefab, visualPipHolder).GetComponent<VisualPip>();
            newVisualPip.ChangeColor(pipColor1); // set correct color
            newVisualPip.SetSize(pipSize); // set correct size
            newVisualPip.SetInvisiblePip(invisibleRect); // set the new position
        }
        for (int i = 0; i < pipsState.enemyEatenCount; i++)
        {
            GameObject newPip = Instantiate(pipPrefab, negativeEaten);
            RectTransform invisibleRect = newPip.GetComponent<RectTransform>();
            invisibleRect.sizeDelta = pipSize;

            VisualPip newVisualPip = Instantiate(visualPipPrefab, visualPipHolder).GetComponent<VisualPip>();
            newVisualPip.ChangeColor(pipColor2); // set correct color
            newVisualPip.SetSize(pipSize); // set correct size
            newVisualPip.SetInvisiblePip(invisibleRect); // set the new position
        }

        ForceUpdateCanvas();

        foreach (Transform pip in visualPipHolder)
            pip.GetComponent<VisualPip>().GoToInvisiblePip();
    }

    public void InitializeDeafualtPips()
    {
        InitializePipState(new BackGammonChoiceState());
    }

    public void InitializeNewState(BackGammonChoiceState newState)
    {
        if (newState.Equals(currentState) == false)
        {
            InitializePipState(newState);
        }
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
        int maxDie = 1;
        foreach (VisualDie die in allDice)
        {
            if (die.GetDieValue() >= minDieUsed && die.IsEnabled() && die.IsUsed() == false)
            {
                maxDie = Mathf.Max(die.GetDieValue(), maxDie);
            }
        }

        UsedDie(maxDie);
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

        DieUsedInMove(indexFrom, indexTo, isPositivePlayer);

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
            BoardPoint currentPoint = boardZone.GetPoint(indexFrom - (zone * 6));

            //int childIndex = boardZone.GetYPosition() > 0 ? currentPoint.PipCount() - 1 : 0;
            int childIndex = currentPoint.PipCount() - 1;

            invisiblePip = currentPoint.GetPip(childIndex);
            currentState.board[indexFrom] -= (sbyte)valueToAdd;
        }

        int newSiblingIndex = 0;
        if (indexTo == -1)
        {
            if (isPositivePlayer)
                newParent = positiveEnd;
            else
                newParent = negativeEnd;
        }
        else
        {
            int zone = indexTo / 6;

            BoardZone boardZone = insideBoard.GetChild(zone).GetComponent<BoardZone>();
            BoardPoint parentPoint = boardZone.GetPoint(indexTo - (zone * 6));
            newParent = parentPoint.GetPipHolder();

            //newSiblingIndex = boardZone.GetYPosition() > 0 ? parentPoint.PipCount() : 0;
            newSiblingIndex = parentPoint.PipCount();

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
                    parentPoint.GetPip(0).SetParent(negativeEaten);
                }
                else
                {
                    currentState.myEatenCount++;
                    parentPoint.GetPip(0).SetParent(positiveEaten);
                }

                currentState.board[indexTo] = (sbyte)valueToAdd;
            }
        }

        currentState.myPieces.Clear();
        currentState.enemyPieces.Clear();

        for (int i = 0; i < 24; i++)
        {
            if (currentState.board[i] > 0)
                currentState.myPieces.Add((byte)i);
            else if (currentState.board[i] < 0)
                currentState.enemyPieces.Add((byte)i);
        }

        invisiblePip.SetParent(newParent);
        invisiblePip.SetSiblingIndex(newSiblingIndex);

        yield return StartCoroutine(HelperSpace.HelperMethods.WaitXFrames(2)); // wait 2 frames so that IsStationary has time to refresh...
        yield return new WaitUntil(() => IsStationary);
    }
    
    public IEnumerator DoMoves(BackGammonChanceAction action, bool isPositivePlayer)
    {
        print("Is stationary: " + IsStationary);
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
}
