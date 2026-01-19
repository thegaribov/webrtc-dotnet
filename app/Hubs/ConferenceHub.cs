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
            var roomUser = new RoomUser
            {
                Id = Context.ConnectionId,
                UserName = userName,
                RoomId = roomId
            };
            Users[Context.ConnectionId] = roomUser;

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
            await Clients.Group(roomId).SendAsync("UserConnected", roomUser);

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

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (Users.TryRemove(Context.ConnectionId, out RoomUser roomUser))
            {
                if (Rooms.TryGetValue(roomUser.RoomId, out var roomUsers))
                {
                    roomUsers.Remove(Context.ConnectionId);

                    // Notify others that user disconnected
                    await Clients.Group(roomUser.RoomId).SendAsync("UserDisconnected", Context.ConnectionId);

                    Console.WriteLine($"{roomUser.UserName} left room {roomUser.RoomId}");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
