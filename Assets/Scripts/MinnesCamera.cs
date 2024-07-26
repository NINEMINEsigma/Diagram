using System;
using Diagram;
using UnityEngine;

namespace Game
{
    [Serializable]
    public class MinnesCamera : MinnesController, IOnDependencyCompleting
    {
        private void Start()
        {
            this.RegisterControllerOn(typeof(Minnes), null, typeof(Minnes.StartRuntimeCommand)); 
        }
        public void OnDependencyCompleting()
        {
            LineScript.RunScript("Camera.ls", ("this", this));
        }
        public Camera GetCamera() => this.SeekComponent<Camera>();
        public void SetPerspective() => GetCamera().orthographic = false;
        public void SetOrthographic() => GetCamera().orthographic = true;
        public void SetFieldOfView(float value) => GetCamera().fieldOfView = value;
        public void SetOrthographicSize(float value) => GetCamera().orthographicSize = value;
        public void SetNear(float near) => GetCamera().nearClipPlane = near;
        public void SetFar(float far) => GetCamera().farClipPlane = far;
    }
}
