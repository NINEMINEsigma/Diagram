using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;

namespace Diagram
{
    [Serializable]
    [AddComponentMenu("Diagram/AudioSystem", 100)]
    public class AudioSystem : LineBehaviour
    {
        #region Attribute
        [SerializeField] private AudioSource source;
        public AudioSource Source
        {
            get
            {
                if (source == null)
                    source = this.SeekComponent<AudioSource>();
                if (source == null)
                    source = this.gameObject.AddComponent<AudioSource>();
                return source;
            }
            set
            {
                Stop();
                AudioClip clip = source.clip;
                source = value;
                CurrentClip = clip;
            }
        }
        private float CurrentClock = 0;
        private float delay = 0;
        private bool IsDelayToStart = false;
        private bool IsNowPlaying = false; 

        public AudioClip CurrentClip
        {
            get => Source.clip;
            set
            {
                Source.clip = value;
                Refresh();
                Stop();
            }
        }
        public bool IsPlay
        {
            get { return (Source.isPlaying || IsDelayToStart) && !IsPause; }
            set
            {
                if (CurrentClip == null) return;
                if (value) Play();
                else Pause();
            }
        }
        public bool IsPause { get; private set; } = false;
        public float CurrentTime
        {
            get { return CurrentClock; }
            set
            {
                CurrentClock = value;
                Source.time = Mathf.Clamp(value, 0, (CurrentClip == null) ? 0 : CurrentClip.length);
                delay = Mathf.Clamp(-value, 0, Mathf.Infinity);
                if (value < 0 && IsPlay)
                {
                    Stop();
                    Play();
                }
            }
        }
        public float CurrentDelay => (IsDelayToStart) ? delay : 0;

        public AudioMixer Mixer = null;
        public bool Sampling = false;

        public LineRenderer MyLineRenderer = null;

        #endregion

        #region Function

        private void Awake()
        {
            OnValidate();
            GetSampleCount();
        }

        private void OnValidate()
        {
            if (samples.Length != spectrumLength) samples = new float[spectrumLength];
            if (bands.Length != BandCount) bands = new float[BandCount];
            if (freqBands.Length != BandCount) freqBands = new float[BandCount];
            if (bandBuffers.Length != BandCount) bandBuffers = new float[BandCount];
            if (bufferDecrease.Length != BandCount) bufferDecrease = new float[BandCount];
            if (bandHighest.Length != BandCount) bandHighest = new float[BandCount];
            if (normalizedBands.Length != BandCount) normalizedBands = new float[BandCount];
            if (normalizedBandBuffers.Length != BandCount) normalizedBandBuffers = new float[BandCount];
            if (sampleCount.Length != BandCount) sampleCount = new int[BandCount];
        }

        private void Update()
        {
            if (Source.clip == null)
            {
                if (CurrentClip != null)
                    Refresh();
                return;
            } 
            if (Sampling)
                WhenSampling();
            if (MyLineRenderer != null)
                MyLineRenderer.gameObject.SetActive(!(!Sampling || !DrawingLine));
            if (IsNowPlaying)
                WhenPlaying();
            if (IsNowPlaying && IsDelayToStart)
                WhenDelayCounting();

            void WhenSampling()
            {
                GetSpectrums();
                GetFrequencyBands();
                GetNormalizedBands();
                GetBandBuffers(increasingType, decreasingType);
                BandNegativeCheck();
                if (DrawingLine)
                    OnDrawLineRenderer();
            }

            void WhenPlaying()
            {
                WhenNeedUpdataCurrentClock();
            }

            void WhenNeedUpdataCurrentClock()
            {
                if (IsPause == false && IsDelayToStart == false)
                {
                    CurrentClock = (float)Source.timeSamples / (float)Source.clip.frequency;
                }
            }

            void WhenNeedUpdataDelay()
            {
                if (IsPause == false)
                    delay -= Time.deltaTime;
            }

            void WhenDelayCounting()
            {
                WhenNeedUpdataDelay();
                if (delay <= 0)
                {
                    IsDelayToStart = false;
                    Play();
                }
            }
        }

