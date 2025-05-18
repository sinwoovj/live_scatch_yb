using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DrawingWithGuide : MonoBehaviour
{
    [Header("Preview")]
    public Texture2D drawnTexture;
    public Texture2D guideMask;

    [Header("Options")]
    public bool isLeft;
    public bool isUseText; // true = Use Text , false = Use Image
    public float similarityThreshold = 0.85f; // 85% �̻��� �� �̺�Ʈ �߻�
    public float imageGenerateDuration = 3f;
    public float drawDelay = 0.5f;
    public float lineFadeInDelay = 0.5f;

    [Header("Drawing")]
    public GameObject lineCanvasObject;
    public Camera cam;
    public Material defaultMaterial;
    public Color startColor;
    public Color endColor;
    public float startWidth;
    public float endWidth;
    private bool drawDisable = false;
    private bool isFullStacked = false;

    [Header("GuideText")]
    public GameObject guideTextObj;
    public GameObject subGuideTextObj;
    public GameObject refTextObj;
    public GameObject startPos;


    [System.Serializable]
    public class GuideTextData
    {
        public string guideText;
        public string subGuideText;
        public string moveText;
        public GameObject endPos;
        public GameObject moveTextObj;
        public float originalMoveTextFontSize;
        public Color originalMoveTextColor;
        public bool isAvailable;
        public bool isComplete;
    }
    public List<GuideTextData> data;
    private int curIndex;
    //public Camera textRenderCamera;
    //public RenderTexture textRT; // UI�� ǥ�õ� ���̵� �ؽ�Ʈ �ؽ���

    //[Header("GuideImage")]
    //public RawImage guideImage; // UI�� ǥ�õ� ���̵� �̹���
    [HideInInspector]
    public UnityEvent CharacterFillEvent;

    private LineRenderer curLine;
    private int positionCount = 2;
    private Vector3 prevPos = Vector3.zero;

    public List<GameObject> drawnLines = new List<GameObject>();
    private Stack<GameObject> undoStack = new Stack<GameObject>();
    private Stack<GameObject> redoStack = new Stack<GameObject>();
    public const int maxUndoCount = 20;



    private void Start()
    {
        if (CharacterFillEvent != null)
        {
            CharacterFillEvent = new UnityEvent();
        }
        for (int i = 0; i < data.Count; i++)
        {
            data[i].isAvailable = true;
            data[i].isComplete = false;
            data[i].moveTextObj.SetActive(false);
            data[i].originalMoveTextFontSize = data[i].moveTextObj.GetComponent<TextMeshProUGUI>().fontSize;
            data[i].originalMoveTextColor = data[i].moveTextObj.GetComponent<TextMeshProUGUI>().color;
        }        
        SetTexts();
        CharacterFillEvent.AddListener(OnCharacterFillSuccess);
    }
    void Update()
    {
        DrawMouse();
        HandleInput();
    }

    //�պ����� (������ ��ġ�� �ȵ� + �Ʒ� ���� ����)
    //���ڰ� ������ �ö󰡰� ���� ���� ������� �ð��� �������� 
    //��� �������� �׳� ����ΰ� �ð������� �ٽ� �����Ѱ͸� �ٷ� ���� �ֵ��� ����
    void SetTexts()
    {
        List<GuideTextData> filtered = data.Where(obj => { return obj.isAvailable; }).ToList();
        if (filtered.Count <= 0) { isFullStacked = true; return; }
        GuideTextData curGuideTextData = filtered[Random.Range(0, filtered.Count)];
        curIndex = data.IndexOf(curGuideTextData);
        data[curIndex].isAvailable = false;
        guideTextObj.GetComponent<TextMeshProUGUI>().text = data[curIndex].guideText;
        subGuideTextObj.GetComponent<TextMeshProUGUI>().text = data[curIndex].subGuideText;
        //data[curIndex].moveTextObj.GetComponent<TextMeshProUGUI>().text = data[curIndex].moveText;
        refTextObj.GetComponent<TextMeshProUGUI>().text = data[curIndex].moveText;
    }

    void OnCharacterFillSuccess()
    {
        drawDisable = true;
        Debug.Log("Character filled successfully!");
        ClearAllLines();
        //StartCoroutine(FadeInAlpha(guideImage, imageGenerateDuration)); // not use now
        //cam.cullingMask |= (1 << LayerMask.NameToLayer(gameObject.name)); // not use now
        data[curIndex].isComplete = true;
        StartCoroutine(MoveTextAndBigger(4.0f, 0.8f));
    }

    //public IEnumerator FadeInAlpha(RawImage targetImage, float duration)
    //{
    //    Color startColor = targetImage.color;
    //    float startAlpha = startColor.a;
    //    float endAlpha = 1f;

    //    float elapsed = 0f;

    //    while (elapsed < duration)
    //    {
    //        elapsed += Time.deltaTime;
    //        float t = Mathf.Clamp01(elapsed / duration);
    //        float newAlpha = Mathf.Lerp(startAlpha, endAlpha, t);
    //        targetImage.color = new Color(startColor.r, startColor.g, startColor.b, newAlpha);
    //        yield return null;
    //    }

    //    // ����: ������ 1�� ����
    //    targetImage.color = new Color(startColor.r, startColor.g, startColor.b, endAlpha);
    //}

    public IEnumerator MoveTextAndBigger(float duration, float biggerCoefficient)
    {
        data[curIndex].moveTextObj.SetActive(true);
        RectTransform rectTransform = data[curIndex].moveTextObj.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = startPos.GetComponent<RectTransform>().anchoredPosition;
        float targetFontSize = data[curIndex].originalMoveTextFontSize * biggerCoefficient;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // Position Lerp
            rectTransform.anchoredPosition = Vector2.Lerp(startPos.GetComponent<RectTransform>().anchoredPosition, data[curIndex].endPos.GetComponent<RectTransform>().anchoredPosition, t);
            data[curIndex].moveTextObj.GetComponent<TextMeshProUGUI>().color = new Color(data[curIndex].moveTextObj.GetComponent<TextMeshProUGUI>().color.r,
            data[curIndex].moveTextObj.GetComponent<TextMeshProUGUI>().color.g, data[curIndex].moveTextObj.GetComponent<TextMeshProUGUI>().color.b, Mathf.Lerp(128 / 255, 255 / 255, t));
            // Font size Lerp
            data[curIndex].moveTextObj.GetComponent<TextMeshProUGUI>().fontSize = Mathf.Lerp(data[curIndex].originalMoveTextFontSize, targetFontSize, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure final values are set
        rectTransform.anchoredPosition = data[curIndex].endPos.GetComponent<RectTransform>().anchoredPosition;
        data[curIndex].moveTextObj.GetComponent<TextMeshProUGUI>().fontSize = targetFontSize;
        drawDisable = false;
        StartCoroutine(WaitThenResetMoveText(20.0f, curIndex)); //duration�� ������ ���� �ؽ�Ʈ ����
        SetTexts(); // �������� ����Ʈ�� �ִ� �ؽ�Ʈ �� �ϳ��� ����
    }
    public IEnumerator WaitThenResetMoveText(float duration, int idx)
    {
        yield return new WaitForSeconds(duration);

        // ���� �ؽ�Ʈ �ʱ�ȭ
        ResetMoveText(idx);
        isFullStacked = false;
    }

    public void ResetMoveText(int idx)
    {
        data[idx].isAvailable = true;
        data[idx].isComplete = false;
        data[idx].moveTextObj.SetActive(false);
        data[idx].moveTextObj.GetComponent<TextMeshProUGUI>().fontSize = data[curIndex].originalMoveTextFontSize; //��Ʈ������ �������
        data[idx].moveTextObj.GetComponent<TextMeshProUGUI>().color = data[curIndex].originalMoveTextColor;
        data[idx].moveTextObj.GetComponent<RectTransform>().anchoredPosition = startPos.GetComponent<RectTransform>().anchoredPosition;
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
        bool mousePosIsLeft = Input.mousePosition.x < (Screen.width / 2) - (Screen.width / 11.3) ? true : false;
        //Debug.Log(Screen.width + " : " + Input.mousePosition.x);
        if (drawDisable || isFullStacked)
            return;
        if (mousePosIsLeft == isLeft)
        {
            if(data.Where(obj => { return obj.isComplete; }).ToList().Count < data.Count)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    //if ((cam.cullingMask & (1 << LayerMask.NameToLayer(gameObject.name))) != 0) //����Ŭ �ѹ� ���� �ʱ�ȭ�ϴ� ����
                    //{
                    //    //cam.cullingMask &= ~(1 << LayerMask.NameToLayer(gameObject.name)); //�𸵸���ũ���� ���̵��ؽ�Ʈ ���� // not use now
                    //    ResetMoveText(); // MoveText ��ġ, ��, ��Ʈ������ ������� �ǵ���
                    //}
                    RequestCreateLine(mousePos);
                }
                else if (Input.GetMouseButton(0))
                {
                    RequestConnectLine(mousePos);
                }
                else if (Input.GetMouseButtonUp(0)) // ���콺 ���� ��ư�� �� ������ ���絵 Ȯ��
                {
                    CheckSimilarity();
                }
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
        GameObject line = new GameObject("Line" + (isLeft ? "Left" : "Right"));
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
        drawnLines.Add(line);
        undoStack.Push(line);
        if (undoStack.Count > maxUndoCount)
        {
            GameObject oldest = undoStack.ToArray()[undoStack.Count - 1];
            undoStack = new Stack<GameObject>(new Stack<GameObject>(undoStack).ToArray()[..maxUndoCount]);
            Destroy(oldest);
        }

        redoStack.Clear(); // ���ο� ���� �׸��� redo �ʱ�ȭ
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

    void CheckSimilarity()
    {
        StartCoroutine(CaptureAndCompare());
    }

    System.Collections.IEnumerator CaptureAndCompare()
    {
        yield return new WaitForEndOfFrame();

        // 1. ���� ������ ��� ĸó
        drawnTexture = GetTextAsTexture("Lines");

        if (isUseText)
        {
            // 2. ���̵� �̹���/�ؽ�Ʈ �ؽ��� ��������
            guideMask = GetTextAsTexture(gameObject.name);
            if (guideMask == null)
            {
                Debug.LogWarning("Guide texture not assigned.");
                yield break;
            }

            // 3. ���絵 ���
            float similarity = CompareTextures(drawnTexture, guideMask);
            Debug.Log($"Similarity: {similarity * 100f:F2}%");

            // 4. �̺�Ʈ �߻�
            if (similarity >= similarityThreshold)
            {
                CharacterFillEvent?.Invoke();
            }
        }
        //else
        //{
        //    // 2. ���̵� �̹���/�ؽ�Ʈ �ؽ��� ��������
        //    Texture2D guideTexture = guideImage.texture as Texture2D;
        //    if (guideTexture == null)
        //    {
        //        Debug.LogWarning("Guide texture not assigned.");
        //        yield break;
        //    }

        //    // 3. ���絵 ���
        //    float similarity = CompareTextures(drawnTexture, guideTexture);
        //    Debug.Log($"Similarity: {similarity * 100f:F2}%");

        //    // 4. �̺�Ʈ �߻�
        //    if (similarity >= similarityThreshold)
        //    {
        //        CharacterFillEvent?.Invoke();
        //    }
        //}
    }

    //�Ʒ� �ڵ�� ������� ���ƾ���
    //float CompareTextures(Texture2D texA, Texture2D texB)
    //{
    //    int width = Mathf.Min(texA.width, texB.width);
    //    int height = Mathf.Min(texA.height, texB.height);

    //    Color[] pixelsA = texA.GetPixels(0, 0, width, height);
    //    Color[] pixelsB = texB.GetPixels(0, 0, width, height);

    //    int matchCount = 0;
    //    for (int i = 0; i < pixelsA.Length; i++)
    //    {
    //        if (Vector4.Distance(pixelsA[i].linear, pixelsB[i].linear) < 0.2f) // ������ ����
    //        {
    //            matchCount++;
    //        }
    //    }

    //    return (float)matchCount / pixelsA.Length;
    //}

    public Texture2D GetTextAsTexture(string layer)
    {
        //cam.cullingMask |= 1 << LayerMask.NameToLayer("Lines"); //����ũ �߰�
        //cam.cullingMask = ~(1 << LayerMask.NameToLayer("Lines")); //����ũ ����

        // 1. �ؽ�Ʈ ������ ��� ĸó
        RenderTexture rt = new RenderTexture(1920, 1080, 24);
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

// ���� ���� ����Ʈ : https://wepplication.github.io/tools/charMap/#unicode-2150-218F