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
        style.normal.textColor = Color.black; // ���� ������ �������� ����

        MouseHidden(isMouseVisable);
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) // ���α׷� ����
        {
#if DEBUG
            EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }

        if (Input.GetKeyDown(KeyCode.V)) // Ŀ�� �����/���̱�
        {
            isMouseVisable = !isMouseVisable;
            MouseHidden(isMouseVisable);
        }

        if (Input.GetKeyDown(KeyCode.D)) // ����� ��� �ѱ�/����
        {
            isDebugMod = !isDebugMod;
        }
    }
    void OnGUI() // ����׿�
    {
        if(isDebugMod)
        {
            Vector3 mousePos = Input.mousePosition;
            string text = $"Mouse Position: {mousePos.x:0}, {mousePos.y:0}";

            // ȭ�� ���� ��ܿ� ��� (y ��ǥ�� GUI�� ���� 0��)
            GUI.Label(new Rect(10, 10, 200, 20), text, style);
        }
    }

    public void MouseHidden(bool isMouseLock)
    {
        Cursor.visible = isMouseLock;
    }
}
