﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AD.Utility
{
    public static class StringExtension
    {
        public static bool Input<T>(this string self, out object result)
        {
            return ADGlobalSystem.Input<T>(self, out result);
        }

        public static bool Input(this string self, out string result)
        {
            return ADGlobalSystem.Input(self, out result);
        }

        public static void Output<T>(this string self, T source)
        {
            ADGlobalSystem.Output(self, source);
        }

        public static void Output(this string self, string str)
        {
            ADGlobalSystem.Output(self, str);
        }

        #region Translate

        public static bool Install(string path, bool isSetCurrent = true)
        {
            if (ADGlobalSystem.Input<Package>(path, out object obj))
            {
                if (isSetCurrent) CurrentIndex = Packages.Count;
                Packages.Add(obj as Package);
                return true;
            }
            else return false;
        }

        public static void Uninstall(string Name, ulong ID)
        {
            var cat = Packages.FirstOrDefault(T => T.Name == Name && T.ID == ID);
            Packages.Remove(cat);
        }

        public static string Get(this string self)
        {
            return CurrentPackage[self];
        }

        public static string Translate(this string self)
        {
            return CurrentPackage[self];
        }

        [Serializable]
        public class Package
        {
            public string Name;
            public string Description;
            public int index;
            public ulong ID;

            public Package()
            {
                Name = "Empty Package";
                Description = "";
                ID = (ulong)this.GetHashCode();
            }

            public Package(string name, string description, int index, ulong iD)
            {
                Name = name;
                Description = description;
                this.index = index;
                ID = iD;
            }

            public virtual Dictionary<string, string> values { get; set; } = new();

            public virtual string this[string key] => values.TryGetValue(key, out var value) ? value : key;

        }

        [Serializable]
        public class DefualtPackage : Package
        {
            public override string this[string key] => key;
        }

        internal static List<Package> Packages = new List<Package>();
        private static readonly DefualtPackage defualt = new DefualtPackage();
        public static int CurrentIndex = -1;
        public static int Count => Packages.Count;
        public static Package CurrentPackage
        {
            get
            {
                if (CurrentIndex < 0 || CurrentIndex >= Packages.Count)
                    return defualt;
                else return Packages[CurrentIndex];
            }
        }

        #endregion

        public static string Link(this string[] self, int first, int end)
        {
            string result = "";
            while (first < end - 1)
            {
                result += self[first++] + " ";
            }
            result += self[end - 1];
            return result;
        }

        public static string Link(this string[] self)
        {
            return self.Link(0, self.Length);
        }

        public static string LinkAndInsert(this string[] self, char key)
        {
            string result = "";
            if (self == null) return result;
            int first = 0, end = self.Length;
            while (first < end - 1)
            {
                result += self[first++] + key;
            }
            result += self[end - 1];
            return result;
        }

        public static string LinkAndInsert(this string[] self, string key)
        {
            string result = "";
            if (self == null) return result;
            int first = 0, end = self.Length;
            while (first < end - 1)
            {
                result += self[first++] + key;
            }
            result += self[end - 1];
            return result;
        }

        public static string LinkAndInsert(this List<string> self, char key)
        {
            string result = "";
            if (self == null) return result;
            int first = 0, end = self.Count;
            while (first < end - 1)
            {
                result += self[first++] + key;
            }
            result += self[end - 1];
            return result;
        }

        public static string LinkAndInsert(this List<string> self, string key)
        {
            string result = "";
            if (self == null) return result;
            int first = 0, end = self.Count;
            while (first < end - 1)
            {
                result += self[first++] + key;
            }
            result += self[end - 1];
            return result;
        }

        public static byte[] ToByteArray(this string self)
        {
            return self.ToByteArray(System.Text.Encoding.Default);
        }

        public static byte[] ToByteArrayUTF8(this string self)
        {
            return self.ToByteArray(System.Text.Encoding.UTF8);
        }

        public static byte[] ToByteArray(this string self, Encoding encoding)
        {
            return encoding.GetBytes(self);
        }

        public static string LoadFromMemory(byte[] bytes)
        {
            return LoadFromMemory(bytes, System.Text.Encoding.Default);
        }

        public static string LoadFromMemoryUTF8(byte[] bytes)
        {
            return LoadFromMemory(bytes, System.Text.Encoding.UTF8);
        }

        public static string LoadFromMemory(byte[] bytes, Encoding encoding)
        {
            return encoding.GetString(bytes);
        }
    }

    [Serializable]
    public class StringView : IEnumerable<char>
    {
        private readonly string OriginStr;

        public StringView(string source)
        {
            OriginStr = source;
            Left = 0;
            Right = source.Length;
        }

        public int Left;
        public int Right;

        public char this[int index]
        {
            get => OriginStr[index + Left];
        }

        public override string ToString()
        {
            return OriginStr[Left..Right];
        }

        public IEnumerator<char> GetEnumerator()
        {
            for (int i = Left, e = Right; i < e; i++)
            {
                yield return OriginStr[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static implicit operator string(StringView view) => view.ToString();
    }

    [Serializable]
    public class ShareStringView : StringView
    {
        public static Dictionary<int, string> SharedPool = new();
        public ShareStringView(int id = 0, int left = 0, int right = 0) : base(SharedPool[id])
        {
            Left = left;
            Right = right;
        }
    }
}

