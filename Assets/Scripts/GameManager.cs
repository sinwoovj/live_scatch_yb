using UnityEditor;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public bool isMouseVisable = false;
    public bool isDebugMod = true;

    private GUIStyle style;

    void Start()
    {
        style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.black; // 글자 색상을 검정으로 설정

        MouseHidden(isMouseVisable);
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) // 프로그램 종료
        {
#if DEBUG
            EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }

        if (Input.GetKeyDown(KeyCode.V)) // 커서 숨기기/보이기
        {
            isMouseVisable = !isMouseVisable;
            MouseHidden(isMouseVisable);
        }

        if (Input.GetKeyDown(KeyCode.D)) // 디버그 모드 켜기/끄기
        {
            isDebugMod = !isDebugMod;
        }
    }
    void OnGUI() // 디버그용
    {
        if(isDebugMod)
        {
            Vector3 mousePos = Input.mousePosition;
            string text = $"Mouse Position: {mousePos.x:0}, {mousePos.y:0}";

            // 화면 좌측 상단에 출력 (y 좌표는 GUI는 위가 0임)
            GUI.Label(new Rect(10, 10, 200, 20), text, style);
        }
    }

    public void MouseHidden(bool isMouseLock)
    {
        Cursor.visible = isMouseLock;
    }
}