        public virtual void OnDrawLineRenderer()
        {
            int vextcount = normalizedBands.Length;
            Keyframe[] keyframes = new Keyframe[vextcount * 2];
            Vector3[] vexts = new Vector3[vextcount];
            vextcount = (int)BandCount;
            for (int i = 0; i < vextcount; i++)
                vexts[i] = transform.position +
                    10 * new Vector3(Mathf.Cos(i / (float)vextcount * 2 * Mathf.PI), Mathf.Sin(i / (float)vextcount * 2 * Mathf.PI));
            MyLineRenderer.positionCount = vextcount;
            MyLineRenderer.SetPositions(vexts);
            AnimationCurve m_curve = AnimationCurve.Linear(0, 1, 1, 1);
            for (int i = 0; i < vextcount; i++)
            {
                keyframes[i].time = i / (float)(vextcount * 2);
                keyframes[i].value = Mathf.Clamp(bands[i], 0.01f, 100);
                keyframes[^(i + 1)].time = 1 - i / (float)(vextcount * 2);
                keyframes[^(i + 1)].value = Mathf.Clamp(bands[i], 0.01f, 100);
            }
            m_curve.keys = keyframes;
            MyLineRenderer.widthCurve = m_curve;
        }

        public void Play()
        {
            IsPause = false;
            IsNowPlaying = true;
            if (delay > 0)
            {
                IsDelayToStart = true;
                return;
            }
            Source.Play();
        }
        public void Stop()
        {
            IsPause = false;
            IsNowPlaying = false;
            Source.Stop();
            delay = 0;
            IsDelayToStart = false;
            CurrentClock = 0;
        }
        public void Pause()
        {
            IsPause = true;
            IsNowPlaying = false;
            Source.Pause();
            return;
        }
        public void PlayOrPause()
        {
            IsPlay = !IsPlay;
        }

        public void Refresh()
        {
            Source.clip = CurrentClip;
        }

        public void IgnoreListenerPause()
        {
            Source.ignoreListenerPause = true;
        }
        public void SubscribeListenerPause()
        {
            Source.ignoreListenerPause = false;
        }

        public void IgnoreListenerVolume()
        {
            Source.ignoreListenerVolume = true;
        }
        public void SubscribeListenerVolume()
        {
            Source.ignoreListenerVolume = false;
        }

        public void SetLoop()
        {
            Source.loop = true;
        }
        public void UnLoop()
        {
            Source.loop = false;
        }  

        public void SetMute()
        {
            Source.mute = true;
        }
        public void CancelMute()
        {
            Source.mute = false;
        }

        public void SetPitch(float pitch)
        {
            Source.pitch = pitch;
        }

        public void SetSpeed(float speed,
                             float TargetPitchValue = 1.0f,
                             string TargetGroupName = "Master",
                             string TargetPitch_Attribute_Name = "MasterPitch",
                             string TargetPitchshifterPitch_Attribute_Name = "PitchShifterPitch")
        {
            if (Mixer != null)
            {
                if (TargetPitchValue > 0)
                {
                    Source.pitch = 1;
                    Mixer.SetFloat(TargetPitch_Attribute_Name, TargetPitchValue);
                    float TargetPitchshifterPitchValue = 1.0f / TargetPitchValue;
                    Mixer.SetFloat(TargetPitchshifterPitch_Attribute_Name, TargetPitchshifterPitchValue);
                    Source.outputAudioMixerGroup = Mixer.FindMatchingGroups(TargetGroupName)[0];
                }
                else
                {
                    Source.pitch = -1;
                    Mixer.SetFloat(TargetPitch_Attribute_Name, -TargetPitchValue);
                    float TargetPitchshifterPitchValue = -1.0f / TargetPitchValue;
                    Mixer.SetFloat(TargetPitchshifterPitch_Attribute_Name, TargetPitchshifterPitchValue);
                    Source.outputAudioMixerGroup = Mixer.FindMatchingGroups(TargetGroupName)[0];
                }
            }
            else
            {
                Debug.LogWarning("you try to change an Audio's speed without AudioMixer, which will cause it to change its pitch");
                SetPitch(speed);
            }
        }

        public void SetVolume(float volume)
        {
            Source.volume = volume;
        }

        public void SetPriority(int priority)
        {
            Source.priority = priority;
        }

        public void PrepareToOtherScene()
        {
            StartCoroutine(ClockOnJump());
            IEnumerator ClockOnJump()
            {
                transform.parent = null;
                DontDestroyOnLoad(gameObject);
                for (float now = 0; now < 1; now += UnityEngine.Time.deltaTime)
                {
                    this.SetVolume(1 - now);
                    yield return new WaitForEndOfFrame();
                }
                Destroy(gameObject);
            }
        } 

        #endregion

