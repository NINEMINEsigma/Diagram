﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AD.BASE;
using AD.Math;
using AD.Reflection;
using AD.Types;
using AD.UI;
using AD.Utility;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using static AD.Derivation.GameEditor.GUIContent;

namespace AD.Derivation.GameEditor
{
    public class GUI
    {
        public static GUISkin skin;
    }

    public class GUIContent
    {
        public GameObject RootObject;
        public IADUI TargetItem;
        public GUIContentType ContentType = GUIContentType.Default;
        public int ExtensionalSpaceLine = 0;
        public string Message;

        public enum GUIContentType
        {
            Space,
            Default
        }

        public GUIContent(GameObject root, IADUI targetItem, GUIContentType contentType = GUIContentType.Default, int extensionalSpaceLine = 0, string message = "")
        {
            RootObject = root;
            TargetItem = targetItem;
            ContentType = contentType;
            ExtensionalSpaceLine = extensionalSpaceLine;
            Message = message;
        }
    }

    public static class PropertiesLayout
    {
        private static bool IsApply = true;

        internal static PropertiesItem CurrentEditorMatchItem;

        private static List<List<GUIContent>> GUILayoutLineList = new();
        private static bool IsNeedMulLine = true;
        private static void DoGUILine(GUIContent content)
        {
            if (!IsNeedMulLine)
            {
                GUILayoutLineList[^1].Add(content);
            }
            else
            {
                GUILayoutLineList.Add(new() { content });
            }
        }
        public static IADUI GUIField(string text, string style, string message = "", bool IsHorizontal = true)
        {
            GameObject root = ObtainGUIStyleInstance(style, out IADUI targeADUI);
            int extensionalSpaceLine = ((int)root.transform.As<RectTransform>().sizeDelta.y / (int)PropertiesItem.DefaultRectHightLevelSize) - 1;
            GUIContent content = new(root, targeADUI, GUIContentType.Default, extensionalSpaceLine, message);
            if (extensionalSpaceLine > 0)
            {
                EndHorizontal();
            }
            if (IsHorizontal) BeginHorizontal();
            GameObject labelRoot = GUI.skin.FindStyle("Label(UI)").Prefab.PrefabInstantiate();
            DoGUILine(new GUIContent(labelRoot, labelRoot.GetComponentInChildren<AD.UI.Text>().SetText(text.Translate()), GUIContentType.Default, 0, message));
            if (extensionalSpaceLine > 0)
            {
                EndHorizontal();
            }
            DoGUILine(content);
            if (IsHorizontal) EndHorizontal();
            return targeADUI;
        }
        public static IADUI GUIField(string style, string message = "")
        {
            GameObject root = ObtainGUIStyleInstance(style, out IADUI targeADUI);
            int extensionalSpaceLine = ((int)root.transform.As<RectTransform>().sizeDelta.y / (int)PropertiesItem.DefaultRectHightLevelSize) - 1;
            GUIContent content = new(root, targeADUI, GUIContentType.Default, extensionalSpaceLine, message);
            DoGUILine(content);
            return targeADUI;
        }
        public static void Space(int line)
        {
            GUIContent content = new(null, null, GUIContentType.Space, line, "");
            DoGUILine(content);
        }
        private static GameObject ObtainGUIStyleInstance(string style, out IADUI targetUI)
        {
            GUIStyle targetStyle = ADGlobalSystem.FinalCheckWithThrow(GUI.skin.FindStyle(style), "cannt find this GUIStyle");
            GameObject cat = targetStyle.Prefab.PrefabInstantiate();
            var targetADUIs = cat.GetComponents<IADUI>();
            if (targetADUIs == null || targetADUIs.Length == 0) targetADUIs = cat.GetComponentsInChildren<IADUI>();
            if (targetADUIs == null || targetADUIs.Length == 0)
            {
                GameEditorApp.instance.AddMessage("PropertiesLayout.ObtainGUIStyleInstance Error");
                targetUI = null;
                return null;
            }
            targetUI = targetADUIs.FirstOrDefault(T => T.GetType().Name == targetStyle.TypeName);
            return cat;
        }

