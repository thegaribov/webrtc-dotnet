using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace WebRtcConference.Hubs
{
    public class RoomUser
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string RoomId { get; set; } = string.Empty;
    }

    public class ConferenceHub : Hub
    {
        private static readonly ConcurrentDictionary<string, RoomUser> Users = new();
        private static readonly ConcurrentDictionary<string, HashSet<string>> Rooms = new();

        public async Task JoinRoom(string roomId, string userName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // Store user information
            var user = new RoomUser
            {
                Id = Context.ConnectionId,
                UserName = userName,
                RoomId = roomId
            };
            Users[Context.ConnectionId] = user;

            // Initialize room if it doesn't exist
            Rooms.TryAdd(roomId, new HashSet<string>());
            Rooms[roomId].Add(Context.ConnectionId);

            // Get existing room users (excluding current user)
            var roomUsers = Rooms[roomId]
                .Where(id => id != Context.ConnectionId)
                .Select(id => Users.TryGetValue(id, out var u) ? u : null)
                .Where(u => u != null)
                .ToList();

            // Send existing users to the new user
            await Clients.Caller.SendAsync("RoomUsers", roomUsers);

            // Notify others that a new user connected
            await Clients.Group(roomId).SendAsync("UserConnected", user);

            Console.WriteLine($"{userName} joined room {roomId}");
        }

        // WebRTC Signaling
        public async Task SendOffer(string targetId, string sdp)
        {
            try
            {
                Console.WriteLine($"SendOffer from {Context.ConnectionId} to {targetId}");
                await Clients.Client(targetId).SendAsync("Offer", new
                {
                    sdp = sdp,
                    sender = Context.ConnectionId
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendOffer: {ex.Message}");
                throw;
            }
        }

        public async Task SendAnswer(string targetId, string sdp)
        {
            try
            {
                Console.WriteLine($"SendAnswer from {Context.ConnectionId} to {targetId}");
                await Clients.Client(targetId).SendAsync("Answer", new
                {
                    sdp = sdp,
                    sender = Context.ConnectionId
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendAnswer: {ex.Message}");
                throw;
            }
        }

        public async Task SendIceCandidate(string targetId, object candidate)
        {
            await Clients.Client(targetId).SendAsync("IceCandidate", new
            {
                candidate = candidate,
                sender = Context.ConnectionId
            });
        }

        // Chat messages
        public async Task SendChatMessage(string message)
        {
            if (Users.TryGetValue(Context.ConnectionId, out var user))
            {
                await Clients.Group(user.RoomId).SendAsync("ChatMessage", new
                {
                    userName = user.UserName,
                    message = message,
                    time = DateTime.Now.ToString("HH:mm:ss")
                });
            }
        }

        // Media state (audio/video)
        public async Task SendMediaState(bool audio, bool video)
        {
            if (Users.TryGetValue(Context.ConnectionId, out var user))
            {
                await Clients.Group(user.RoomId).SendAsync("UserMediaState", new
                {
                    userId = Context.ConnectionId,
                    audio = audio,
                    video = video
                });
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Users.TryRemove(Context.ConnectionId, out var user))
            {
                if (Rooms.TryGetValue(user.RoomId, out var roomUsers))
                {
                    roomUsers.Remove(Context.ConnectionId);

                    // Notify others that user disconnected
                    await Clients.Group(user.RoomId).SendAsync("UserDisconnected", Context.ConnectionId);

                    Console.WriteLine($"{user.UserName} left room {user.RoomId}");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
