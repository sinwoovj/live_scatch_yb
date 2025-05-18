using UnityEngine;

public class DrawingByPulse : MonoBehaviour
{
    public Camera cam;
    public Material defaultMaterial;
    public Color startColor;
    public Color endColor;
    public float startWidth = 0.05f;
    public float endWidth = 0.05f;

    public float maxInputInterval = 0.3f; // 시간 간격
    public float segmentSpacing = 0.01f;  // 선분 간 최소 거리 (보간 기준)

    private LineRenderer curLine;
    private Vector3? prevPos = null;
    private int positionCount = 0;
    private float lastInputTime = -999f;

    void Update()
    {
        // 테스트용 마우스 입력
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 pos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.3f));
            ReceiveInput(pos);
        }
    }

    public void ReceiveInput(Vector3 newPos)
    {
        float now = Time.time;

        if (now - lastInputTime > maxInputInterval || prevPos == null)
        {
            CreateNewLine(newPos);
        }
        else
        {
            AddInterpolatedSegment(prevPos.Value, newPos);
        }

        prevPos = newPos;
        lastInputTime = now;
    }

    void CreateNewLine(Vector3 startPos)
    {
        GameObject lineObj = new GameObject("Line");
        lineObj.transform.parent = cam.transform;
        lineObj.transform.position = Vector3.zero;

        curLine = lineObj.AddComponent<LineRenderer>();
        curLine.material = defaultMaterial;
        curLine.startColor = startColor;
        curLine.endColor = endColor;
        curLine.startWidth = startWidth;
        curLine.endWidth = endWidth;
        curLine.numCornerVertices = 5;
        curLine.numCapVertices = 5;

        positionCount = 1;
        curLine.positionCount = positionCount;
        curLine.SetPosition(0, startPos);
    }

    void AddInterpolatedSegment(Vector3 from, Vector3 to)
    {
        float distance = Vector3.Distance(from, to);
        int steps = Mathf.FloorToInt(distance / segmentSpacing);

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 point = Vector3.Lerp(from, to, t);
            AddPoint(point);
        }
    }

    void AddPoint(Vector3 point)
    {
        positionCount++;
        curLine.positionCount = positionCount;
        curLine.SetPosition(positionCount - 1, point);
    }
}
