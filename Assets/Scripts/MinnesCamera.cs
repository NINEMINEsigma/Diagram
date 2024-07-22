using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Diagram;
using UnityEngine;

namespace Game
{
    [Serializable]
    public class MinnesCamera : MinnesController
    {
        private void Start()
        {
            this.RegisterControllerOn(typeof(Minnes), new(), typeof(Minnes.StartRuntimeCommand));
            using ToolFile file = new(Path.Combine(ToolFile.userPath, "Camera.ls"), false, true, false); 
            new LineScript(("this", this)).Run(file.GetString(false, System.Text.Encoding.UTF8));
            Minnes.MinnesInstance.AllControllers.Add(this);
        }
        public Camera GetCamera() => this.SeekComponent<Camera>();
        public void SetPerspective() => GetCamera().orthographic = false;
        public void SetOrthographic() => GetCamera().orthographic = true;
        public void SetFieldOfView(float value) => GetCamera().fieldOfView = value;

        private void Update()
        {
            Minnes.MinnesInstance.CurrentTick = 500;
        }
    }
}
