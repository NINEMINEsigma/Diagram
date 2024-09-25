using System.Collections.Generic;
using Diagram;
using UnityEngine;

public class BufferTest : MonoBehaviour
{
    public BuffManager BuffManager;
    private bool stats;

    private void Awake()
    {
        stats = true;
        BuffManager = new(this);
    }

    private void LateUpdate()
    {
        if (stats == false) return;
        string script = Resources.Load<TextAsset>("__demo/buffcommand").text;
        new LineScript(("this", this), ("manager", BuffManager)).Share(out var core).Run(script);
        Dictionary<string, string> mapper = new();
        string script2 = Resources.Load<TextAsset>("__demo/buffcommand_after").text;
        core.Run(script2);
        stats = false;
    }

    public void Log(object tar)
    {
        Debug.Log(tar);
    }
}
