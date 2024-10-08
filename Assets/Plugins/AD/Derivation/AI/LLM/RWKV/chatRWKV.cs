using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace AD.Derivation.LLM
{
    public class chatRWKV : LLM
    {
        public override VariantSetting GetSetting()
        {
            var setting = base.GetSetting();
            setting.Settings = new()
            {
                { "SystemSetting", m_SystemSetting },
                { "gptModel", m_gptModel }
            };
            return setting;
        }

        public override void InitVariant(VariantSetting setting)
        {
            base.InitVariant(setting);
            m_SystemSetting = setting.Settings["SystemSetting"];
            m_gptModel = setting.Settings["gptModel"];
        }

        public chatRWKV()
        {
            url = "http://127.0.0.1:8000/v1/chat/completions";
        }

        /// <summary>
        /// AI设定
        /// </summary>
        public string m_SystemSetting = string.Empty;
        /// <summary>
        /// gpt-3.5-turbo
        /// </summary>
        public string m_gptModel = "RWKV";

        private void Start()
        {
            m_DataList.Add(new SendData("system", m_SystemSetting));
        }

        public override void PostMessage(string _msg, Action<string> _callback)
        {
            base.PostMessage(_msg, _callback);
        }

        public override IEnumerator Request(string _postWord, System.Action<string> _callback)
        {
            stopwatch.Restart();
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                PostData _postData = new PostData
                {
                    model = m_gptModel,
                    messages = m_DataList
                };

                string _jsonText = JsonUtility.ToJson(_postData);
                byte[] data = System.Text.Encoding.UTF8.GetBytes(_jsonText);
                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
                request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

                request.SetRequestHeader("Content-Type", "application/json");
                //request.SetRequestHeader("Authorization", string.Format("Bearer {0}", api_key));

                yield return request.SendWebRequest();

                if (request.responseCode == 200)
                {
                    string _msgBack = request.downloadHandler.text;
                    MessageBack _textback = JsonUtility.FromJson<MessageBack>(_msgBack);
                    if (_textback != null && _textback.choices.Count > 0)
                    {

                        string _backMsg = _textback.choices[0].message.content;
                        //添加记录
                        m_DataList.Add(new SendData("assistant", _backMsg));
                        _callback(_backMsg);
                    }
                }
                else
                {
                    string _msgBack = request.downloadHandler.text;
                    Debug.LogError(_msgBack);
                }

                stopwatch.Stop();
                Debug.Log("RWKV耗时：" + stopwatch.Elapsed.TotalSeconds);
            }
        }

        #region 数据包

        [Serializable]
        public class PostData
        {
            public string model;
            public List<SendData> messages;
        }


        [Serializable]
        public class MessageBack
        {
            public string id;
            public string created;
            public string model;
            public List<MessageBody> choices;
        }
        [Serializable]
        public class MessageBody
        {
            public Message message;
            public string finish_reason;
            public string index;
        }
        [Serializable]
        public class Message
        {
            public string role;
            public string content;
        }

        #endregion

    }
}
