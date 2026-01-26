using Microsoft.AspNetCore.SignalR.Client;

namespace WebClient.Services
{
    public class WebClientConnection
    {
        private HubConnection? _connection;
        private readonly string _serverUrl;

        public HubConnection? Connection => _connection;
        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public WebClientConnection(string serverUrl)
        {
            _serverUrl = serverUrl;
        }

        public async Task ConnectAsync()
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                return;
            }

            _connection = new HubConnectionBuilder()
                .WithUrl($"{_serverUrl}/conferenceHub", options =>
                {
                    options.AccessTokenProvider = async () => await Task.FromResult(string.Empty);
                })
                .WithAutomaticReconnect()
                .Build();

            await _connection.StartAsync();
        }

        public async Task DisconnectAsync()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        public async Task JoinRoomAsync(string roomId, string userName)
        {
            if (_connection?.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("Connection is not established");
            }

            await _connection.InvokeAsync("JoinRoom", roomId, userName);
        }

        public async Task SendOfferAsync(string targetId, string sdp)
        {
            if (_connection?.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("Connection is not established");
            }

            await _connection.InvokeAsync("SendOffer", targetId, sdp);
        }

        public async Task SendAnswerAsync(string targetId, string sdp)
        {
            if (_connection?.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("Connection is not established");
            }

            await _connection.InvokeAsync("SendAnswer", targetId, sdp);
        }

        public async Task SendIceCandidateAsync(string targetId, object candidate)
        {
            if (_connection?.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("Connection is not established");
            }

            await _connection.InvokeAsync("SendIceCandidate", targetId, candidate);
        }

        public async Task SendChatMessageAsync(string message)
        {
            if (_connection?.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("Connection is not established");
            }

            await _connection.InvokeAsync("SendChatMessage", message);
        }

        public void On<T>(string methodName, Func<T, Task> handler)
        {
            _connection?.On(methodName, handler);
        }

        public void On<T1, T2>(string methodName, Func<T1, T2, Task> handler)
        {
            _connection?.On(methodName, handler);
        }

        public void On(string methodName, Func<Task> handler)
        {
            _connection?.On(methodName, handler);
        }
    }
}
