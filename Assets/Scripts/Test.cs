using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AD.UI;
using Diagram.Arithmetic;

public class Test : MonoBehaviour
{
    public InputField Input;
    public Text Output;
    public Button ButtonClick;
    private void Start()
    {
        ArithmeticExtension.InitArithmeticExtension();
        ArithmeticExtension.RegisterVariable("Test", "5+9");
        ButtonClick.OnClick.AddListener(() =>
        {
            Output.SetText(Input.text.Compute().ToString());
        });
    }
}
