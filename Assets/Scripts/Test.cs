using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AD.UI;
using Diagram.Arithmetic;
using Diagram;

public class Test : MonoBehaviour
{
    public InputField Input;
    public Text Output;
    public Button ButtonClick;
    private void Start()
    { 
        ButtonClick.OnClick.AddListener(() =>
        {
            LineWord.WordPairs = new()
            {
                { "using",new SystemKeyWord.using_Key()},
                { "import",new SystemKeyWord.import_Key()},
                { "if",new SystemKeyWord.if_Key()},
                { "else",new SystemKeyWord.else_Key()},
                { "while",new SystemKeyWord.while_Key()},
                //{ "for",new SystemKeyWord.for_Key()},
                { "break",new SystemKeyWord.break_Key()},
                { "continue",new SystemKeyWord.continue_Key()},
                { "define",new SystemKeyWord.define_Key()},
                { "new",new SystemKeyWord.new_Key()}
            };
            LineScript ls = new();
            Debug.Log("run");
            ls.Run(
                "new(Test) Testing.TestForm(\"0.6*0.8\",\"95.0\")\n"+
                "Test  Log(\"Test Message Debug\")\n"+
                "Test  Create\n"+
                "using Diagram.LineScriptLauncher\r\n"+
                "Log \"Version: 0.5.1(With+DiagramCore+LineScript)\""
                );
        });
    }
}

namespace Testing
{
    public class TestForm
    {
        public TestForm(string message,float index)
        {
            Debug.Log("ok: "+message+"->"+index.ToString());
        }

        public void Log(string message)
        {
            Debug.Log(message);
        }

        public void Create()
        {
            new GameObject("New GO");
        }
    }
}