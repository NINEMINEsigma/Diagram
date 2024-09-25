using System.Collections.Generic;
using UnityEngine;

public class CanvasMovement : MonoBehaviour
{
    public List<Transform> transforms = new();
    public float speed = 1;
    public AnimationCurve yCurve, xCurve;
    public float time => Time.timeSinceLevelLoad * speed;
    public float d;

    // Update is called once per frame
    void Update()
    {
        foreach(var item in transforms)
        {
            var current = item.position;
            var t = (Mathf.Sin(time) + 1) * 0.5f;
            item.position = new Vector3(d * xCurve.Evaluate(t), d * yCurve.Evaluate(t), current.z);
        }
    }
}
