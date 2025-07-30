using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DrawingWithGuide : MonoBehaviour
{
    public const int GUIDECOUNT = 1;
    public const int GUIDETEXTCOUNT = 9;

    [Header("Preview")]
    public Texture2D drawnTexture;

    [Header("Texts")]
    public List<string> texts = new List<string>{"Ȳ��","���","������","����","������","������Ż�","�뵵��","�Ӱ��","����"};

    [Header("Options")]
    public float similarityThreshold = 0.85f; // 85% �̻��� �� �̺�Ʈ �߻�
    public float drawDelay = 0.5f;
    public float lineFadeInDelay = 0.5f;
    public float startWidth;
    public float endWidth;

    [Header("Drawing")]
    public int mousePosLoc;
    public GameObject lineCanvasObject;
    public Camera cam;
    public Material defaultMaterial;
    public Color startColor;
    public Color endColor;
    public float targetFontSize;

    public List<GuideData> data = new List<GuideData>();
    [System.Serializable]
    public class GuideData
    {
        public int curIndex;
        public int guideNum;
        public string layerName;
        public Texture2D guideMask;
        public GameObject guideTextObj;
        public GameObject refTextObj;
        public GameObject moveTextObj;
        public List<GuideTextData> textData = new List<GuideTextData>();

        [System.Serializable]
        public class GuideTextData
        {
            public string text;
            public bool isAvailable;
            public bool isComplete;
        }

        public bool isGuideCompleted = false;
        public bool drawDisable = false;
        public bool isFullStacked = false;
    }

    public GameObject endPos;
    public float originalMoveTextFontSize = 150f;
    public Color originalMoveTextColor = Color.black;

    [HideInInspector]
    public UnityEvent CharacterFillEvent;

    private LineRenderer curLine;
    private int positionCount = 2;
    private Vector3 prevPos = Vector3.zero;

    public List<List<GameObject>> drawnLinesList = new List<List<GameObject>>() {new List<GameObject>(), new List<GameObject>(), new List<GameObject>()};
    private List<Stack<GameObject>> undoStackList = new List<Stack<GameObject>>() { new Stack<GameObject>(), new Stack<GameObject>(), new Stack<GameObject>() };
    private List<Stack<GameObject>> redoStackList = new List<Stack<GameObject>>() { new Stack<GameObject>(), new Stack<GameObject>(), new Stack<GameObject>() };
    public const int maxUndoCount = 20;

    private void Start()
    {
        if (CharacterFillEvent != null)
        {
            CharacterFillEvent = new UnityEvent();
        }

        data.Clear();

        //����
        for (int i = 0; i < GUIDECOUNT; i++)
        {
            data.Add(new GuideData());
            GuideData d = data[i];
            d.guideNum = i;
            d.layerName = "Guide" + (i + 1).ToString();
            d.guideTextObj = GameObject.Find("GuideText" + (i + 1).ToString());
            d.refTextObj = GameObject.Find("RefText" + (i + 1).ToString());
            d.moveTextObj = GameObject.Find("MoveText" + (i + 1).ToString());
            d.moveTextObj.GetComponent<TextMeshProUGUI>().color = Color.clear;

            for (int j = 0; j < GUIDETEXTCOUNT; j++)
            {
                data[i].textData.Add(new GuideData.GuideTextData());
                GuideData.GuideTextData dt = data[i].textData[j];
                dt.text = texts[j + i * GUIDETEXTCOUNT];
                dt.isAvailable = true;
                dt.isComplete = false;
            }
            SetTexts(i);
        }  
        ChangeLoadSettingsValues();
        CharacterFillEvent.AddListener(OnCharacterFillSuccess);
    }
    void Update()
    {
        DrawMouse();
        HandleInput();
    }


    void OnCharacterFillSuccess()
    {
        data[mousePosLoc].isGuideCompleted = true;
        data[mousePosLoc].drawDisable = true;
        Debug.Log("Character filled successfully!");
        ClearAllLines(mousePosLoc);
        data[mousePosLoc].textData[data[mousePosLoc].curIndex].isComplete = true;
        data[mousePosLoc].moveTextObj.GetComponent<TextMeshProUGUI>().text = data[mousePosLoc].textData[data[mousePosLoc].curIndex].text;
        StartCoroutine(MoveTextAndBigger(mousePosLoc, 4.0f));
    }


    public IEnumerator MoveTextAndBigger(int guideNum, float duration)
    {
        data[guideNum].moveTextObj.GetComponent<TextMeshProUGUI>().color = Color.black;
        RectTransform rectTransform = data[guideNum].moveTextObj.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = data[guideNum].refTextObj.GetComponent<RectTransform>().anchoredPosition;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // Position Lerp
            rectTransform.anchoredPosition = Vector2.Lerp(data[guideNum].refTextObj.GetComponent<RectTransform>().anchoredPosition, endPos.GetComponent<RectTransform>().anchoredPosition, t);
            data[guideNum].moveTextObj.GetComponent<TextMeshProUGUI>().color = new Color(data[guideNum].moveTextObj.GetComponent<TextMeshProUGUI>().color.r,
            data[guideNum].moveTextObj.GetComponent<TextMeshProUGUI>().color.g, data[guideNum].moveTextObj.GetComponent<TextMeshProUGUI>().color.b, Mathf.Lerp(128 / 255, 255 / 255, t));
            // Font size Lerp
            data[guideNum].moveTextObj.GetComponent<TextMeshProUGUI>().fontSize = Mathf.Lerp(originalMoveTextFontSize, targetFontSize, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure final values are set
        rectTransform.anchoredPosition = endPos.GetComponent<RectTransform>().anchoredPosition;
        data[guideNum].moveTextObj.GetComponent<TextMeshProUGUI>().fontSize = targetFontSize;
        data[guideNum].drawDisable = false;
        StartCoroutine(WaitThenResetMoveText(20.0f, guideNum, data[guideNum].curIndex)); //duration�� ������ ���� �ؽ�Ʈ ����
        SetTexts(guideNum); // �������� ����Ʈ�� �ִ� �ؽ�Ʈ �� �ϳ��� ����
    }
    void SetTexts(int guideNum)
    {
        List<GuideData.GuideTextData> filtered = data[guideNum].textData.Where(d => d.isAvailable == true).ToList();
        if (filtered.Count <= 0)
        {
            data[guideNum].isFullStacked = true;
            return;
        }
        //index ����
        GuideData.GuideTextData curGuideTextData = filtered[Random.Range(0, filtered.Count)];
        data[guideNum].curIndex = data[guideNum].textData.IndexOf(curGuideTextData);
        curGuideTextData.isAvailable = false;
        data[guideNum].guideTextObj.GetComponent<TextMeshProUGUI>().text = curGuideTextData.text;
        data[guideNum].refTextObj.GetComponent<TextMeshProUGUI>().text = curGuideTextData.text;
    }

    public IEnumerator WaitThenResetMoveText(float duration, int guideNum, int guideTextIndex)
    {
        float currTime = 0f;
        while (currTime < duration)
        {
            if(data.Where(d=> d.isGuideCompleted == true && d != data[guideNum]).ToList().Count > 0)
                break;
            currTime += Time.deltaTime;
            yield return null;
        }
        data[guideNum].isGuideCompleted = false;

        // ���� �ؽ�Ʈ �ʱ�ȭ
        ResetMoveText(guideNum, guideTextIndex);
        data[guideNum].isFullStacked = false;
    }

    public void ResetMoveText(int guideNum, int guideTextIndex)
    {
        data[guideNum].textData[guideTextIndex].isAvailable = true;
        data[guideNum].textData[guideTextIndex].isComplete = false;
        data[guideNum].moveTextObj.GetComponent<TextMeshProUGUI>().text = data[guideNum].textData[data[guideNum].curIndex].text;
        data[guideNum].moveTextObj.GetComponent<TextMeshProUGUI>().color = Color.clear;
        data[guideNum].moveTextObj.GetComponent<TextMeshProUGUI>().fontSize = originalMoveTextFontSize; //��Ʈ������ �������
        data[guideNum].moveTextObj.GetComponent<TextMeshProUGUI>().color = originalMoveTextColor;
        data[guideNum].moveTextObj.GetComponent<RectTransform>().anchoredPosition = data[guideNum].refTextObj.GetComponent<RectTransform>().anchoredPosition;
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            for(int i = 0; i < GUIDECOUNT; i++) ClearAllLines(i);
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
        if (data.Count != GUIDECOUNT)
            return;
        Vector3 mousePos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.3f));

        //if (Input.mousePosition.x < (Screen.width / GUIDECOUNT)) mousePosLoc = 0;
        //else if (Input.mousePosition.x < (Screen.width / GUIDECOUNT) * 2) mousePosLoc = 1;
        //else mousePosLoc = 2;

        mousePosLoc = 0;

        if (data[mousePosLoc].drawDisable || data[mousePosLoc].isFullStacked)
            return;

        if (data[mousePosLoc].textData.Where(d => d.isComplete == true).ToList().Count < data[mousePosLoc].textData.Count)
        {
            if (Input.GetMouseButtonDown(0))
            {
                RequestCreateLine(mousePos);
            }
            else if (Input.GetMouseButton(0))
            {
                RequestConnectLine(mousePos);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                CheckSimilarity(mousePosLoc);
            }
        }
    }

    public void RequestCreateLine(Vector3 mousePos)
    {
        StartCoroutine(DelayedCreateLine(mousePos));
    }

    private IEnumerator DelayedCreateLine(Vector3 mousePos)
    {
        yield return new WaitForSeconds(drawDelay);
        CreateLine(mousePos); // ���� �Լ� �״�� ȣ��
    }

    void CreateLine(Vector3 mousePos)
    {
        positionCount = 2;
        GameObject line = new GameObject("Line_" + mousePosLoc);
        line.transform.SetParent(lineCanvasObject.transform, false); // false: ���� ��ǥ ����
        LineRenderer lineRend = line.AddComponent<LineRenderer>();

        line.transform.position = Vector3.zero;
        line.layer = LayerMask.NameToLayer("Lines");
        lineRend.material = defaultMaterial;

        Color transparentStart = new Color(startColor.r, startColor.g, startColor.b, 0f);
        Color transparentEnd = new Color(endColor.r, endColor.g, endColor.b, 0f);
        lineRend.startColor = transparentStart;
        lineRend.endColor = transparentEnd;

        lineRend.startWidth = startWidth;
        lineRend.endWidth = endWidth;
        lineRend.numCornerVertices = 5;
        lineRend.numCapVertices = 5;
        lineRend.positionCount = 2;
        lineRend.SetPosition(0, mousePos);
        lineRend.SetPosition(1, mousePos);

        curLine = lineRend;

        // �� ���� �� ����
        drawnLinesList[mousePosLoc].Add(line);
        undoStackList[mousePosLoc].Push(line);
        if (undoStackList[mousePosLoc].Count > maxUndoCount)
        {
            GameObject oldest = undoStackList[mousePosLoc].ToArray()[undoStackList[mousePosLoc].Count - 1];
            undoStackList[mousePosLoc] = new Stack<GameObject>(new Stack<GameObject>(undoStackList[mousePosLoc]).ToArray()[..maxUndoCount]);
            Destroy(oldest);
        }

        redoStackList[mousePosLoc].Clear(); // ���ο� ���� �׸��� redo �ʱ�ȭ
        StartCoroutine(FadeInLine(lineRend, lineFadeInDelay)); // 0.5�� ���� ���̵� ��
    }

    private IEnumerator FadeInLine(LineRenderer lineRend, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            // lineRend �Ǵ� ����� GameObject�� �ı��Ǿ����� Ȯ��
            if (lineRend == null || lineRend.gameObject == null)
                yield break;

            float alpha = time / duration;
            Color start = new Color(startColor.r, startColor.g, startColor.b, alpha);
            Color end = new Color(endColor.r, endColor.g, endColor.b, alpha);

            lineRend.startColor = start;
            lineRend.endColor = end;

            time += Time.deltaTime;
            yield return null;
        }

        if (lineRend != null && lineRend.gameObject != null)
        {
            lineRend.startColor = startColor;
            lineRend.endColor = endColor;
        }
    }

    public void RequestConnectLine(Vector3 mousePos)
    {
        StartCoroutine(DelayedConnectLine(mousePos));
    }

    private IEnumerator DelayedConnectLine(Vector3 mousePos)
    {
        yield return new WaitForSeconds(drawDelay);
        ConnectLine(mousePos); // ���� �Լ� �״�� ȣ��
    }

    void ConnectLine(Vector3 mousePos)
    {
        if (prevPos != null && Vector3.Distance(prevPos, mousePos) >= 0.001f)
        {
            prevPos = mousePos;
            positionCount++;
            if (curLine == null)
                return;
            curLine.positionCount = positionCount;
            curLine.SetPosition(positionCount - 1, mousePos);
        }
    }

    void CheckSimilarity(int guideNum)
    {
        StartCoroutine(CaptureAndCompare(guideNum));
    }

    System.Collections.IEnumerator CaptureAndCompare(int guideNum)
    {
        yield return new WaitForEndOfFrame();

        // 1. ���� ������ ��� ĸó
        drawnTexture = GetTextAsTexture("Lines");
        // 2. ���̵� �̹���/�ؽ�Ʈ �ؽ��� ��������
        data[guideNum].guideMask = GetTextAsTexture(data[guideNum].layerName);
        if (data[guideNum].guideMask == null)
        {
            Debug.LogWarning("Guide texture not assigned.");
            yield break;
        }

        // 3. ���絵 ���
        float similarity = CompareTextures(drawnTexture, data[guideNum].guideMask);
        Debug.Log($"Similarity: {similarity * 100f:F2}%");

        // 4. �̺�Ʈ �߻�
        if (similarity >= similarityThreshold)
        {
            CharacterFillEvent?.Invoke();
        }
    }

    public Texture2D GetTextAsTexture(string layer)
    {
        //cam.cullingMask |= 1 << LayerMask.NameToLayer("Lines"); //����ũ �߰�
        //cam.cullingMask = ~(1 << LayerMask.NameToLayer("Lines")); //����ũ ����

        // 1. �ؽ�Ʈ ������ ��� ĸó
        RenderTexture rt = new RenderTexture(1920, 1400, 24);
        cam.targetTexture = rt;

        // ���� cullingMask ���
        int originalMask = cam.cullingMask;

        // Ư�� ���̾ ���̰� ���� (��: "UIOnly" ���̾�)
        int maskToExclude = 1 << LayerMask.NameToLayer(layer);
        cam.cullingMask = maskToExclude;

        // ������ ���� ����
        cam.Render();

        // ĸó
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        // ����
        cam.cullingMask = originalMask;
        RenderTexture.active = null;
        cam.targetTexture = null;
        Destroy(rt);

        return tex;
    }

    //�Ʒ� �ڵ�� ���򿡴� ������� ���� ���� �׷����� ������ ��.
    float CompareTextures(Texture2D drawn, Texture2D guide)
    {
        int width = Mathf.Min(drawn.width, guide.width);
        int height = Mathf.Min(drawn.height, guide.height);

        Color[] drawnPixels = drawn.GetPixels(0, 0, width, height);
        Color[] guidePixels = guide.GetPixels(0, 0, width, height);

        int totalRelevantPixels = 0;
        int matchingPixels = 0;

        for (int i = 0; i < guidePixels.Length; i++)
        {
            float guideAlpha = guidePixels[i].a;
            if (guideAlpha < 0.1f) continue; // ���̵尡 ���� ���� ����

            float guideBrightness = GetBrightness(guidePixels[i]);

            // ���̵忡�� �׷��� �� �κ��� ��ο� �κ��̶�� ����
            if (guideBrightness < 0.9f)
            {
                totalRelevantPixels++;

                float drawnBrightness = GetBrightness(drawnPixels[i]);
                if (drawnBrightness < 0.9f) // �׷��� �ȼ��� ��ο�
                {
                    matchingPixels++;
                }
            }
        }

        if (totalRelevantPixels == 0) return 0f; // ���� �κ��� ���ٸ� 0

        return (float)matchingPixels / totalRelevantPixels;
    }

    float GetBrightness(Color color)
    {
        return color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
    }

    void ClearAllLines(int guideNum)
    {
        foreach (GameObject line in drawnLinesList[guideNum])
        {
            Destroy(line);
        }
        drawnLinesList[guideNum].Clear();
        undoStackList[guideNum].Clear();
        redoStackList[guideNum].Clear();
    }

    void Undo()
    {
        if (undoStackList[mousePosLoc].Count > 0)
        {
            GameObject lastLine = undoStackList[mousePosLoc].Pop();
            lastLine.SetActive(false);
            redoStackList[mousePosLoc].Push(lastLine);
            drawnLinesList[mousePosLoc].Remove(lastLine);
        }
    }

    void Redo()
    {
        if (redoStackList[mousePosLoc].Count > 0)
        {
            GameObject line = redoStackList[mousePosLoc].Pop();
            line.SetActive(true);
            undoStackList[mousePosLoc].Push(line);
            drawnLinesList[mousePosLoc].Add(line);
        }
    }
    
    public void ChangeLoadSettingsValues()
    {
        similarityThreshold = SettingsLoader.Instance.GetSetting("SimilarityThreshold", 0.33f);
        drawDelay = SettingsLoader.Instance.GetSetting("DrawDelay", 0.2f);
        lineFadeInDelay = SettingsLoader.Instance.GetSetting("LineFadeInDelay", 0.4f);
        startWidth = SettingsLoader.Instance.GetSetting("StartWidth", 0.15f);
        endWidth = SettingsLoader.Instance.GetSetting("EndWidth", 0.1f);
    }
}