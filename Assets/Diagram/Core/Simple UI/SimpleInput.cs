using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using Diagram.Arithmetic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using System.Linq;

namespace Diagram.UI
{
    /// <summary>
    /// this<para></para>
    /// -->Icon, Input, Text
    /// </summary>
    [Serializable]
    public class SimpleInput : LineBehaviour
    {
        [SerializeField] private Image m_Icon;
        public Image Icon
        {
            get
            {
                if (m_Icon == null)
                    m_Icon = this.SeekComponent<Image>();
                return m_Icon;
            }
        }
        [SerializeField] private TMP_InputField m_InputField;
        public TMP_InputField InputField
        {
            get
            {
                if (m_InputField == null)
                    m_InputField = this.SeekComponent<TMP_InputField>();
                return m_InputField;
            }
        }
        [SerializeField] private TMP_Text m_Title;
        public TMP_Text Title
        {
            get
            {
                if (m_Title == null)
                    m_Title = this.SeekComponent<TMP_Text>();
                return m_Title;
            }
        }

        public void SetText(string str, bool isNotify = true)
        {
            if (isNotify)
                InputField.text = str;
            else
                InputField.SetTextWithoutNotify(str);
        }

        public void SetTitle(string str)
        {
            Title.SetText(str);
        }

        public void SetIcon(Sprite icon)
        {
            Icon.sprite = icon;
        }

        public void Binding(string member, int sort, params object[] target)
        {
            Binding(DiagramType.GetOrCreateDiagramType(target[0].GetType()).GetMember(member), sort, target);
        }
        public void Binding(DiagramMember member,int sort,params object[] target)
        {
            BindingActions.Add(sort, T =>
            {
                try
                {
                    member.reflectedMember.SetValueFromString(T, target);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            });
        }
        private Dictionary<int, Action<string>> BindingActions = new();
        public void RemoveBinding(int sort)
        {
            BindingActions.Remove(sort);
        }
        public void RemoveAllBinding()
        {
            BindingActions.Clear();
        }
        private void OnEditEnd(string input)
        {
            foreach (var action in BindingActions)
            {
                action.Value.Invoke(input);
            }
        }
        private void OnEnable()
        {
            if(InputField!=null)
            {
                InputField.onEndEdit.AddListener(OnEditEnd);
            }
        }
        private void OnDisable()
        {
            InputField.onEndEdit.RemoveListener(OnEditEnd);
        }
    }
}
