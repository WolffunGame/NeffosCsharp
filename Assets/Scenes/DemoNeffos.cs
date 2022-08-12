using System;
using NeffosCSharp;
using NeffosCSharp.ConnectionHandles;
using UnityEngine;
using UnityEngine.UI;

public class DemoNeffos : MonoBehaviour
{
    public string URL = "ws://localhost:8080/ws/chat";

    public string KEY =
        "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzaWQiOiIweGFmMzU4ZGNjMzRlOWFkZDBjOTBmYzlkNTM4YTI5ZjlkOThkMjg0NTQiLCJzdWIiOiJLQkMuVHJ1bmciLCJ1c2VyX2lkIjoiNjFjOWExNTdlODllODg1MGI4NGNmM2ZlIiwiY2FuX21pbnQiOiJGYWxzZSIsIm5iZiI6MTY1OTYwMzA4OCwiZXhwIjoxNjYwMjA3ODg4LCJpc3MiOiJodHRwczovL2FwaS5tYXJrZXRwbGFjZS5hcHAiLCJhdWQiOiJKV1RfQVBJUyJ9.5-9ccdKYfjhlqOUg34ad-m2MGKkiGfM_TbbbzVnGt0A";

    public string @namespace = "Game";
    async void MyTest()
    {
        Debug.Log("start my test");
        var neffosClient = new NeffosClient();
        neffosClient.Key = KEY;
        var chatServiceHandler = new ChatServiceHandler();

        var c = await neffosClient.DialAsync(URL,
            new ConnectionHandlerBase[]
            {
                chatServiceHandler
            },
            new Options(), s => { Debug.Log("rejected reason: " + s); });
        Debug.Log("prepare for connect");
        var nsConnection = await c.Connect(@namespace);
        Debug.Log("connected to namespace: " + @namespace);
        
        var room = await nsConnection.JoinRoom("Party-499");
        Debug.Log("joined room: " + room);
        nsConnection.EmitBinary("Say", "Hello World");
    }

    private void Start()
    {
        MyTest();
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Test Neffos"))
        {
            MyTest();
        }
    }
    
}