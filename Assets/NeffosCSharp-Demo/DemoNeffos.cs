using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
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
    private Connection _client2Connection;

    private NSConnection _nsClient1Connection;
    private NSConnection _nsClient2Connection;

    private Room _client1Room;
    private Room _client2Room;

    async void DemoConnection()
    {
        var client1 = new NeffosClient();
        client1.Key = keyClient1;
        var chatServiceHandler = new MainNamespaceHandler();

        _client1Connection = await client1.DialAsync(URL,
            new IConnectionHandler[]
            {
                chatServiceHandler
            },
            new Options(), s => { Debug.Log("rejected reason: " + s); });

        _nsClient1Connection = await _client1Connection.Connect(@namespace);

        _client1Room = await _nsClient1Connection.JoinRoom("Party-499");

        _nsClient1Connection.Events[Configuration.OnAnyEvent] += (nsConnection, message) =>
        {
            Debug.Log("Player 1 Receive: " + message.Body.ToUTF8String());
            return message.Error;
        };

        var client2 = new NeffosClient();
        client2.Key = keyClient2;
        var chatServiceHandler2 = new MainNamespaceHandler();
        _client2Connection = await client2.DialAsync(URL,
            new IConnectionHandler[]
            {
                chatServiceHandler2
            },
            new Options(), s => { Debug.Log("rejected reason: " + s); });

        _nsClient2Connection = await _client2Connection.Connect(@namespace);
        _client2Room = await _nsClient2Connection.JoinRoom("Party-499");
        _nsClient2Connection.Events[Configuration.OnAnyEvent] += (nsConnection, message) =>
        {
            Debug.Log("Player 2 Receive: " + message.Body.ToUTF8String() + " from ");
            return message.Error;
        };
    }

    private void Start()
    {
        DemoConnection();
    }

    private void OnDestroy()
    {
        _client1Connection?.Close();
        _client2Connection?.Close();
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

        if (GUILayout.Button("Client 2 Send Message"))
        {
            string random = UnityEngine.Random.Range(100, 1000000).ToString();
            _client2Room.Emit("TestEvent", random);
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
    }
}