using System.Collections;
using System.Collections.Generic;
using Diagram;
using UnityEngine;

namespace DemoGame
{
    public class FullSystem : MonoBehaviour
    {
        private void Start()
        {
            ArchitectureDiagram.RegisterArchitecture(this);
        }
    }
}
