using System.Collections;
using System.Collections.Generic;
using Diagram;
using UnityEngine;

public class GameObjectLoader : ABObject
{
    public GameObject MyObject;

    public GameObjectLoader(string package, string name) : base(package, name)
    {
        MyObject = (target as GameObject).PrefabInstantiate();
    }

    public Component ObtainComponent(string compName)
    {
        var result = MyObject.GetComponent(compName);
        if (result != null)
            result = MyObject.AddComponent(compName.ToType());
        return result;
    }
}
