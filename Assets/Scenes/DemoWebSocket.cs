using System;
using BestHTTP;
using BestHTTP.Examples;
using BestHTTP.Examples.Helpers;
using UnityEngine;
using UnityEngine.UI;
using BestHTTP.WebSocket;

namespace Scenes
{
    public class DemoWebSocket : MonoBehaviour
    {
#pragma warning disable 0649

        [SerializeField] [Tooltip("The WebSocket address to connect")]
        private string address = "wss://gameserver.staging.thetanarena.com/ws/chat";

        [SerializeField] private string key =
            "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJKV1RfQVBJUyIsImNhbl9taW50IjpmYWxzZSwiZXhwIjoxNjU5OTU2ODkyLCJpc3MiOiJodHRwczovL2FwaS5tYXJrZXRwbGFjZS5hcHAiLCJuYmYiOjE2NTkzNTIwOTIsInJvbGUiOjAsInNpZCI6IjB4ZjUxMWYzMTJiMTNmMjVhOGY0M2NjNjQ4ZTU2ZDBlYjg2MDMxZmViYyIsInN1YiI6InBUTUwydE01QmNyNiIsInVzZXJfaWQiOiI2MmU3ODkyMWE3MjBiNjYyYmRkYWM0MTYifQ.-WSoFcK8RIsVomT9-r1me2YCl0vKD23R34ivNvbCsTk";

        [SerializeField] private InputField input;

        [SerializeField] private ScrollRect scrollRect;

        [SerializeField] private RectTransform contentRoot;

        [SerializeField] private TextListItem listItemPrefab;

        [SerializeField] private int maxListItemEntries = 100;

        [SerializeField] private Button connectButton;

        [SerializeField] private Button closeButton;

#pragma warning restore

        /// <summary>
        /// Saved WebSocket instance
        /// </summary>
        private WebSocket _webSocket;

        protected void Start()
        {
            SetButtons(true, false);
            this.input.interactable = false;
            
            this.connectButton.onClick.AddListener(OnConnectButton);
            input.onEndEdit.AddListener(OnInputField);
            this.closeButton.onClick.AddListener(OnCloseButton);
        }

        void OnDestroy()
        {
            if (this._webSocket != null)
            {
                this._webSocket.Close();
                this._webSocket = null;
            }
        }

        public void OnConnectButton()
        {
            // Create the WebSocket instance
            this._webSocket = new WebSocket(new Uri(address));

#if !UNITY_WEBGL || UNITY_EDITOR
            this._webSocket.StartPingThread = true;

#if !BESTHTTP_DISABLE_PROXY
            if (HTTPManager.Proxy != null)
                this._webSocket.OnInternalRequestCreated = (ws, internalRequest) =>
                    internalRequest.Proxy =
                        new HTTPProxy(HTTPManager.Proxy.Address, HTTPManager.Proxy.Credentials, false);
#endif
#endif

            // Subscribe to the WS events
            this._webSocket.OnOpen += OnOpen;
            this._webSocket.OnMessage += OnMessageReceived;
            this._webSocket.OnClosed += OnClosed;
            this._webSocket.OnError += OnError;
            this._webSocket.OnBinary += OnBinary;

            this._webSocket.OnInternalRequestCreated += OnInternalRequestCreated;

            // Start connecting to the server
            this._webSocket.Open();

            AddText("Connecting...");

            SetButtons(false, true);
            this.input.interactable = false;
        }

        private void OnBinary(WebSocket websocket, byte[] data)
        {
            Debug.Log("OnBinary: " + data.Length);
            AddText("OnBinary: " + data.Length);
        }

        private void OnInternalRequestCreated(WebSocket arg1, HTTPRequest arg2)
        {
            arg2.AddHeader("Authorization", key);
        }

        public void OnCloseButton()
        {
            AddText("Closing!");
            // Close the connection
            this._webSocket.Close(1000, "Bye!");

            SetButtons(false, false);
            this.input.interactable = false;
        }

        public void OnInputField(string textToSend)
        {
            if ((!Input.GetKeyDown(KeyCode.KeypadEnter) && !Input.GetKeyDown(KeyCode.Return)) ||
                string.IsNullOrEmpty(textToSend))
                return;

            AddText(string.Format("Sending message: <color=green>{0}</color>", textToSend))
                .AddLeftPadding(20);


            // Send message to the server
            this._webSocket.Send(System.Text.Encoding.UTF8.GetBytes(textToSend));
            Debug.Log(textToSend);
        }

        #region WebSocket Event Handlers

        /// <summary>
        /// Called when the web socket is open, and we are ready to send and receive data
        /// </summary>
        void OnOpen(WebSocket ws)
        {
            AddText("WebSocket Open!");
            Debug.Log("Websocket open!");

            this.input.interactable = true;
        }

        /// <summary>
        /// Called when we received a text message from the server
        /// </summary>
        void OnMessageReceived(WebSocket ws, string message)
        {
            AddText(string.Format("Message received: <color=yellow>{0}</color>", message))
                .AddLeftPadding(20);
            Debug.Log(message);
        }

        /// <summary>
        /// Called when the web socket closed
        /// </summary>
        void OnClosed(WebSocket ws, UInt16 code, string message)
        {
            AddText(string.Format("WebSocket closed! Code: {0} Message: {1}", code, message));

            _webSocket = null;

            SetButtons(true, false);
        }

        /// <summary>
        /// Called when an error occured on client side
        /// </summary>
        void OnError(WebSocket ws, string error)
        {
            AddText(string.Format("An error occured: <color=red>{0}</color>", error));

            _webSocket = null;

            SetButtons(true, false);
        }

        #endregion

        private void SetButtons(bool connect, bool close)
        {
            if (this.connectButton != null)
                this.connectButton.interactable = connect;

            if (this.closeButton != null)
                this.closeButton.interactable = close;
        }

        private TextListItem AddText(string text)
        {
            return GUIHelper.AddText(this.listItemPrefab, this.contentRoot, text, this.maxListItemEntries,
                this.scrollRect);
        }
    }
}