        public static void SetUpPropertiesLayout(ISerializePropertiesEditor target)
        {
            CurrentEditorMatchItem = ADGlobalSystem.FinalCheck(target).MatchItem;
            CurrentEditorMatchItem.Init();
            IsNeedMulLine = true;
            if (!IsApply)
            {
                foreach (var GUILine in GUILayoutLineList)
                {
                    foreach (var content in GUILine)
                    {
                        GameObject.Destroy(content.RootObject);
                    }
                }
            }
            IsApply = false;
        }

        public static void SetUpPropertiesLayout(PropertiesItem target)
        {
            CurrentEditorMatchItem = target;
            CurrentEditorMatchItem.Init();
            foreach (var items in GUILayoutLineList)
            {
                foreach (GUIContent item in items)
                {
                    GameObject.Destroy(item.RootObject);
                }
            }
            IsNeedMulLine = true;
            IsApply = false;
        }

        public static void ApplyPropertiesLayout()
        {
            try
            {
                foreach (var line in GUILayoutLineList)
                {
                    RectTransform rect = null;
                    rect = CurrentEditorMatchItem.AddNewLevelLine(true, 1);
                    rect.GetComponent<AreaDetecter>().Message = line[0].Message;
                    rect.name = $"<{line[0].TargetItem.GetType().Name ?? ""}>{line[0].Message}";
                    int LineItemCount = line.Count;
                    int extensionalSpaceLine = 0;
                    foreach (var content in line)
                    {
                        switch (content.ContentType)
                        {
                            case GUIContent.GUIContentType.Space:
                                {
                                    CurrentEditorMatchItem.AddNewLevelLine(false, content.ExtensionalSpaceLine);
                                }
                                break;
                            default:
                                {
                                    if (content.ExtensionalSpaceLine > extensionalSpaceLine) extensionalSpaceLine = content.ExtensionalSpaceLine;
                                    content.RootObject.transform.SetParent(rect, false);
                                    var contentRect = content.RootObject.transform.As<UnityEngine.RectTransform>();
                                    contentRect.sizeDelta = new UnityEngine.Vector2(rect.sizeDelta.x / (float)LineItemCount, contentRect.sizeDelta.y);
                                }
                                break;
                        }
                    }
                    if (extensionalSpaceLine > 0)
                        CurrentEditorMatchItem.AddNewLevelLine(false, extensionalSpaceLine);
                }
                GUILayoutLineList.Clear();
            }
            catch (Exception ex)
            {
                GameEditorApp.instance.GetController<Information>().Error("ApplyPropertiesLayout Failed : " + ex.Message);
                foreach (var items in GUILayoutLineList)
                {
                    foreach (GUIContent item in items)
                    {
                        GameObject.Destroy(item.RootObject);
                    }
                }
            }
            finally
            {
                CurrentEditorMatchItem = null;
                EndHorizontal();
                IsApply = true;
            }
        }

        //public static T GenerateElement<T>() where T : ADUI
        //{
        //    T element = ADGlobalSystem.FinalCheck<T>(ADGlobalSystem.GenerateElement<T>(), "On PropertiesLayout , you try to obtain a null object with some error");
        //}

        public static IADUI ObjectField(string text, string style, string message = "", bool IsHorizontal = true) => GUIField(text, style, message, IsHorizontal);
        public static IADUI ObjectField(string style, string message = "") => GUIField(style, message);

        public static T CField<T>(string text, string message = "", bool IsHorizontal = true) where T : IADUI
        {
            return (T)GUIField(text, typeof(T).Name, message, IsHorizontal);
        }
        public static T CField<T>(string message = "") where T : IADUI
        {
            return (T)GUIField(typeof(T).Name, message);
        }


        #region L

        public static IButton Button(string buttonText, bool isModernUI, string message, UnityAction action)
        {
            return GUIField(isModernUI ? "Button(ModernUI)" : "Button(UI)", message).As<IButton>().SetTitle(buttonText).AddListener(action);
        }
        public static Button Button(string buttonText, string message, UnityAction action)
        {
            return Button(buttonText, false, message, action) as AD.UI.Button;
        }
        public static ModernUIButton ModernUIButton(string buttonText, string message, UnityAction action)
        {
            return Button(buttonText, true, message, action) as AD.UI.ModernUIButton;
        }

