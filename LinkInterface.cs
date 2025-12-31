using System;
using System.Collections.Generic;
using System.Text;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;

namespace ResoniteLink
{
    public class LinkInterface : IDisposable
    {
        const int BUFFER_SIZE = 1024 * 1024 * 32; // 32 MB

        public bool IsConnected => _client.State == WebSocketState.Open;
        public Exception FailureException { get; private set; }

        ClientWebSocket _client;
        CancellationTokenSource cancellation;

        ConcurrentDictionary<string, TaskCompletionSource<Response>> _pendingResponses = new ConcurrentDictionary<string, TaskCompletionSource<Response>>();

        public LinkInterface()
        {
        }

        public async Task Connect(Uri target, System.Threading.CancellationToken cancellationToken)
        {
            if(_client != null)
                throw new InvalidOperationException("Client has already been initialized.");

            _client = new ClientWebSocket();

            cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await _client.ConnectAsync(target, cancellation.Token);

            _ = Task.Run(async () => ReceiverHandler(cancellation.Token));
        }

        async Task ReceiverHandler(CancellationToken cancellationToken)
        {
            try
            {
                byte[] buffer = new byte[BUFFER_SIZE];

                while (!cancellationToken.IsCancellationRequested)
                {
                    var message = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    switch(message.MessageType)
                    {
                        case WebSocketMessageType.Text:
                            var response = System.Text.Json.JsonSerializer.Deserialize<Response>(
                                new MemoryStream(buffer, 0, message.Count));

                            if (_pendingResponses.TryRemove(response.SourceMessageID, out var completion))
                                completion.SetResult(response);
                            else
                                throw new Exception("There is no pending response with this ID");

                                break;

                        case WebSocketMessageType.Binary:
                            throw new NotSupportedException("Binary messages aren't currently supported");
                            break;

                        case WebSocketMessageType.Close:
                            cancellation.Cancel();
                            break;
                    }
                }
            }
            catch(Exception ex)
            {
                FailureException = ex;
            }

            if(_client.State == WebSocketState.Open)
                await _client.CloseAsync(FailureException == null ?
                    WebSocketCloseStatus.NormalClosure :
                    WebSocketCloseStatus.InternalServerError, 
                    FailureException == null ? "Closing" : "Internal Error", CancellationToken.None);

            _client.Dispose();
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

        public void Dispose()
        {
            cancellation.Cancel();
        }
    }
}