        #region Inspector
        [Header("MusicSampler")]
        public bool DrawingLine = false;
        /// <summary>
        /// 这个参数用于设置进行采样的精度
        /// </summary>
        [Tooltip("采样精度")] public SpectrumLength SpectrumCount = SpectrumLength.Spectrum256;
        private int spectrumLength => (int)Mathf.Pow(2, ((int)SpectrumCount + 6));
        /// <summary>
        /// 这个属性返回采样得到的原始数据
        /// </summary>
        [Tooltip("原始数据")] public float[] samples = new float[64];
        private int[] sampleCount = new int[8];
        /// <summary>
        /// 这个参数用于设置将采样的结果分为几组进行讨论
        /// </summary>
        [Tooltip("拆分组数")] public uint BandCount = 8;
        /// <summary>
        /// 这个参数用于设置组别采样数值减小时使用的平滑策略
        /// </summary>
        [Tooltip("平衡采样值滑落的平滑策略")] public BufferDecreasingType decreasingType = BufferDecreasingType.Jump;
        /// <summary>
        /// 这个参数用于设置在Slide和Falling设置下，组别采样数值减小时每帧下降的大小。
        /// </summary>
        [Tooltip("Slide/Falling:采样值的滑落幅度")] public float decreasing = 0.003f;
        /// <summary>
        /// 这个参数用于设置在Falling设置下，组别采样数值减小时每帧下降时加速度的大小。
        /// </summary>
        [Tooltip("Falling:采样值滑落的加速度")] public float DecreaseAcceleration = 0.2f;
        /// <summary>
        /// 这个参数用于设置组别采样数值增大时使用的平滑策略
        /// </summary>
        [Tooltip("平衡采样值提升的平滑策略")] public BufferIncreasingType increasingType = BufferIncreasingType.Jump;
        /// <summary>
        /// 这个参数用于设置在Slide设置下，组别采样数值增大时每帧增加的大小。
        /// </summary>
        [Tooltip("Slide:采样值的提升幅度")] public float increasing = 0.003f;
        /// <summary>
        /// 这个属性返回经过平滑和平均的几组数据
        /// </summary>
        [Tooltip("经处理后的数据")] public float[] bands = new float[8];
        private float[] freqBands = new float[8];
        private float[] bandBuffers = new float[8];
        private float[] bufferDecrease = new float[8];
        /// <summary>
        /// 这个属性返回总平均采样结果
        /// </summary>
        public float average
        {
            get
            {
                float average = 0;
                for (int i = 0; i < BandCount; i++)
                {
                    average += normalizedBands[i];
                }
                average /= BandCount;
                return average;
            }
        }

        private float[] bandHighest = new float[8];
        /// <summary>
        /// 这个属性返回经过平滑、平均和归一化的几组数据
        /// </summary>
        [Tooltip("经过平滑、平均和归一化的几组数据")] public float[] normalizedBands = new float[8];
        private float[] normalizedBandBuffers = new float[8];

        #endregion  

        #region Programs

        private void GetSampleCount()
        {
            float acc = (((float)((int)SpectrumCount + 6)) / BandCount);
            int sum = 0;
            int last = 0;
            for (int i = 0; i < BandCount - 1; i++)
            {
                int pow = (int)Mathf.Pow(2, acc * (i));
                sampleCount[i] = pow - sum;
                if (sampleCount[i] < last) sampleCount[i] = last;
                sum += sampleCount[i];
                last = sampleCount[i];
            }
            sampleCount[BandCount - 1] = samples.Length - sum;
        }

        private void GetSpectrums()
        {
            Source.GetSpectrumData(samples, 0, FFTWindow.Blackman);
        }

        private void GetFrequencyBands()
        {
            int counter = 0;
            for (int i = 0; i < BandCount; i++)
            {
                float average = 0;
                for (int j = 0; j < sampleCount[i]; j++)
                {
                    average += samples[counter] * (counter + 1);
                    counter++;
                }
                average /= sampleCount[i];
                freqBands[i] = average * 10;
            }
        }

        private void GetNormalizedBands()
        {
            for (int i = 0; i < BandCount; i++)
            {
                if (freqBands[i] > bandHighest[i])
                {
                    bandHighest[i] = freqBands[i];
                }
            }
        }