        public enum TextType
        {
            Text, Title, Label
        }
        public static Text Text(string text, TextType type, string message)
        {
            var cat = GUIField(type.ToString() + "(UI)", message).As<Text>().SetText(text);
            return cat;
        }
        public static Text Label(string text, string message)
        {
            return Text(text, TextType.Label, message);
        }
        public static Text Label(string text)
        {
            return Text(text, TextType.Label, text);
        }
        public static Text Title(string text)
        {
            return Text(text, text);
        }
        public static Text Title(string text, string message)
        {
            return Text(text, TextType.Title, message);
        }
        public static Text Text(string text, string message)
        {
            return Text(text, TextType.Text, message);
        }

        public static InputField InputField(string text, string placeholderText, string message)
        {
            var cat = GUIField("InputField(UI)", message).As<InputField>().SetText(text);
            cat.SetPlaceholderText(placeholderText);
            return cat as InputField;
        }
        public static InputField InputField(string text, string message)
        {
            var cat = GUIField("InputField(UI)", message).As<InputField>().SetText(text);
            return cat as InputField;
        }

        public static IBoolButton BoolButton(string label, bool isModernUI, bool initBool, string message, UnityAction<bool> action)
        {
            return isModernUI ? ModernUISwitch(label, initBool, message, action) : Toggle(label, initBool, message, action);
        }
        public static ModernUISwitch ModernUISwitch(string label, bool initBool, string message, UnityAction<bool> action)
        {
            var cat = GUIField(label, "Switch(ModernUI)", message) as ModernUISwitch;
            cat.isOn = initBool;
            cat.AddListener(action);
            return cat;
        }
        public static Toggle Toggle(string label, bool initBool, string message, UnityAction<bool> action)
        {
            var cat = GUIField("Toggle(UI)", message) as Toggle;
            cat.isOn = initBool;
            cat.AddListener(action);
            cat.SetTitle(label);
            return cat;
        }

        public static Dropdown Dropdown(string label, string[] options, string initSelect, string message, UnityAction<string> action)
        {
            Dropdown cat = GUIField(label, "Dropdown(UI)", message).As<Dropdown>();
            cat.ClearOptions();
            cat.AddOption(options);
            cat.AddListener(action);
            cat.Select(initSelect);
            return cat;
        }
        public static Dropdown Dropdown(string[] options, string initSelect, string message, UnityAction<string> action)
        {
            Dropdown cat = GUIField("Dropdown(UI)", message).As<Dropdown>();
            cat.ClearOptions();
            cat.AddOption(options);
            cat.AddListener(action);
            cat.Select(initSelect);
            return cat;
        }
        public static ModernUIDropdown ModernUIDropdown(string label, string[] options, string[] initSelects, string message, UnityAction<string> action)
        {
            ModernUIDropdown cat = GUIField(label, "Dropdown(ModernUI)", message).As<ModernUIDropdown>();
            cat.ClearOptions();
            cat.AddOption(options);
            cat.AddListener(action);
            cat.maxSelect = initSelects.Length;
            foreach (var initSelect in initSelects)
            {
                cat.Select(initSelect);
            }
            return cat;
        }
        public static ModernUIDropdown ModernUIDropdown(string[] options, string[] initSelects, string message, UnityAction<string> action)
        {
            ModernUIDropdown cat = GUIField("Dropdown(ModernUI)", message).As<ModernUIDropdown>();
            cat.ClearOptions();
            cat.AddOption(options);
            cat.AddListener(action);
            cat.maxSelect = initSelects.Length;
            foreach (var initSelect in initSelects)
            {
                cat.Select(initSelect);
            }
            return cat;
        }

        public static RawImage RawImage(Texture texture, string message)
        {
            var cat = GUIField("RawImage(UI)", message) as RawImage;
            cat.source.texture = texture;
            return cat;
        }

