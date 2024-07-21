using System.Collections;
using System.Collections.Generic;
using System.IO;
using Diagram;
using UnityEngine;

namespace Game
{
    public class MinnesCamera : MonoBehaviour
    {
        private void Start()
        {
            this.RegisterControllerOn(typeof(Minnes), new());
            using ToolFile file = new(Path.Combine(ToolFile.userPath, "Camera.ls"), false, true, false);
            var core = new LineScript();
            core.CreatedInstances.Add("this", this);
            core.Run(file.GetString(false, System.Text.Encoding.UTF8));
        }

    }
}
