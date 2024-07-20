using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Diagram
{
    public class LineScriptCore 
    {
        public void Run(LineScriptAssets ls)
        {
            Run(ls.text);
        }
        public void Run(string ls)
        {
            CoreRun(ls.Split('\n'));
        }
        private void CoreRun(string[] ls)
        {

        }
    }

    [ScriptedImporter(2, ".ls")]
    public class LineScriptImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var lineTxt = File.ReadAllText(ctx.assetPath);

            Debug.Log("Import:" + ctx.assetPath);
            //转化为TextAsset，也可写个LuaAsset的类作为保存对象，但要继承Object的类
            var assetsText = new LineScriptAssets(lineTxt);

            ctx.AddObjectToAsset("main obj", assetsText, Resources.Load<Texture2D>("Editor/Icon/LineScript"));
            ctx.SetMainObject(assetsText);
        }
    }

    public class LineScriptAssets : TextAsset
    {
        public LineScriptAssets() : base("") { }
        public LineScriptAssets(string lines) : base(lines) { }


    }
}