        public static Slider Slider(string label, float min, float max, float initValue, bool IsNormalized, string message, UnityAction<float> action)
        {
            BeginHorizontal();
            Label(label, message);
            var cat = GUIField("Slider(UI)", message) as Slider;
            cat.source.minValue = min;
            cat.source.maxValue = max;
            if (IsNormalized) cat.source.value = initValue;
            else cat.source.normalizedValue = initValue;
            cat.source.onValueChanged.AddListener(action);
            EndHorizontal();
            return cat;
        }
        public static Slider Slider(float min, float max, float initValue, bool IsNormalized, string message, UnityAction<float> action)
        {
            var cat = GUIField("Slider(UI)", message) as Slider;
            cat.source.minValue = min;
            cat.source.maxValue = max;
            if (IsNormalized) cat.source.value = initValue;
            else cat.source.normalizedValue = initValue;
            cat.source.onValueChanged.AddListener(action);
            return cat;
        }
        public static ModernUIFillBar ModernUIFillBar(string label, float min, float max, float initValue, string message, UnityAction<float> action)
        {
            BeginHorizontal();
            Label(label, message);
            var cat = GUIField("FillBar(ModernUI)", message) as ModernUIFillBar;
            cat.Set(initValue, min, max);
            cat.OnValueChange.AddListener(action);
            EndHorizontal();
            return cat;
        }
        public static ModernUIFillBar ModernUIFillBar(float min, float max, float initValue, string message, UnityAction<float> action)
        {
            var cat = GUIField("FillBar(ModernUI)", message) as ModernUIFillBar;
            cat.Set(initValue, min, max);
            cat.OnValueChange.AddListener(action);
            return cat;
        }

        public static ColorManager ColorPanel(string label, Color initColor, string message, UnityAction<Color> action)
        {
            EndHorizontal();
            Label(label, message);
            var cat = GUIField("ColorPanel(UI)", message) as ColorManager;
            cat.ColorValue = initColor;
            cat.ColorProperty.AddListenerOnSet(action);
            return cat;
        }
        public static ColorManager ColorPanel(Color initColor, string message, UnityAction<Color> action)
        {
            var cat = GUIField("ColorPanel(UI)", message) as ColorManager;
            cat.ColorValue = initColor;
            cat.ColorProperty.AddListenerOnSet(action);
            return cat;
        }

        public static ViewController Image(string label, string message)
        {
            BeginHorizontal();
            Label(label, message);
            var cat = GUIField("Image(UI)", message) as ViewController;
            EndHorizontal();
            return cat;
        }
        public static ViewController Image(string message)
        {
            var cat = GUIField("Image(UI)", message) as ViewController;
            return cat;
        }

        public static void BeginHorizontal()
        {
            if (IsNeedMulLine)
            {
                IsNeedMulLine = false;
                if (GUILayoutLineList.Count == 0 || GUILayoutLineList[^1].Count > 0)
                    GUILayoutLineList.Add(new());
            }
            else
            {
                IsNeedMulLine = false;
                if (GUILayoutLineList.Count == 0)
                    GUILayoutLineList.Add(new());
            }
        }
        public static void EndHorizontal()
        {
            IsNeedMulLine = true;
        }

        #endregion

        //Extension by 12.12

        public static Vector2UI Vector2(string label, Vector2 initVec, string message, UnityAction<Vector2> action)
        {
            var cat = GUIField(label, "Vector2(UI)", message, false).As<VectorBaseUI>().InitValue(initVec.x, initVec.y) as Vector2UI;
            cat.action = action;
            return cat;
        }
        public static Vector3UI Vector3(string label, Vector3 initVec, string message, UnityAction<Vector3> action)
        {
            var cat = GUIField(label, "Vector3(UI)", message, false).As<VectorBaseUI>().InitValue(initVec.x, initVec.y, initVec.z) as Vector3UI;
            cat.action = action;
            return cat;
        }
        public static Vector4UI Vector4(string label, Vector4 initVec, string message, UnityAction<Vector4> action)
        {
            var cat = GUIField(label, "Vector3(UI)", message, false).As<VectorBaseUI>().InitValue(initVec.x, initVec.y, initVec.z, initVec.w) as Vector4UI;
            cat.action = action;
            return cat;
        }

