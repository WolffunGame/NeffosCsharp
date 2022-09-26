using System;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using NeffosCSharp;
using NeffosCSharp.ConnectionHandles;
using Scenes;
using UnityEditor;
using UnityEngine;

public class DemoNeffos : MonoBehaviour
{
    public string URL = "ws://localhost:8080/ws/chat";

    public string keyClient1 = "Bearer";

    public string @namespace = "Test";

    public NeffosClient Client;
    const string MultipleAccountEvent = "DisconnectPrevDevice";
    public NSConnection GetNSConnection(string namespaceName)
    {
        return Client.Connection.GetNamespace(@namespace);
    }

    public Room GetRoom(string roomName)
    {
        var nsConn = Client.Connection.GetNamespace(@namespace);
        return nsConn.GetJoinedRoom(roomName);
    }

    async void DemoConnectionA()
    {
        var chatServiceHandler = new MyConnectionHandler();
        var option = new Options(3, 5f, false);
        Client = new NeffosClient(URL, option, chatServiceHandler);
        Client.Key = keyClient1;
        Client.State.Subscribe(AwaitForReconnect);

        await Client.DialAsync(Debug.LogError);

        var nsConn = await Client.Connection.Connect(@namespace);

        await nsConn.JoinRoom("Party-499");

        nsConn.Events[Configuration.OnAnyEvent] += (nsConnection, message) =>
        {
            Debug.Log("Player 1 Receive Event: "+ $"{message.Event}" + message.Body.ToUTF8String());
            return message.Error;
        };
        
        RegisterEvent(MultipleAccountEvent, (nsConnection, message) =>
        {
            Debug.Log("Player 1 Receive Event: "+ $"{message.Event}" + message.Body.ToUTF8String());
            return message.Error;
        });
    }

    void AwaitForReconnect(NeffosClientState state)
    {
        switch (state)
        {
            case NeffosClientState.Offline:
                Debug.LogWarning("Offline");
                break;
            case NeffosClientState.Reconnecting:
                Debug.LogWarning("Reconnecting");
                break;
            case NeffosClientState.Connected:
                Debug.LogWarning("Connected");
                break;
            case NeffosClientState.Connecting:
                Debug.LogWarning("Connecting");
                break;
            case NeffosClientState.FailedToReconnectPreviously:
                Debug.LogWarning("FailedToReconnectPreviously");
                break;
            case NeffosClientState.ReconnectButWasClosed:
                Debug.LogWarning("ReconnectButWasClosed");
                break;
            default:
                Debug.LogWarning("Unknown");
                break;
        }
    }

    // private void Start()
    // {
    //     DemoConnectionA();
    // }

    private void OnDestroy()
    {
        UnRegisterEvent(MultipleAccountEvent, (nsConnection, message) =>
        {
            Debug.Log("Player 1 Receive Event: "+ $"{message.Event}" + message.Body.ToUTF8String());
            return message.Error;
        });
        Client.Connection.Close();
    }

    private void OnGUI()
    {
        // if (GUILayout.Button("Client 1 Send Message"))
        // {
        //     string seed = "qwertyuiop[]asdfghjklzxcvbnm";
        //     var random = new System.Random();
        //
        //     //random a string 300 characters long
        //     string message = new string(
        //         Enumerable.Repeat(seed, 300)
        //             .Select(s => s[random.Next(s.Length)])
        //             .ToArray());
        //
        //
        //     _client1Room.Emit("TestEvent", message);
        // }
        //
        //
        // if (GUILayout.Button("Login"))
        // {
        //     _nsClient1Connection.Ask("Login", string.Empty).Forget();
        // }
        //
        // if (GUILayout.Button("Client 1 Join Room 01"))
        // {
        //     _nsClient1Connection.JoinRoom("Room-Vip-01").Forget();
        // }
        //
        // if (GUILayout.Button("Client 1 Join Room 02"))
        // {
        //     _nsClient1Connection.JoinRoom("Room-Vip-02").Forget();
        // }
        //
        // if (GUILayout.Button("Client 1 Leave All Room"))
        // {
        //     _nsClient1Connection.LeaveAll().Forget();
        // }
        //
        // if(GUILayout.Button("Client 1 Leave Room Party-499"))
        // {
        //     _client1Room.Leave().Forget();
        // }
        //
        // if (GUILayout.Button("Create Party Room"))
        // {
        //     _nsClient1Connection.Ask("CreatePartyRoom", string.Empty).Forget();
        // }

        if (GUILayout.Button("Dial"))
        {
            DemoConnectionA();
        }


        if (GUILayout.Button("Login"))
        {
            Client.Connection.GetNamespace(@namespace).Ask("Login", string.Empty).Forget();
        }

        if (GUILayout.Button("Log connected namespaces"))
        {
            foreach (var (key, value) in Client.Connection.ConnectedNamespaces)
            {
                foreach (var keyValuePair in value.Rooms)
                {
                    Debug.Log("Room: "+keyValuePair.Key);
                }

                Debug.Log($"namespace: {key}");
            }
        }

        if (GUILayout.Button("Reconnect"))
        {
            Client.Reconnect();
        }
    }
    
    public void RegisterEvent(string eventName, Func<NSConnection, Message, string> eventHandler)
    {
        var nsConn = GetNsConnection(@namespace);
        if (nsConn == null)
            return;

        if (!nsConn.Events.TryGetValue(eventName, out var handler))
        {
            nsConn.Events.Add(eventName, eventHandler);
        }
        else
        {
            nsConn.Events[eventName] += eventHandler;
        }
    }

    public void UnRegisterEvent(string eventName, Func<NSConnection, Message, string> eventHandler)
    {
        var nsConn = GetNsConnection(@namespace);
        if (nsConn == null)
            return;

        if (!nsConn.Events.TryGetValue(eventName, out var handler) || handler == null)
        {
            return;
        }

        nsConn.Events[eventName] -= eventHandler;
    }
    
    public  NSConnection GetNsConnection(string namespaceName)
    {
        if (Client?.Connection == null) return null;
        return Client.Connection.GetNamespace(namespaceName);
    }

    void Test()
    {
        //create a scriptable object and save it in Resources folder
        var scriptableObject = ScriptableObject.CreateInstance<ScriptableObject>();
        AssetDatabase.CreateAsset(scriptableObject, "Assets/Resources/ScriptableObject.asset");
        AssetDatabase.SaveAssets();

        //load the scriptable object
        var loadedScriptableObject = Resources.Load<ScriptableObject>("ScriptableObject");
    }
}