        private void GetBandBuffers(BufferIncreasingType increasingType, BufferDecreasingType decreasingType)
        {
            for (int i = 0; i < BandCount; i++)
            {
                if (freqBands[i] > bandBuffers[i])
                {
                    switch (increasingType)
                    {
                        case BufferIncreasingType.Jump:
                            bandBuffers[i] = freqBands[i];
                            bufferDecrease[i] = decreasing;
                            break;
                        case BufferIncreasingType.Slide:
                            bufferDecrease[i] = decreasing;
                            bandBuffers[i] += increasing;
                            break;
                    }
                    if (freqBands[i] < bandBuffers[i]) bandBuffers[i] = freqBands[i];
                }
                if (freqBands[i] < bandBuffers[i])
                {
                    switch (decreasingType)
                    {
                        case BufferDecreasingType.Jump:
                            bandBuffers[i] = freqBands[i];
                            break;
                        case BufferDecreasingType.Falling:
                            bandBuffers[i] -= decreasing;
                            break;
                        case BufferDecreasingType.Slide:
                            bandBuffers[i] -= bufferDecrease[i];
                            bufferDecrease[i] *= 1 + DecreaseAcceleration;
                            break;
                    }
                    if (freqBands[i] > bandBuffers[i]) bandBuffers[i] = freqBands[i]; ;
                }
                bands[i] = bandBuffers[i];
                if (bandHighest[i] == 0) continue;
                normalizedBands[i] = (freqBands[i] / bandHighest[i]);
                normalizedBandBuffers[i] = (bandBuffers[i] / bandHighest[i]);
                if (normalizedBands[i] > normalizedBandBuffers[i])
                {
                    switch (increasingType)
                    {
                        case BufferIncreasingType.Jump:
                            normalizedBandBuffers[i] = normalizedBands[i];
                            bufferDecrease[i] = decreasing;
                            break;
                        case BufferIncreasingType.Slide:
                            bufferDecrease[i] = decreasing;
                            normalizedBandBuffers[i] += increasing;
                            break;
                    }
                    if (normalizedBands[i] < normalizedBandBuffers[i]) normalizedBandBuffers[i] = normalizedBands[i];
                }
                if (normalizedBands[i] < normalizedBandBuffers[i])
                {
                    switch (decreasingType)
                    {
                        case BufferDecreasingType.Jump:
                            normalizedBandBuffers[i] = normalizedBands[i];
                            break;
                        case BufferDecreasingType.Falling:
                            normalizedBandBuffers[i] -= decreasing;
                            break;
                        case BufferDecreasingType.Slide:
                            normalizedBandBuffers[i] -= bufferDecrease[i];
                            bufferDecrease[i] *= 1 + DecreaseAcceleration;
                            break;
                    }
                    if (normalizedBands[i] > normalizedBandBuffers[i]) normalizedBandBuffers[i] = normalizedBands[i];
                }
                normalizedBands[i] = normalizedBandBuffers[i];
            }
        }

        private void BandNegativeCheck()
        {
            for (int i = 0; i < BandCount; i++)
            {
                if (bands[i] < 0)
                {
                    bands[i] = 0;
                }
                if (normalizedBands[i] < 0)
                {
                    normalizedBands[i] = 0;
                }
            }
        }

        #endregion

        #region Resource

        public void LoadOnResource(string path)
        {
            CurrentClip = Resources.Load<AudioClip>(path);
        }

        public void LoadOnUrl(string url)
        {
            LoadOnUrl(url, GetAudioType(url));
        }

        public void LoadOnUrl(string url, AudioType audioType)
        {
            StartCoroutine(LoadAudio(url, audioType));
        }

        public IEnumerator LoadAudio(string path, AudioType audioType)
        {
            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(path, audioType);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
                CurrentClip = audioClip;
            }
            else  Debug.LogError(request.result);
        }

        #endregion

        /// <summary>
        /// 通过这个函数来生成一个AudioSource,并初始化其播放的片段为audioClip
        /// </summary>
        /// <param name="audioClip">播放的片段</param>
        /// <returns></returns>
        public static AudioSource CreateSampler(AudioClip audioClip)
        {
            GameObject go = new GameObject("New AudioSource");
            AudioSource asr = go.AddComponent<AudioSource>();
            asr.clip = audioClip;
            asr.loop = false;
            asr.Play();
            return asr;
        }