        public static Vector2UI Vector2(Vector2 initVec, string message, UnityAction<Vector2> action)
        {
            var cat = GUIField("Vector2(UI)", message).As<VectorBaseUI>().InitValue(initVec.x, initVec.y) as Vector2UI;
            cat.action = action;
            return cat;
        }
        public static Vector3UI Vector3(Vector3 initVec, string message, UnityAction<Vector3> action)
        {
            var cat = GUIField("Vector3(UI)", message).As<VectorBaseUI>().InitValue(initVec.x, initVec.y, initVec.z) as Vector3UI;
            cat.action = action;
            return cat;
        }
        public static Vector4UI Vector4(Vector4 initVec, string message, UnityAction<Vector4> action)
        {
            var cat = GUIField("Vector3(UI)", message).As<VectorBaseUI>().InitValue(initVec.x, initVec.y, initVec.z, initVec.w) as Vector4UI;
            cat.action = action;
            return cat;
        }

        public static void Transform(Transform transform)
        {
            EndHorizontal();
            Title("Local", "Local Value");
            var localPosition = Vector3("LocalPosition", transform.localPosition, "LocalPosition", T => transform.localPosition = T);
            localPosition.xyz = () => transform.localPosition;
            var localEulerAngles = Vector3("LocalEulerAngles", transform.localEulerAngles, "LocalEulerAngles", T => transform.localEulerAngles = T);
            localEulerAngles.xyz = () => transform.localEulerAngles;
            var localScale = Vector3("LocalScale", transform.localEulerAngles, "LocalScale", T => transform.localScale = T);
            localScale.xyz = () => transform.localScale;

            Title("World", "World Value");
            var Position = Vector3("Position", transform.position, "Position", T => transform.position = T);
            Position.xyz = () => transform.position;
            var EulerAngles = Vector3("EulerAngles", transform.eulerAngles, "EulerAngles", T => transform.eulerAngles = T);
            EulerAngles.xyz = () => transform.eulerAngles;
        }

        public static Dropdown Enum<T>(string label, int initEnumValue, string message, UnityAction<string> action)
        {
            Type TargetEnum = typeof(T);
            if (!TargetEnum.IsEnum) throw new ADException("No Enum");
            var ops = TargetEnum.GetEnumNames();
            if (!TargetEnum.IsEnumDefined(initEnumValue)) throw new ADException("Not Defined On This Enum");
            return Dropdown(label, ops, TargetEnum.GetEnumName(initEnumValue), message, action);
        }

        public static ModernUIDropdown EnumByModern<T>(string label, int[] initEnumValue, string message, UnityAction<string> action)
        {
            Type TargetEnum = typeof(T);
            if (!TargetEnum.IsEnum) throw new ADException("No Enum");
            var ops = TargetEnum.GetEnumNames();
            foreach (var single in initEnumValue)
            {
                if (!TargetEnum.IsEnumDefined(single)) throw new ADException("Not Defined On This Enum");
            }
            List<string> iniops = new();
            foreach (var op in initEnumValue)
            {
                iniops.Add(TargetEnum.GetEnumName(op));
            }
            return ModernUIDropdown(label, ops, iniops.ToArray(), message, action);
        }

        public static ModernUIInputField ModernUIInputField(string text, string message)
        {
            var cat = GUIField("InputField(ModernUI)", message).As<ModernUIInputField>().SetText(text);
            return cat as ModernUIInputField;
        }

        //ListView

        public static ListView ListView(string message, ListViewItem item)
        {
            var cat = GUIField("ListView(UI)", message).As<ListView>();
            cat.SetPrefab(item);
            return cat;
        }

        //Tie Value

        public static InputField FloatField(string label, float initValue, string message, UnityAction<float> action)
        {
            EndHorizontal();
            BeginHorizontal();
            Label(label, message);
            var input = InputField(initValue.ToString(), label, message);
            input.AddListener(T =>
            {
                if (ArithmeticExtension.TryParse(T, out var value))
                    action.Invoke(value.ReadValue());
                else
                {
                    input.SetTextWithoutNotify("0");
                    action.Invoke(0);
                }
            });
            EndHorizontal();
            return input;
        }

