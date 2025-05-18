using UnityEngine;
using System.Collections.Generic;

public class Drawing : MonoBehaviour
{
    public Camera cam;
    public Material defaultMaterial;

    public Color startColor;
    public Color endColor;
    public float startWidth;
    public float endWidth;

    private LineRenderer curLine;
    private int positionCount = 2;
    private Vector3 PrevPos = Vector3.zero;

    private List<GameObject> drawnLines = new List<GameObject>();
    private Stack<GameObject> undoStack = new Stack<GameObject>();
    private Stack<GameObject> redoStack = new Stack<GameObject>();
    public const int maxUndoCount = 20;

    void Update()
    {
        HandleInput();
        DrawMouse();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            ClearAllLines();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Undo();
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Redo();
        }
    }

    void DrawMouse()
    {
        Vector3 mousePos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.3f));

        if (Input.GetMouseButtonDown(0))
        {
            createLine(mousePos);
        }
        else if (Input.GetMouseButton(0))
        {
            connectLine(mousePos);
        }
    }

    void createLine(Vector3 mousePos)
    {
        positionCount = 2;
        GameObject line = new GameObject("Line");
        LineRenderer lineRend = line.AddComponent<LineRenderer>();

        line.transform.parent = cam.transform;
        line.transform.position = mousePos;

        lineRend.startColor = startColor;
        lineRend.endColor = endColor;
        lineRend.startWidth = startWidth;
        lineRend.endWidth = endWidth;
        lineRend.numCornerVertices = 5;
        lineRend.numCapVertices = 5;
        lineRend.material = defaultMaterial;
        lineRend.positionCount = 2;
        lineRend.SetPosition(0, mousePos);
        lineRend.SetPosition(1, mousePos);

        curLine = lineRend;

        // 선 생성 시 저장
        drawnLines.Add(line);
        undoStack.Push(line);
        if (undoStack.Count > maxUndoCount)
        {
            GameObject oldest = undoStack.ToArray()[undoStack.Count - 1];
            undoStack = new Stack<GameObject>(new Stack<GameObject>(undoStack).ToArray()[..maxUndoCount]);
            Destroy(oldest);
        }

        redoStack.Clear(); // 새로운 선을 그리면 redo 초기화
    }

    void connectLine(Vector3 mousePos)
    {
        if (PrevPos != null && Mathf.Abs(Vector3.Distance(PrevPos, mousePos)) >= 0.001f)
        {
            PrevPos = mousePos;
            positionCount++;
            curLine.positionCount = positionCount;
            curLine.SetPosition(positionCount - 1, mousePos);
        }
    }

    void ClearAllLines()
    {
        foreach (GameObject line in drawnLines)
        {
            Destroy(line);
        }
        drawnLines.Clear();
        undoStack.Clear();
        redoStack.Clear();
    }

    void Undo()
    {
        if (undoStack.Count > 0)
        {
            GameObject lastLine = undoStack.Pop();
            lastLine.SetActive(false);
            redoStack.Push(lastLine);
            drawnLines.Remove(lastLine);
        }
    }

    void Redo()
    {
        if (redoStack.Count > 0)
        {
            GameObject line = redoStack.Pop();
            line.SetActive(true);
            undoStack.Push(line);
            drawnLines.Add(line);
        }
    }
}
