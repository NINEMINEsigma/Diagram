using UnityEngine;

namespace Diagram
{
    public class LineScriptRunner : MonoBehaviour
    {
        [TextArea(1, 20)]
        public string Script;

        private void Awake()
        {
            new LineScript(("this", this)).Run(Script);
            GameObject.Destroy(gameObject);
        }
    }
}