        public static InputField IntField(string label, int initValue, string message, UnityAction<int> action)
        {
            Label(label, message);
            var input = InputField(initValue.ToString(), label, message);
            input.AddListener(T =>
            {
                if (ArithmeticExtension.TryParse(T, out var value))
                    action.Invoke((int)value.ReadValue());
                else
                {
                    input.SetTextWithoutNotify("0");
                    action.Invoke(0);
                }
            });
            return input;
        }
    }

    public static class PropertiesExLayout
    {
        private static Stack<PropertiesItem> items = new();
        private static HashSet<object> objects = new();

        public static Dictionary<object, bool> isFolderObject = new();

        public static List<IADUI> Generate(object source)
        {
            return Generate(source, ADType.GetOrCreateADType(source.GetType()));
        }

        private static List<IADUI> DoGenerate(List<IADUI> result, object source, ADType sourceType, string keyLabel = null)
        {
            if (sourceType.members == null) sourceType.GetMembers(true);
            if (sourceType.IsCollection)
            {
                DoSubMemberCollectionType(result, source, keyLabel);
            }
            else if (sourceType.IsReflectedType)
            {
                foreach (var member in sourceType.members)
                {
                    DoSubMember(result, member, source);
                }
            }
            else Debug.LogWarning("PropertiesExLayout Not Support Type : " + sourceType.type.Name);

            return result;
        }

        public static List<IADUI> Generate(object source, ADType sourceType)
        {
            return DoGenerate(new(), source, sourceType);
        }