        /// <summary>
        /// 传入一个AudioClip 会将AudioClip上挂载的音频文件生成频谱到一张Texture2D上
        /// </summary>
        /// <param name="_clip"></param>
        /// <param name="resolution">这个值可以控制频谱的密度</param>
        /// <param name="width">这个是最后生成的Texture2D图片的宽度</param>
        /// <param name="height">这个是最后生成的Texture2D图片的高度</param>
        /// <returns></returns>
        public static Texture2D BakeAudioWaveform(AudioClip _clip, int resolution = 60, int width = 1920, int height = 200)
        {
            resolution = _clip.frequency / resolution;

            float[] samples = new float[_clip.samples * _clip.channels];
            _clip.GetData(samples, 0);

            float[] waveForm = new float[(samples.Length / resolution)];

            float min = 0;
            float max = 0;
            bool inited = false;

            for (int i = 0; i < waveForm.Length; i++)
            {
                waveForm[i] = 0;

                for (int j = 0; j < resolution; j++)
                {
                    waveForm[i] += Mathf.Abs(samples[(i * resolution) + j]);
                }

                if (!inited)
                {
                    min = waveForm[i];
                    max = waveForm[i];
                    inited = true;
                }
                else
                {
                    if (waveForm[i] < min)
                    {
                        min = waveForm[i];
                    }

                    if (waveForm[i] > max)
                    {
                        max = waveForm[i];
                    }
                }
                //waveForm[i] /= resolution;
            }


            Color backgroundColor = Color.black;
            Color waveformColor = Color.green;
            Color[] blank = new Color[width * height];
            Texture2D texture = new Texture2D(width, height);

            for (int i = 0; i < blank.Length; ++i)
            {
                blank[i] = backgroundColor;
            }

            texture.SetPixels(blank, 0);

            float xScale = (float)width / (float)waveForm.Length;

            int tMid = (int)(height / 2.0f);
            float yScale = 1;

            if (max > tMid)
            {
                yScale = tMid / max;
            }

            for (int i = 0; i < waveForm.Length; ++i)
            {
                int x = (int)(i * xScale);
                int yOffset = (int)(waveForm[i] * yScale);
                int startY = tMid - yOffset;
                int endY = tMid + yOffset;

                for (int y = startY; y <= endY; ++y)
                {
                    texture.SetPixel(x, y, waveformColor);
                }
            }

            texture.Apply();
            return texture;
        }
        /// <summary>
        /// 传入一个AudioClip 会将AudioClip上挂载的音频文件生成频谱到一张Texture2D上
        /// </summary>
        /// <param name="_clip"></param>
        /// <param name="resolution">这个值可以控制频谱的密度</param>
        /// <param name="width">这个是最后生成的Texture2D图片的宽度</param>
        /// <param name="height">这个是最后生成的Texture2D图片的高度</param>
        /// <returns></returns>
        public static Texture2D BakeAudioWaveformVertical(AudioClip _clip, int resolution = 60, int width = 200, int height = 1920)
        {
            resolution = _clip.frequency / resolution;

            float[] samples = new float[_clip.samples * _clip.channels];
            _clip.GetData(samples, 0);

            float[] waveForm = new float[(samples.Length / resolution)];

            float min = 0;
            float max = 0;
            bool inited = false;

            for (int i = 0; i < waveForm.Length; i++)
            {
                waveForm[i] = 0;

                for (int j = 0; j < resolution; j++)
                {
                    waveForm[i] += Mathf.Abs(samples[(i * resolution) + j]);
                }

                if (!inited)
                {
                    min = waveForm[i];
                    max = waveForm[i];
                    inited = true;
                }
                else
                {
                    if (waveForm[i] < min)
                    {
                        min = waveForm[i];
                    }

                    if (waveForm[i] > max)
                    {
                        max = waveForm[i];
                    }
                }
                //waveForm[i] /= resolution;
            }


            Color backgroundColor = Color.black;
            Color waveformColor = Color.green;
            Color[] blank = new Color[width * height];
            Texture2D texture = new Texture2D(width, height);

            for (int i = 0; i < blank.Length; ++i)
            {
                blank[i] = backgroundColor;
            }

            texture.SetPixels(blank, 0);

            float xScale = (float)height / (float)waveForm.Length;

            int tMid = (int)(width / 2.0f);
            float yScale = 1;

            if (max > tMid)
            {
                yScale = tMid / max;
            }

            for (int i = 0; i < waveForm.Length; ++i)
            {
                int x = (int)(i * xScale);
                int yOffset = (int)(waveForm[i] * yScale);
                int startY = tMid - yOffset;
                int endY = tMid + yOffset;

                for (int y = startY; y <= endY; ++y)
                {
                    texture.SetPixel(y, x, waveformColor);
                }
            }

            texture.Apply();
            return texture;
        }

        public static AudioType GetAudioType(string path)
        {
            return Path.GetExtension(path) switch
            {
                "wav" => AudioType.WAV,
                "mp3" => AudioType.MPEG,
                "ogg" => AudioType.OGGVORBIS,
                _ => AudioType.UNKNOWN
            };
        }
    }

    public enum SpectrumLength
    {
        Spectrum64, Spectrum128, Spectrum256, Spectrum512, Spectrum1024, Spectrum2048, Spectrum4096, Spectrum8192
    }

    public enum BufferDecreasingType
    {
        Jump, Slide, Falling
    }

    public enum BufferIncreasingType
    {
        Jump, Slide
    }
}
