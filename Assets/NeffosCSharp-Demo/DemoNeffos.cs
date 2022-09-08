using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using NeffosCSharp;
using NeffosCSharp.ConnectionHandles;
using UnityEngine;

public class DemoNeffos : MonoBehaviour
{
    public string URL = "ws://localhost:8080/ws/chat";

    public string keyClient1 = "Bearer";

    public string keyClient2 = "Bearer";

    public string @namespace = "Test";

    private Connection _client1Connection;

    private NSConnection _nsClient1Connection;

    private Room _client1Room;

    private NeffosClient client1;



    async void DemoConnectionA()
    {
        var chatServiceHandler = new MainNamespaceHandler();
        var option = new Options(3, 10f);
        client1 = new NeffosClient(URL, option, chatServiceHandler);
        client1.Key = keyClient1;
        client1.State.Subscribe(AwaitForReconnect);

        _client1Connection = await client1.DialAsync(Debug.LogError);
        
        _nsClient1Connection = await _client1Connection.Connect(@namespace);

        _client1Room = await _nsClient1Connection.JoinRoom("Party-499");

        _nsClient1Connection.Events[Configuration.OnAnyEvent] += (nsConnection, message) =>
        {
            Debug.Log("Player 1 Receive: " + message.Body.ToUTF8String());
            return message.Error;
        };
        

    }
    
    void AwaitForReconnect(NeffosClientState state)
    {
        switch (state)
        {
            case NeffosClientState.Offline:
                Debug.Log("Offline");
                break;
            case NeffosClientState.Reconnecting:
                Debug.Log("Reconnecting");
                break;
            case NeffosClientState.Connected:
                Debug.Log("Connected");
                break;
            case NeffosClientState.Connecting:
                Debug.Log("Connecting");
                break;
            default:
                Debug.Log("Unknown");
                break;
        }
        
    }

    private void Start()
    {
        DemoConnectionA();
    }

    private void OnDestroy()
    {
        _client1Connection?.Close();
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Client 1 Send Message"))
        {
            string seed = "qwertyuiop[]asdfghjklzxcvbnm";
            var random = new System.Random();

            //random a string 300 characters long
            string message = new string(
                Enumerable.Repeat(seed, 300)
                    .Select(s => s[random.Next(s.Length)])
                    .ToArray());


            _client1Room.Emit("TestEvent", message);
        }
        

        if (GUILayout.Button("Login"))
        {
            _nsClient1Connection.Ask("Login", string.Empty).Forget();
        }

        if (GUILayout.Button("Client 1 Join Room 01"))
        {
            _nsClient1Connection.JoinRoom("Room-Vip-01").Forget();
        }

        if (GUILayout.Button("Client 1 Join Room 02"))
        {
            _nsClient1Connection.JoinRoom("Room-Vip-02").Forget();
        }

        if (GUILayout.Button("Client 1 Leave All Room"))
        {
            _nsClient1Connection.LeaveAll().Forget();
        }
        
        if(GUILayout.Button("Client 1 Leave Room Party-499"))
        {
            _client1Room.Leave().Forget();
        }
        
        if (GUILayout.Button("Create Party Room"))
        {
            _nsClient1Connection.Ask("CreatePartyRoom", string.Empty).Forget();
        }
    }
}