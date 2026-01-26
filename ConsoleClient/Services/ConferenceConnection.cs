using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace ConsoleClient.Services
{
    public class ConferenceConnection
    {
        private HubConnection? _connection;
        private readonly string _serverUrl;
        private readonly string _userName;
        private readonly string _roomId;

        public HubConnection? Connection => _connection;
        public bool IsConnected => _connection?.State == HubConnectionState.Connected;
        public List<RoomUser> RoomUsers { get; private set; } = new();

        public event Func<RoomUser, Task>? OnUserConnected;
        public event Func<RoomUser, Task>? OnUserDisconnected;
        public event Func<OfferData, Task>? OnOfferReceived;
        public event Func<AnswerData, Task>? OnAnswerReceived;
        public event Func<IceCandidateData, Task>? OnIceCandidateReceived;
        public event Func<List<RoomUser>, Task>? OnRoomUsersReceived;
        public event Func<VideoFrameData, Task>? OnVideoFrameReceived;

        public ConferenceConnection(string serverUrl, string userName, string roomId)
        {
            _serverUrl = serverUrl;
            _userName = userName;
            _roomId = roomId;
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
                    
                    // Accept self-signed certificates for development
                    if (_serverUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                    {
                        options.HttpMessageHandlerFactory = (message) =>
                        {
                            if (message is HttpClientHandler clientHandler)
                            {
                                clientHandler.ServerCertificateCustomValidationCallback =
                                    (sender, cert, chain, sslPolicyErrors) =>
                                    {
                                        // Accept all certificates for development
                                        // In production, implement proper certificate validation
                                        Console.WriteLine($"? SSL Certificate: {cert?.Subject}");
                                        return true;
                                    };
                            }
                            return message;
                        };
                    }
                })
                .WithAutomaticReconnect()
                .Build();

            // Register event handlers
            _connection.On<List<RoomUser>>("RoomUsers", async roomUsers =>
            {
                RoomUsers = roomUsers;
                if (OnRoomUsersReceived != null)
                    await OnRoomUsersReceived(roomUsers);
            });

            _connection.On<RoomUser>("UserConnected", async user =>
            {
                Console.WriteLine($"\n? User connected: {user.UserName} ({user.Id})");
                if (OnUserConnected != null)
                    await OnUserConnected(user);
            });

            _connection.On<RoomUser>("UserDisconnected", async user =>
            {
                Console.WriteLine($"\n? User disconnected: {user.UserName}");
                if (OnUserDisconnected != null)
                    await OnUserDisconnected(user);
            });

            _connection.On<dynamic>("Offer", async offer =>
            {
                var offerData = new OfferData
                {
                    Sdp = offer.sdp.ToString(),
                    Sender = offer.sender.ToString()
                };
                Console.WriteLine($"\n?? Offer received from {offerData.Sender.Substring(0, 8)}...");
                if (OnOfferReceived != null)
                    await OnOfferReceived(offerData);
            });

            _connection.On<dynamic>("Answer", async answer =>
            {
                var answerData = new AnswerData
                {
                    Sdp = answer.sdp.ToString(),
                    Sender = answer.sender.ToString()
                };
                Console.WriteLine($"\n?? Answer received from {answerData.Sender.Substring(0, 8)}...");
                if (OnAnswerReceived != null)
                    await OnAnswerReceived(answerData);
            });

            _connection.On<dynamic>("IceCandidate", async candidate =>
            {
                var iceCandidateData = new IceCandidateData
                {
                    Candidate = candidate.candidate,
                    Sender = candidate.sender.ToString()
                };
                Console.WriteLine($"\n??  ICE Candidate received from {iceCandidateData.Sender.Substring(0, 8)}...");
                if (OnIceCandidateReceived != null)
                    await OnIceCandidateReceived(iceCandidateData);
            });

            _connection.On<dynamic>("VideoFrame", async frame =>
            {
                try
                {
                    var frameData = new VideoFrameData
                    {
                        Timestamp = (long)frame.Timestamp,
                        FrameIndex = (int)frame.FrameIndex,
                        Width = (int)frame.Width,
                        Height = (int)frame.Height,
                        EncodedData = frame.EncodedData.ToString(),
                        QualityLevel = (int)frame.QualityLevel,
                        BytesSize = (int)frame.BytesSize
                    };
                    if (OnVideoFrameReceived != null)
                        await OnVideoFrameReceived(frameData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"? Error processing video frame: {ex.Message}");
                }
            });

            await _connection.StartAsync();
            Console.WriteLine("? Connected to SignalR hub");
        }

        public async Task JoinRoomAsync()
        {
            if (_connection?.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("Connection is not established");
            }

            await _connection.InvokeAsync("JoinRoom", _roomId, _userName);
            Console.WriteLine($"? Joined room: {_roomId}");
        }

        public async Task SendOfferAsync(string targetId, string sdp)
        {
            if (_connection?.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("Connection is not established");
            }

            await _connection.InvokeAsync("SendOffer", targetId, sdp);
            Console.WriteLine($"?? Offer sent to {targetId}");
        }

        public async Task SendAnswerAsync(string targetId, string sdp)
        {
            if (_connection?.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("Connection is not established");
            }

            await _connection.InvokeAsync("SendAnswer", targetId, sdp);
            Console.WriteLine($"?? Answer sent to {targetId}");
        }

        public async Task SendIceCandidateAsync(string targetId, object candidate)
        {
            if (_connection?.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("Connection is not established");
            }

            await _connection.InvokeAsync("SendIceCandidate", targetId, candidate);
        }

        public async Task SendVideoFrameAsync(string targetId, VideoFrameData frame)
        {
            if (_connection?.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("Connection is not established");
            }

            try
            {
                await _connection.InvokeAsync("SendVideoFrame", targetId, frame);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to send video frame: {ex.Message}", ex);
            }
        }

        public async Task DisconnectAsync()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
            Console.WriteLine("? Disconnected from SignalR hub");
        }
    }

    public class RoomUser
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string RoomId { get; set; } = string.Empty;
    }

    public class OfferData
    {
        public string Sdp { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
    }

    public class AnswerData
    {
        public string Sdp { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
    }

    public class IceCandidateData
    {
        public object Candidate { get; set; } = new object();
        public string Sender { get; set; } = string.Empty;
    }

    public class VideoFrameData
    {
        public long Timestamp { get; set; }
        public int FrameIndex { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string EncodedData { get; set; } = string.Empty;
        public int QualityLevel { get; set; }
        public int BytesSize { get; set; }
    }
}
