using System.Collections.Generic;
using AD.UI;
using Diagram;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    public class MinnesTimeline : LineBehaviour, IOnDependencyCompleting
    {
        public static MinnesTimeline instance;

        public AudioSystem ASC => Minnes.MinnesInstance.ASC;
        public float CurrnetTime => Minnes.MinnesInstance.CurrentTick;
        //public float Offset=>
        public RawImage TimeLineRawImage;
        public float TimeLineDisplayLength = 3;
        public RawImage BarlineRawImage;
        public ModernUIFillBar TimeLineBar;
        public ModernUIButton Stats;


        public void SetTimeDisplayLength(float length)
        {
            this.Architecture<Minnes>().GetController<MinnesTimeline>().To<MinnesTimeline>().TimeLineDisplayLength = length;
        }

        public List<Color[]> BarLineColorsList;
        public int BarColorPointer = 0;

        public static Texture2D BakeAudioWaveformBarline(float bpm, float songLength, int width, int height, params Color[] barLineColors)
        {
            Texture2D texture = new Texture2D(width, height);
            Color backgroundColor = new(0, 0, 1, 0);
            Color[] blank = new Color[width * height];
            for (int i = 0; i < blank.Length; ++i)
            {
                blank[i] = backgroundColor;
            }
            texture.SetPixels(blank, 0);
            float oneBarScale = 60.0f / bpm / songLength * height / (float)barLineColors.Length;
            int index = 0;
            while (index * oneBarScale < height)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, (int)(index * oneBarScale), barLineColors[index % barLineColors.Length]);
                }
                index++;
            }
            for (int x = 1; x < 13; x++)
            {
                for (int y = 0; y < 0; y++)
                {

                }
            }
            texture.Apply();
            return texture;
        }

        public void OnDependencyCompleting()
        {
            BarLineColorsList = new()
            {
                new Color[]{ Color.white},
                new Color[]{
                    Color.white
                    ,Color.blue,
                    Color.red
                    ,Color.blue},
                new Color[]{
                    Color.white, Color.green
                    , Color.blue, Color.green,
                    Color.red, Color.green
                    , Color.blue, Color.green },
                new Color[]{
                    Color.white,Color.yellow,Color.green, Color.yellow,
                    Color.blue, Color.yellow,Color.green, Color.yellow,
                    Color.red,  Color.yellow, Color.green,Color.yellow,
                    Color.blue, Color.yellow,Color.green, Color.yellow,}
            };
            TimeLineRawImage.MainTex = AudioSourceController.BakeAudioWaveformVertical(ASC.CurrentClip, 60, 300, 4000);
            BarlineRawImage.MainTex = BakeAudioWaveformBarline(Minnes.ProjectBPM, ASC.CurrentClip.length, 1, 16384, BarLineColorsList[BarColorPointer]);
            TimeLineBar.Set(0, ASC.CurrentClip.length);
            TimeLineBar.IsInt = false;
            TimeLineBar.OnTransValueChange.AddListener(T =>
            {
                if (ASC.IsPlay == false)
                    ASC.CurrentTime = T;
            });
            Stats.ButtonText = Minnes.ProjectName;
            LineScript.RunScript("TimeLine.ls", ("this", this));
        }

        private void Start()
        {
            instance = this;
            this.RegisterControllerOn(typeof(Minnes), new(), typeof(Minnes.StartRuntimeCommand));
        }

        private void Update()
        {
            if (Keyboard.current[Key.LeftCtrl].isPressed && Keyboard.current[Key.T].wasPressedThisFrame)
            {
                if (Keyboard.current[Key.A].isPressed)
                {
                    TimeLineRawImage.transform.As<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, 1600);
                    TimeLineTable.instance.rects = Diagram.RectTransformExtension.GetRect(TimeLineTable.instance.transform.As<RectTransform>());
                }
                else if (Keyboard.current[Key.LeftAlt].isPressed)
                {
                    TimeLineRawImage.transform.As<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, 300 );
                    TimeLineTable.instance.rects = Diagram.RectTransformExtension.GetRect(TimeLineTable.instance.transform.As<RectTransform>());
                }
                else
                {
                    TimeLineRawImage.gameObject.SetActive(!TimeLineRawImage.gameObject.activeSelf);
                    TimeLineBar.gameObject.SetActive(!TimeLineBar.gameObject.activeSelf);
                    Stats.gameObject.SetActive(!Stats.gameObject.activeSelf);
                }
            }
            if (Keyboard.current[Key.LeftCtrl].isPressed && Keyboard.current[Key.Space].wasPressedThisFrame)
            {
                ASC.PlayOrPause();
                this.TimeLineBar.IsLockByScript = ASC.IsPlay;
            }
            if (Keyboard.current[Key.LeftCtrl].isPressed && Keyboard.current[Key.S].wasPressedThisFrame)
            {
                ASC.Stop();
                ASC.CurrentTime = 0;
                this.TimeLineBar.IsLockByScript = ASC.IsPlay;
            }
            if (Keyboard.current[Key.LeftCtrl].isPressed && Keyboard.current[Key.P].wasPressedThisFrame)
            {
                ASC.Stop();
                ASC.Play();
            }
            if (Keyboard.current[Key.LeftCtrl].isPressed && Keyboard.current[Key.Z].wasPressedThisFrame)
            {
                BarColorPointer += Keyboard.current[Key.LeftAlt].isPressed ? -1 : 1;
                BarColorPointer = BarColorPointer % BarLineColorsList.Count;
                BarlineRawImage.MainTex = BakeAudioWaveformBarline(Minnes.ProjectBPM, ASC.CurrentClip.length, 1, 16384, BarLineColorsList[BarColorPointer]);
            }
            if (Keyboard.current[Key.LeftCtrl].isPressed && Keyboard.current[Key.A].isPressed)
            {
                TimeLineDisplayLength = Mathf.Clamp(TimeLineDisplayLength + Time.deltaTime * Mouse.current.scroll.ReadValue().y * 0.1f, 0.1f, ASC.CurrentClip.length);
            }

            try
            {
                Rect rawImageRect = new(0, CurrnetTime / ASC.CurrentClip.length, 1, TimeLineDisplayLength / ASC.CurrentClip.length);
                this.TimeLineRawImage.source.uvRect = rawImageRect;
                this.BarlineRawImage.source.uvRect = rawImageRect;
                if (ASC.IsPlay)
                    this.TimeLineBar.SetValue(CurrnetTime);
            }
            catch { }
        }

    }
}
