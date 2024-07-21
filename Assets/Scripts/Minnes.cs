using System;
using System.Collections;
using System.Collections.Generic;
using AD.Utility;
using Diagram;
using UnityEngine;
using UnityEngine.Events;

namespace Game
{
    [Serializable]
    public class TimeEvent:UnityEvent
    {
        public int FocusTime;
        public override int GetHashCode()
        {
            return FocusTime;
        }
    }

    public class Minnes : MonoBehaviour
    {
        private void Awake()
        {
            ArchitectureDiagram.RegisterArchitecture(this);
            CurrentTick = 0;
        }

        private static int RawCurrentTick;
        public static int CurrentTick
        {
            get => RawCurrentTick;
            set
            {
                RawCurrentTick = value;
            }
        }
        public List<TimeEvent> TimeEvents = new();
        public int TimePointer;
    }

    public class MinnesController:MonoBehaviour
    {
        public void SetPosition(float x,float y,float z)
        {
            this.transform.position = new Vector3(x, y, z);
        }

        public void SetRotation(float x,float y,float z)
        {
            this.transform.eulerAngles = new Vector3(x,y,z);
        }

        public void SetScale(float x,float y,float z)
        {
            this.transform.localScale = new Vector3(x,y,z);
        }


        public void Movement(int startTime, int endTime, float x, float y, float z, float x2, float y2, float z2, int easeType = 0)
        {
            EaseCurveType ease = (EaseCurveType)easeType;

        }

        virtual protected void Update()
        {
                
        }
    }
}
