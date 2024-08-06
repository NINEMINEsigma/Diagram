using System.Collections;
using System.IO;
using Diagram.Message;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Diagram
{
    public class LineScriptConfig : MonoBehaviour
    {
        public string LauncherPath => Path.Combine(ToolFile.userPath, "LineScripts", "Launcher.ls");
        public string LauncherDir => Path.GetDirectoryName(LauncherPath); 

        private void Start()
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
                { "new",new SystemKeyWord.new_Key()},
                {"include",new SystemKeyWord.include_Key() },
                {"delete",new SystemKeyWord.delete_Key()},
                {"call",new SystemKeyWord.call_key() }
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
            MyCore.Run($"include {path}");
        }

        public void Task(string path)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                Start(path);
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

        public void ToScene(string scenename)
        {
            SceneManager.LoadScene(scenename);
        }

        public void Cache(string message)
        {
            new AddCache(message);
        }
    }
}
