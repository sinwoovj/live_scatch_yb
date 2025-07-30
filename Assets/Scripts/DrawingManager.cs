using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DrawingManager : MonoBehaviour
{
    public static DrawingManager Instance;

    public List<GameObject> guideObjs;

    private void Start()
    {
        Instance = this;
    }
}