        private static List<IADUI> DoSubMember(List<IADUI> result, ADMember member, object that)
        {
            items.Push(PropertiesLayout.CurrentEditorMatchItem);
            Type type = member.type;
            if (type == typeof(GameObject) || type.IsAssignableFromOrSubClass(typeof(MonoBehaviour)))
            {

            }
            else if (type == typeof(bool))
            {
                IADUI cat = PropertiesLayout.ModernUISwitch(member.name, (bool)member.reflectedMember.GetValue(that), member.name, T => member.reflectedMember.SetValue(that, T));
                result.Add(cat);
            }
            else if (type == typeof(char))
            {
                PropertiesLayout.BeginHorizontal();
                PropertiesLayout.Label(member.name, member.name);
                var cat = PropertiesLayout.InputField(((char)member.reflectedMember.GetValue(that)).ToString(), member.name);
                result.Add(cat);
                cat.AddListener(T =>
                {
                    member.reflectedMember.SetValue(that, T[0]);
                    cat.SetTextWithoutNotify(T[..1]);
                });
                PropertiesLayout.EndHorizontal();
            }
            else if (type == typeof(double))
            {
                PropertiesLayout.BeginHorizontal();
                PropertiesLayout.Label(member.name, member.name);
                var cat = PropertiesLayout.InputField(((double)member.reflectedMember.GetValue(that)).ToString(), member.name);
                result.Add(cat);
                cat.AddListener(T =>
                {
                    if (double.TryParse(T, out double value))
                    {
                        member.reflectedMember.SetValue(that, value);
                    }
                    else
                    {
                        cat.SetTextWithoutNotify(((double)member.reflectedMember.GetValue(that)).ToString());
                    }
                });
                PropertiesLayout.EndHorizontal();
            }
            else if (type == typeof(float))
            {
                PropertiesLayout.BeginHorizontal();
                PropertiesLayout.Label(member.name, member.name);
                var cat = PropertiesLayout.InputField(((float)member.reflectedMember.GetValue(that)).ToString(), member.name);
                result.Add(cat);
                cat.AddListener(T =>
                {
                    if (float.TryParse(T, out float value))
                    {
                        member.reflectedMember.SetValue(that, value);
                    }
                    else
                    {
                        cat.SetTextWithoutNotify(((float)member.reflectedMember.GetValue(that)).ToString());
                    }
                });
                PropertiesLayout.EndHorizontal();
            }
            else if (type == typeof(int))
            {
                PropertiesLayout.BeginHorizontal();
                PropertiesLayout.Label(member.name + "(integer)", member.name + "(integer)");
                var cat = PropertiesLayout.InputField(((int)member.reflectedMember.GetValue(that)).ToString(), member.name + "(integer)");
                result.Add(cat);
                cat.AddListener(T =>
                {
                    if (int.TryParse(T, out int value))
                    {
                        member.reflectedMember.SetValue(that, value);
                    }
                    else
                    {
                        cat.SetTextWithoutNotify(((int)member.reflectedMember.GetValue(that)).ToString());
                    }
                });
                PropertiesLayout.EndHorizontal();
            }
            else if (type == typeof(uint))
            {
                PropertiesLayout.BeginHorizontal();
                PropertiesLayout.Label(member.name + "(unsigned integer)", member.name + "(unsigned integer)");
                var cat = PropertiesLayout.InputField(((uint)member.reflectedMember.GetValue(that)).ToString(), member.name + "(unsigned integer)");
                result.Add(cat);
                cat.AddListener(T =>
                {
                    if (uint.TryParse(T, out uint value))
                    {
                        member.reflectedMember.SetValue(that, value);
                    }
                    else
                    {
                        cat.SetTextWithoutNotify(((uint)member.reflectedMember.GetValue(that)).ToString());
                    }
                });
                PropertiesLayout.EndHorizontal();
            }
            else if (type == typeof(long))
            {
                PropertiesLayout.BeginHorizontal();
                PropertiesLayout.Label(member.name + "(long)", member.name + "(long)");
                var cat = PropertiesLayout.InputField(((long)member.reflectedMember.GetValue(that)).ToString(), member.name + "(long)");
                result.Add(cat);
                cat.AddListener(T =>
                {
                    if (long.TryParse(T, out long value))
                    {
                        member.reflectedMember.SetValue(that, value);
                    }
                    else
                    {
                        cat.SetTextWithoutNotify(((long)member.reflectedMember.GetValue(that)).ToString());
                    }
                });
                PropertiesLayout.EndHorizontal();
            }
            else if (type == typeof(ulong))
            {
                PropertiesLayout.BeginHorizontal();
                PropertiesLayout.Label(member.name + "(unsigned long)", member.name + "(unsigned long)");
                var cat = PropertiesLayout.InputField(((ulong)member.reflectedMember.GetValue(that)).ToString(), member.name + "(unsigned long)");
                result.Add(cat);
                cat.AddListener(T =>
                {
                    if (ulong.TryParse(T, out ulong value))
                    {
                        member.reflectedMember.SetValue(that, value);
                    }
                    else
                    {
                        cat.SetTextWithoutNotify(((ulong)member.reflectedMember.GetValue(that)).ToString());
                    }
                });
                PropertiesLayout.EndHorizontal();
            }
            else if (type == typeof(short))
            {
                PropertiesLayout.BeginHorizontal();
                PropertiesLayout.Label(member.name + "(short)", member.name + "(short)");
                var cat = PropertiesLayout.InputField(((short)member.reflectedMember.GetValue(that)).ToString(), member.name + "(short)");
                result.Add(cat);
                cat.AddListener(T =>
                {
                    if (short.TryParse(T, out short value))
                    {
                        member.reflectedMember.SetValue(that, value);
                    }
                    else
                    {
                        cat.SetTextWithoutNotify(((short)member.reflectedMember.GetValue(that)).ToString());
                    }
                });
                PropertiesLayout.EndHorizontal();
            }
            else if (type == typeof(ushort))
            {
                PropertiesLayout.BeginHorizontal();
                PropertiesLayout.Label(member.name + "(unsigh short)", member.name + "(unsigh short)");
                var cat = PropertiesLayout.InputField(((ushort)member.reflectedMember.GetValue(that)).ToString(), member.name + "(unsigh short)");
                result.Add(cat);
                cat.AddListener(T =>
                {
                    if (ushort.TryParse(T, out ushort value))
                    {
                        member.reflectedMember.SetValue(that, value);
                    }
                    else
                    {
                        cat.SetTextWithoutNotify(((ushort)member.reflectedMember.GetValue(that)).ToString());
                    }
                });
                PropertiesLayout.EndHorizontal();
            }
            else if (type == typeof(string))
            {
                PropertiesLayout.BeginHorizontal();
                PropertiesLayout.Label(member.name, member.name);
                var cat = PropertiesLayout.InputField((string)member.reflectedMember.GetValue(that), member.name);
                result.Add(cat);
                cat.AddListener(T =>
                {
                    member.reflectedMember.SetValue(that, T);
                });
                PropertiesLayout.EndHorizontal();
            }
            else if (type == typeof(Vector2))
            {
                var value = (Vector2)member.reflectedMember.GetValue(that);
                result.Add(PropertiesLayout.Vector2(member.name, value, member.name, T => member.reflectedMember.SetValue(that, T)));
            }
            else if (type == typeof(Vector3))
            {
                var value = (Vector3)member.reflectedMember.GetValue(that);
                result.Add(PropertiesLayout.Vector3(member.name, value, member.name, T => member.reflectedMember.SetValue(that, T)));
            }
            else if (type == typeof(Vector4))
            {
                var value = (Vector4)member.reflectedMember.GetValue(that);
                result.Add(PropertiesLayout.Vector4(member.name, value, member.name, T => member.reflectedMember.SetValue(that, T)));
            }
            else if (type == typeof(Color))
            {
                result.Add(PropertiesLayout.ColorPanel(member.name, (Color)member.reflectedMember.GetValue(that), member.name, T => { member.reflectedMember.SetValue(that, T); }));
            }
            else if (type == typeof(Texture2D))
            {
                PropertiesLayout.Label(member.name, member.name);
                result.Add(PropertiesLayout.RawImage((Texture2D)member.reflectedMember.GetValue(that), member.name));
            }
            else if (type == typeof(Sprite))
            {
                PropertiesLayout.Image(member.name, member.name).Share(out var cat).CurrentImagePair = new() { SpriteSource = (Sprite)member.reflectedMember.GetValue(that) };
                result.Add(cat);
            }
            else if (type == typeof(Transform))
            {
                PropertiesLayout.Label(member.name, member.name);
                PropertiesLayout.BeginHorizontal();
                PropertiesLayout.Transform((Transform)member.reflectedMember.GetValue(that));
                PropertiesLayout.EndHorizontal();
            }
            else
            {
                object item = member.reflectedMember.GetValue(that);
                if (objects.Add(item))
                {
                    object obj = item;
                    var folder = BuildListViewFolder(member.name);
                    if (obj != null && isFolderObject.TryGetValue(obj, out bool isFolder))
                    {
                        folder.target = obj;
                        folder.FolderButton.IsClick = !isFolder;
                    }
                    DoGenerate(result, obj, ADType.GetOrCreateADType(member.reflectedMember.MemberType), member.name);
                }
            }
            items.Pop();
            if (items.Count > 0) PropertiesLayout.CurrentEditorMatchItem = items.Peek();
            else objects.Clear();
            return result;
        }

