using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Diagram
{
    public class LineScriptConfig : MonoBehaviour
    {
        public string LauncherPath;
        public string LauncherDir => Path.GetDirectoryName(LauncherPath);

        private void Reset()
        {
            LauncherPath = Path.Combine(ToolFile.userPath, "LineScripts", "Launcher.ls");
        }

        private void Awake()
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
            StartCoroutine(Waiter());

            IEnumerator Waiter()
            {
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                using ToolFile file = new(LauncherPath, false, true, true);
                if (file)
                {
                    LineScript core = new();
                    core.Run(file.GetString(false, System.Text.Encoding.UTF8));
                }
                else
                {
                    Debug.LogException(file.ErrorException);
                }
            }
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [UnityEngine.Scripting.Preserve]
    public class LineScriptLauncher
    {
        public LineScript MyCore = new();

        public void Import(string path)
        {
            MyCore.Run($"import {path}");
        }

        public void Start(string path)
        {
            using ToolFile file = new(path, false, true, true);
            if (file)
                MyCore.Run(file.GetString(false, System.Text.Encoding.UTF8));
        }

        public void Task(string path)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                using ToolFile file = new(path, false, true, true);
                if (file)
                    new LineScript().Run(file.GetString(false, System.Text.Encoding.UTF8));
            });
        }

        public void Log(string message)
        {
            Debug.Log(message);
        }

        public void Version(int mainv, int subv, int minv)
        {
            Debug.Log($"Version: {mainv}.{subv}.{minv}");
        }
    }
}
