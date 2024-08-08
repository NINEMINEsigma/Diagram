using UnityEngine;

namespace Diagram
{
    public class LineScriptLoadingMask : MonoBehaviour
    {
        private void Awake()
        {
            LineScript.LineScriptRuntimeEvent += Listener;
            DontDestroyOnLoad(this.gameObject);
            this.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            LineScript.LineScriptRuntimeEvent -= Listener;
        }

        public int counter = 0;

        private void Listener(LineScript core,bool value)
        {
            counter += value ? 1 : -1;
            this.gameObject.SetActive(counter > 0);
        }
    }
}
