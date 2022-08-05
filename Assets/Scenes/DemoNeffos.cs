using System;
using NeffosCSharp;
using NeffosCSharp.ConnectionHandles;
using UnityEngine;
using UnityEngine.UI;

public class DemoNeffos : MonoBehaviour
{
    private const string URL = "wss://gameserver.staging.thetanarena.com/ws/chat";

    public string KEY =
        "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJKV1RfQVBJUyIsImNhbl9taW50IjpmYWxzZSwiZXhwIjoxNjU5OTU2ODkyLCJpc3MiOiJodHRwczovL2FwaS5tYXJrZXRwbGFjZS5hcHAiLCJuYmYiOjE2NTkzNTIwOTIsInJvbGUiOjAsInNpZCI6IjB4ZjUxMWYzMTJiMTNmMjVhOGY0M2NjNjQ4ZTU2ZDBlYjg2MDMxZmViYyIsInN1YiI6InBUTUwydE01QmNyNiIsInVzZXJfaWQiOiI2MmU3ODkyMWE3MjBiNjYyYmRkYWM0MTYifQ.-WSoFcK8RIsVomT9-r1me2YCl0vKD23R34ivNvbCsTk";

    public string @namespace = "Game";
    async void MyTest()
    {
        var neffosClient = new NeffosClient();
        neffosClient.Key = KEY;
        var chatServiceHandler = new ChatServiceHandler();

        var c = await neffosClient.DialAsync(URL,
            new ConnectionHandlerBase[]
            {
                chatServiceHandler
            },
            new Options(), s => { Debug.Log("rejected reason: " + s); });
        var nsConnection = await c.Connect(@namespace);
        Debug.Log("connected to namespace: " + @namespace);
        await nsConnection.JoinRoom("Party-499");
        nsConnection.EmitBinary("Say", "Hello World");
    }

    private void Start()
    {
        //MyTest();
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Test Neffos"))
        {
            MyTest();
        }
    }
    
}