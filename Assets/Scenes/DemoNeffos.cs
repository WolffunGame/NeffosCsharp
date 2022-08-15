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
    
    private Connection _connection;
    private NSConnection _nsConnection;
    async void MyTest()
    {
        var neffosClient = new NeffosClient();
        neffosClient.Key = KEY;
        var chatServiceHandler = new ChatServiceHandler();

        _connection = await neffosClient.DialAsync(URL,
            new ConnectionHandlerBase[]
            {
                chatServiceHandler
            },
            new Options(), s => { Debug.Log("rejected reason: " + s); });
        
        _nsConnection = await _connection.Connect(@namespace);

        var room = await _nsConnection.JoinRoom("Party-499");
        _nsConnection.EmitBinary("Say", "Hello World");
    }

    private void Start()
    {
        MyTest();
    }

    private void OnDestroy()
    {
        _connection?.Close();
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Test Neffos"))
        {
            MyTest();
        }
        
        if (GUILayout.Button("Send Message"))
        {
            string random = UnityEngine.Random.Range(100, 1000000).ToString();
            _nsConnection.EmitBinary("Say", random);
        }
        
        if (GUILayout.Button("Login"))
        {
            _nsConnection.EmitBinary("Login", string.Empty);
        }
    }
    
}