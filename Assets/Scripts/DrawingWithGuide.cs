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
    public float similarityThreshold = 0.85f; // 85% 이상일 때 이벤트 발생
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
    //public RenderTexture textRT; // UI에 표시된 가이드 텍스트 텍스쳐

    //[Header("GuideImage")]
    //public RawImage guideImage; // UI에 표시된 가이드 이미지
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

    //손봐야함 (같은거 겹치면 안됨 + 아래 내용 참고)
    //한자가 써지면 올라가고 이후 만약 사라지는 시간이 다지나도 
    //모두 적혔으면 그냥 비워두고 시간지나면 다시 가능한것만 바로 쓸수 있도록 생김
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

    //    // 보정: 완전히 1로 설정
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
        StartCoroutine(WaitThenResetMoveText(20.0f, curIndex)); //duration이 지나면 무브 텍스트 리셋
        SetTexts(); // 랜덤으로 리스트에 있는 텍스트 중 하나로 변경
    }
    public IEnumerator WaitThenResetMoveText(float duration, int idx)
    {
        yield return new WaitForSeconds(duration);

        // 무브 텍스트 초기화
        ResetMoveText(idx);
        isFullStacked = false;
    }

    public void ResetMoveText(int idx)
    {
        data[idx].isAvailable = true;
        data[idx].isComplete = false;
        data[idx].moveTextObj.SetActive(false);
        data[idx].moveTextObj.GetComponent<TextMeshProUGUI>().fontSize = data[curIndex].originalMoveTextFontSize; //폰트사이즈 원래대로
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
                    //if ((cam.cullingMask & (1 << LayerMask.NameToLayer(gameObject.name))) != 0) //사이클 한번 돌고 초기화하는 시점
                    //{
                    //    //cam.cullingMask &= ~(1 << LayerMask.NameToLayer(gameObject.name)); //쿨링마스크에서 가이드텍스트 제거 // not use now
                    //    ResetMoveText(); // MoveText 위치, 색, 폰트사이즈 원래대로 되돌림
                    //}
                    RequestCreateLine(mousePos);
                }
                else if (Input.GetMouseButton(0))
                {
                    RequestConnectLine(mousePos);
                }
                else if (Input.GetMouseButtonUp(0)) // 마우스 왼쪽 버튼을 땔 때마다 유사도 확인
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
        CreateLine(mousePos); // 기존 함수 그대로 호출
    }

    void CreateLine(Vector3 mousePos)
    {
        positionCount = 2;
        GameObject line = new GameObject("Line" + (isLeft ? "Left" : "Right"));
        line.transform.SetParent(lineCanvasObject.transform, false); // false: 로컬 좌표 유지
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
        StartCoroutine(FadeInLine(lineRend, lineFadeInDelay)); // 0.5초 동안 페이드 인
    }

    private IEnumerator FadeInLine(LineRenderer lineRend, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            // lineRend 또는 연결된 GameObject가 파괴되었는지 확인
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
        ConnectLine(mousePos); // 기존 함수 그대로 호출
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

        // 1. 라인 렌더링 결과 캡처
        drawnTexture = GetTextAsTexture("Lines");

        if (isUseText)
        {
            // 2. 가이드 이미지/텍스트 텍스쳐 가져오기
            guideMask = GetTextAsTexture(gameObject.name);
            if (guideMask == null)
            {
                Debug.LogWarning("Guide texture not assigned.");
                yield break;
            }

            // 3. 유사도 계산
            float similarity = CompareTextures(drawnTexture, guideMask);
            Debug.Log($"Similarity: {similarity * 100f:F2}%");

            // 4. 이벤트 발생
            if (similarity >= similarityThreshold)
            {
                CharacterFillEvent?.Invoke();
            }
        }
        //else
        //{
        //    // 2. 가이드 이미지/텍스트 텍스쳐 가져오기
        //    Texture2D guideTexture = guideImage.texture as Texture2D;
        //    if (guideTexture == null)
        //    {
        //        Debug.LogWarning("Guide texture not assigned.");
        //        yield break;
        //    }

        //    // 3. 유사도 계산
        //    float similarity = CompareTextures(drawnTexture, guideTexture);
        //    Debug.Log($"Similarity: {similarity * 100f:F2}%");

        //    // 4. 이벤트 발생
        //    if (similarity >= similarityThreshold)
        //    {
        //        CharacterFillEvent?.Invoke();
        //    }
        //}
    }

    //아래 코드는 색깔까지 같아야함
    //float CompareTextures(Texture2D texA, Texture2D texB)
    //{
    //    int width = Mathf.Min(texA.width, texB.width);
    //    int height = Mathf.Min(texA.height, texB.height);

    //    Color[] pixelsA = texA.GetPixels(0, 0, width, height);
    //    Color[] pixelsB = texB.GetPixels(0, 0, width, height);

    //    int matchCount = 0;
    //    for (int i = 0; i < pixelsA.Length; i++)
    //    {
    //        if (Vector4.Distance(pixelsA[i].linear, pixelsB[i].linear) < 0.2f) // 유사한 색상
    //        {
    //            matchCount++;
    //        }
    //    }

    //    return (float)matchCount / pixelsA.Length;
    //}

    public Texture2D GetTextAsTexture(string layer)
    {
        //cam.cullingMask |= 1 << LayerMask.NameToLayer("Lines"); //마스크 추가
        //cam.cullingMask = ~(1 << LayerMask.NameToLayer("Lines")); //마스크 제거

        // 1. 텍스트 렌더링 결과 캡처
        RenderTexture rt = new RenderTexture(1920, 1080, 24);
        cam.targetTexture = rt;

        // 원래 cullingMask 백업
        int originalMask = cam.cullingMask;

        // 특정 레이어만 보이게 설정 (예: "UIOnly" 레이어)
        int maskToExclude = 1 << LayerMask.NameToLayer(layer);
        cam.cullingMask = maskToExclude;

        // 렌더링 강제 실행
        cam.Render();

        // 캡처
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        // 정리
        cam.cullingMask = originalMask;
        RenderTexture.active = null;
        cam.targetTexture = null;
        Destroy(rt);

        return tex;
    }

    //아래 코드는 색깔에는 상관없이 선이 따라 그려져만 있으면 됨.
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
            if (guideAlpha < 0.1f) continue; // 가이드가 없는 곳은 무시

            float guideBrightness = GetBrightness(guidePixels[i]);

            // 가이드에서 그려야 할 부분은 어두운 부분이라고 가정
            if (guideBrightness < 0.9f)
            {
                totalRelevantPixels++;

                float drawnBrightness = GetBrightness(drawnPixels[i]);
                if (drawnBrightness < 0.9f) // 그려진 픽셀은 어두움
                {
                    matchingPixels++;
                }
            }
        }

        if (totalRelevantPixels == 0) return 0f; // 비교할 부분이 없다면 0

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

// 공백 문자 사이트 : https://wepplication.github.io/tools/charMap/#unicode-2150-218F