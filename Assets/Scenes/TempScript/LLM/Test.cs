using System.Collections;
using System.Collections.Generic;
using LLMUnity;
using UnityEngine;

public class Test : MonoBehaviour
{
    public LLMCharacter character;
    public AD.UI.Button Button;

    void HandleReply(string reply)
    {
        Debug.Log(reply);
    }

    void Start()
    {
        Button.AddListener(async () =>
        {
            await character.Chat("ÄãÊÇË­", HandleReply);
        });
    }
}

public class InfoJson
{
    public string prompt = "ÄãÊÇË­";
}