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
        string script2 = Resources.Load<TextAsset>("__demo/buffcommand_after").text;
        core.Run(script2);
        new LineScript(("this", this)).Share(out var xcore)
            .Run("new(list) Diagram.ListBuilder()")
            .Run("list -> Add(1)")
            .Run("list -> Add(2)");
        xcore
            .Run("list -> ToArray()");
        xcore
            .Run("this -> Log(@result)");
        stats = false;
    }

    public void Log(params object[] tar)
    {
        foreach (var item in tar)
            Debug.Log(item);
    }
}