        private static List<IADUI> DoSubMemberCollectionType(List<IADUI> result, object that, string keyLabel)
        {
            items.Push(PropertiesLayout.CurrentEditorMatchItem);
            var folder = BuildListViewFolder(keyLabel ?? "[ Unknown List ]");
            if (that != null && isFolderObject.TryGetValue(that, out bool isFolder))
            {
                folder.target = that;
                folder.FolderButton.IsClick = !isFolder;
            }
            if (that != null)
                foreach (var item in (IEnumerable)that)
                {
                    DoGenerate(result, item, ADType.GetOrCreateADType(item.GetType()));
                }
            items.Pop();
            if (items.Count > 0) PropertiesLayout.CurrentEditorMatchItem = items.Peek();
            else objects.Clear();
            return result;
        }

        private static PropertiesItemSubListEx BuildListViewFolder(string keyLabel)
        {
            PropertiesLayout.EndHorizontal();
            //PropertiesLayout.Label(member.name, member.name);
            PropertiesItem nextItem = Resources.Load<GameObject>("GameEditor/ListViewItem(Sub)").SeekComponent<PropertiesItem>();
            PropertiesLayout.ListView(keyLabel, nextItem).Share(out var cat).SetTitle(keyLabel);
            PropertiesLayout.CurrentEditorMatchItem = cat.GenerateItem().As<PropertiesItem>().Share(out var curItem);
            curItem.SetTitle(keyLabel);
            return nextItem.SeekComponent<PropertiesItemSubListEx>();
        }
    }
}
