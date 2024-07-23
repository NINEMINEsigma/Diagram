using RuntimeInspectorNamespace;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Utility
{
    public class RuntimeBoardController : MonoBehaviour
    {
        public RuntimeHierarchy Hierarchy;
        public RuntimeInspector Inspector;

        private void Update()
        {
            if (Keyboard.current[Key.LeftCtrl].isPressed && Keyboard.current[Key.H].wasReleasedThisFrame)
            {
                Hierarchy.gameObject.SetActive(Hierarchy.gameObject.activeSelf);
                Inspector.gameObject.SetActive(Inspector.gameObject.activeSelf);
            }
        }
    }
}
