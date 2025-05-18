using UnityEngine;

public class DrawWithMouse : MonoBehaviour
{
    private LineRenderer line;
    private Vector3 previousPosition;

    [SerializeField]
    private float minDistance = 0.1f;

    private void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 1;
        previousPosition = transform.position;
    }

    private void Update()
    {
        if(!Input.GetMouseButton(0))
        {
            return;
        }
        Vector3 currentPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currentPosition.z = 0f;

        if(Vector3.Distance(currentPosition, previousPosition) > minDistance)
        {
            if (previousPosition == transform.position)
            {
                // it means its the first point
                line.SetPosition(0, currentPosition);

            }
            else
            {
                line.positionCount++;
                line.SetPosition(line.positionCount - 1, currentPosition);

            }
            previousPosition = currentPosition;
        }
    }
}
