using System;
using System.Collections.Generic;
using System.Text;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace ResoniteLink
{
    public class LinkInterface
    {
        public bool IsConnected => _client.State == WebSocketState.Open;

        ClientWebSocket _client;

        ConcurrentDictionary<string, TaskCompletionSource<Response>> _pendingResponses = new ConcurrentDictionary<string, TaskCompletionSource<Response>>();

        public LinkInterface()
        {
            _client = new ClientWebSocket();
        }

        public Task Connect(Uri target, System.Threading.CancellationToken cancellationToken)
        {
            if(IsConnected)
                throw new InvalidOperationException("Client is already connected.");

            return _client.ConnectAsync(target, cancellationToken);
        }

        async Task<O> SendMessage<I,O>(I message)
            where I : Message
            where O : Response
        {
            EnsureMessageID(message);

            var responseCompletion = new TaskCompletionSource<Response>();

            if (!_pendingResponses.TryAdd(message.MessageID, responseCompletion))
                throw new InvalidOperationException("Failed to register MessageID. Did you provide duplicate MessageID?");

            var jsonData = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message);

            await _client.SendAsync(new ArraySegment<byte>(jsonData), 
                WebSocketMessageType.Text, false, System.Threading.CancellationToken.None);

            // Wait for response to arrive and cast it to the target type if compatible
            return await responseCompletion.Task as O;
        }

        void EnsureMessageID(Message message)
        {
            if (message.MessageID == null)
                message.MessageID = Guid.NewGuid().ToString();
        }

        #region API

        public Task<SlotData> GetSlotData(GetSlot request) => SendMessage<GetSlot, SlotData>(request);
        public Task<ComponentData> GetComponentData(GetComponent request) => SendMessage<GetComponent, ComponentData>(request);

        public Task<Response> AddSlot(AddSlot request) => SendMessage<AddSlot, Response>(request);
        public Task<Response> UpdateSlot(UpdateSlot request) => SendMessage<UpdateSlot, Response>(request);
        public Task<Response> RemoveSlot(RemoveSlot request) => SendMessage<RemoveSlot, Response>(request);

        public Task<Response> AddComponent(AddComponent request) => SendMessage<AddComponent, Response>(request);
        public Task<Response> UpdateComponent(UpdateComponent request) => SendMessage<UpdateComponent, Response>(request);
        public Task<Response> RemoveComponent(RemoveComponent request) => SendMessage<RemoveComponent, Response>(request);

        #endregion
    }
}
