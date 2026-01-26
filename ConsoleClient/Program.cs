using ConsoleClient.Services;

namespace ConsoleClient
{
    internal class Program
    {
        static CameraCapture? _camera;
        static Dictionary<string, VideoStreamEncoder> _activeStreams = new();

        static async Task Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║  WebRTC Console Client - Streaming     ║");
            Console.WriteLine("╚════════════════════════════════════════╝\n");

            // Get configuration
            var backendServerUrl = Environment.GetEnvironmentVariable("BACKEND_SERVER_URL") ?? "https://192.168.68.236:5001";
            
            Console.Write("Enter your name: ");
            var userName = Console.ReadLine() ?? "User" + Random.Shared.Next(1000);

            Console.Write("Enter room ID: ");
            var roomId = Console.ReadLine() ?? "default-room";

            Console.WriteLine($"\nConnecting to: {backendServerUrl}");
            Console.WriteLine($"User: {userName}");
            Console.WriteLine($"Room: {roomId}\n");

            var connection = new ConferenceConnection(backendServerUrl, userName, roomId);

            // Initialize camera
            _camera = new CameraCapture(cameraIndex: 0, width: 640, height: 480, fps: 30);
            if (!_camera.Initialize())
            {
                Console.WriteLine("⚠️  Warning: Camera initialization failed. Video streaming will not be available.");
            }

            // Register event handlers
            connection.OnUserConnected += async (user) =>
            {
                Console.WriteLine($"→ {user.UserName} joined the conference");
                await Task.CompletedTask;
            };

            connection.OnUserDisconnected += async (user) =>
            {
                Console.WriteLine($"← {user.UserName} left the conference");
                // Stop streaming to disconnected user
                if (_activeStreams.ContainsKey(user.Id))
                {
                    var stream = _activeStreams[user.Id];
                    _activeStreams.Remove(user.Id);
                    stream.Dispose();
                }
                await Task.CompletedTask;
            };

            connection.OnRoomUsersReceived += async (roomUsers) =>
            {
                Console.WriteLine($"\n📋 Room Users ({roomUsers.Count}):");
                foreach (var user in roomUsers)
                {
                    Console.WriteLine($"  • {user.UserName} (ID: {user.Id.Substring(0, 8)}...)");
                }
                Console.WriteLine();
                await Task.CompletedTask;
            };

            connection.OnOfferReceived += async (offer) =>
            {
                Console.WriteLine($"📩 Offer from {offer.Sender.Substring(0, 8)}...");
                await Task.CompletedTask;
            };

            connection.OnAnswerReceived += async (answer) =>
            {
                Console.WriteLine($"📩 Answer from {answer.Sender.Substring(0, 8)}...");
                await Task.CompletedTask;
            };

            connection.OnIceCandidateReceived += async (candidate) =>
            {
                Console.WriteLine($"❄️  ICE Candidate from {candidate.Sender.Substring(0, 8)}...");
                await Task.CompletedTask;
            };

            connection.OnVideoFrameReceived += async (frame) =>
            {
                Console.WriteLine($"🎥 Video frame received - Frame #{frame.FrameIndex}, Size: {frame.BytesSize} bytes");
                await Task.CompletedTask;
            };

            try
            {
                // Connect and join room
                await connection.ConnectAsync();
                await connection.JoinRoomAsync();

                // Start camera capture
                if (_camera.IsRunning || _camera.Initialize())
                {
                    _camera.StartCapture();
                }

                // Display available commands
                DisplayHelp();

                // Main CLI loop
                await RunCliLoop(connection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
            finally
            {
                // Cleanup
                foreach (var stream in _activeStreams.Values)
                {
                    stream.Dispose();
                }
                _activeStreams.Clear();

                _camera?.Dispose();
                await connection.DisconnectAsync();
                Console.WriteLine("Goodbye!");
            }
        }

        static void DisplayHelp()
        {
            Console.WriteLine("\n═══════════════════════════════════════");
            Console.WriteLine("Available Commands:");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("  list               - Show users in room");
            Console.WriteLine("  offer <id>         - Send WebRTC offer to user");
            Console.WriteLine("  answer <id>        - Send WebRTC answer to user");
            Console.WriteLine("  ice <id>           - Send ICE candidate to user");
            Console.WriteLine("  stream <id>        - Start video stream to user");
            Console.WriteLine("  stop-stream <id>   - Stop video stream to user");
            Console.WriteLine("  streams            - List active streams");
            Console.WriteLine("  camera-info        - Show camera info");
            Console.WriteLine("  info               - Show connection info");
            Console.WriteLine("  help               - Show this help");
            Console.WriteLine("  exit               - Exit the application");
            Console.WriteLine("═══════════════════════════════════════\n");
        }

        static async Task RunCliLoop(ConferenceConnection connection)
        {
            while (true)
            {
                Console.Write("> ");
                var input = Console.ReadLine() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = parts[0].ToLower();

                try
                {
                    switch (command)
                    {
                        case "list":
                            DisplayRoomUsers(connection);
                            break;

                        case "offer":
                            if (parts.Length < 2)
                            {
                                Console.WriteLine("Usage: offer <user-id or index>");
                                break;
                            }
                            await SendOffer(connection, parts[1]);
                            break;

                        case "answer":
                            if (parts.Length < 2)
                            {
                                Console.WriteLine("Usage: answer <user-id or index>");
                                break;
                            }
                            await SendAnswer(connection, parts[1]);
                            break;

                        case "ice":
                            if (parts.Length < 2)
                            {
                                Console.WriteLine("Usage: ice <user-id or index>");
                                break;
                            }
                            await SendIceCandidate(connection, parts[1]);
                            break;

                        case "stream":
                            if (parts.Length < 2)
                            {
                                Console.WriteLine("Usage: stream <user-id or index> [quality]");
                                break;
                            }
                            StartVideoStream(connection, parts[1], parts.Length > 2 ? int.Parse(parts[2]) : 80);
                            break;

                        case "stop-stream":
                            if (parts.Length < 2)
                            {
                                Console.WriteLine("Usage: stop-stream <user-id or index>");
                                break;
                            }
                            StopVideoStream(parts[1], connection);
                            break;

                        case "streams":
                            DisplayActiveStreams();
                            break;

                        case "camera-info":
                            DisplayCameraInfo();
                            break;

                        case "info":
                            DisplayConnectionInfo(connection);
                            break;

                        case "help":
                            DisplayHelp();
                            break;

                        case "exit":
                        case "quit":
                            Console.WriteLine("Exiting...");
                            return;

                        default:
                            Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error executing command: {ex.Message}");
                }
            }
        }

        static void DisplayRoomUsers(ConferenceConnection connection)
        {
            Console.WriteLine("\n📋 Users in Room:");
            if (connection.RoomUsers.Count == 0)
            {
                Console.WriteLine("  No other users connected");
            }
            else
            {
                for (int i = 0; i < connection.RoomUsers.Count; i++)
                {
                    var user = connection.RoomUsers[i];
                    var streaming = _activeStreams.ContainsKey(user.Id) ? " 🎬" : "";
                    Console.WriteLine($"  [{i}] {user.UserName}{streaming}");
                    Console.WriteLine($"      ID: {user.Id}");
                }
            }
            Console.WriteLine();
        }

        static async Task SendOffer(ConferenceConnection connection, string targetIdentifier)
        {
            var targetId = GetTargetUserId(connection, targetIdentifier);
            if (string.IsNullOrEmpty(targetId))
            {
                Console.WriteLine("❌ User not found");
                return;
            }

            var sdp = GenerateSdp("offer");
            await connection.SendOfferAsync(targetId, sdp);
        }

        static async Task SendAnswer(ConferenceConnection connection, string targetIdentifier)
        {
            var targetId = GetTargetUserId(connection, targetIdentifier);
            if (string.IsNullOrEmpty(targetId))
            {
                Console.WriteLine("❌ User not found");
                return;
            }

            var sdp = GenerateSdp("answer");
            await connection.SendAnswerAsync(targetId, sdp);
        }

        static async Task SendIceCandidate(ConferenceConnection connection, string targetIdentifier)
        {
            var targetId = GetTargetUserId(connection, targetIdentifier);
            if (string.IsNullOrEmpty(targetId))
            {
                Console.WriteLine("❌ User not found");
                return;
            }

            var candidate = new
            {
                candidate = "candidate:1 1 UDP 2130706431 192.168.1.1 54321 typ host",
                sdpMLineIndex = 0,
                sdpMid = "0"
            };

            await connection.SendIceCandidateAsync(targetId, candidate);
        }

        static void StartVideoStream(ConferenceConnection connection, string targetIdentifier, int quality)
        {
            if (_camera == null || !_camera.IsRunning)
            {
                Console.WriteLine("❌ Camera is not available");
                return;
            }

            var targetId = GetTargetUserId(connection, targetIdentifier);
            if (string.IsNullOrEmpty(targetId))
            {
                Console.WriteLine("❌ User not found");
                return;
            }

            if (_activeStreams.ContainsKey(targetId))
            {
                Console.WriteLine("⚠️  Stream already active for this user. Use 'stop-stream' first.");
                return;
            }

            var encoder = new VideoStreamEncoder(connection, _camera, targetId, quality);
            encoder.StartStreaming();
            _activeStreams[targetId] = encoder;

            Console.WriteLine($"✓ Video stream started to {targetId.Substring(0, 8)}... (quality: {quality}%)");
        }

        static void StopVideoStream(string targetIdentifier, ConferenceConnection connection)
        {
            var targetId = GetTargetUserId(connection, targetIdentifier);
            if (string.IsNullOrEmpty(targetId))
            {
                Console.WriteLine("❌ User not found");
                return;
            }

            if (_activeStreams.ContainsKey(targetId))
            {
                var stream = _activeStreams[targetId];
                _activeStreams.Remove(targetId);
                stream.Dispose();
                Console.WriteLine($"✓ Video stream stopped for {targetId.Substring(0, 8)}...");
            }
            else
            {
                Console.WriteLine("⚠️  No active stream for this user");
            }
        }

        static void DisplayActiveStreams()
        {
            Console.WriteLine("\n🎬 Active Video Streams:");
            if (_activeStreams.Count == 0)
            {
                Console.WriteLine("  No active streams");
            }
            else
            {
                foreach (var kvp in _activeStreams)
                {
                    var stream = kvp.Value;
                    Console.WriteLine($"  • {kvp.Key.Substring(0, 8)}... - {stream.FrameCount} frames sent");
                }
            }
            Console.WriteLine();
        }

        static void DisplayCameraInfo()
        {
            Console.WriteLine("\n📷 Camera Info:");
            if (_camera == null)
            {
                Console.WriteLine("  Camera not initialized");
                return;
            }

            Console.WriteLine($"  Status: {(_camera.IsRunning ? "✓ Running" : "✗ Stopped")}");
            Console.WriteLine($"  Buffered frames: {_camera.GetBufferedFrameCount()}");
            Console.WriteLine();
        }

        static string? GetTargetUserId(ConferenceConnection connection, string identifier)
        {
            if (int.TryParse(identifier, out var index) && index >= 0 && index < connection.RoomUsers.Count)
            {
                return connection.RoomUsers[index].Id;
            }

            var user = connection.RoomUsers.FirstOrDefault(u => 
                u.UserName.Equals(identifier, StringComparison.OrdinalIgnoreCase));
            
            if (user != null)
                return user.Id;

            user = connection.RoomUsers.FirstOrDefault(u => u.Id.StartsWith(identifier));
            return user?.Id;
        }

        static void DisplayConnectionInfo(ConferenceConnection connection)
        {
            Console.WriteLine("\n📊 Connection Info:");
            Console.WriteLine($"  Status: {(connection.IsConnected ? "✓ Connected" : "✗ Disconnected")}");
            Console.WriteLine($"  Room Users: {connection.RoomUsers.Count}");
            Console.WriteLine($"  Active Streams: {_activeStreams.Count}");
            Console.WriteLine($"  Connection State: {connection.Connection?.State}");
            Console.WriteLine();
        }

        static string GenerateSdp(string type)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return $"""
                v=0
                o=user {timestamp} {timestamp} IN IP4 127.0.0.1
                s=-
                t=0 0
                a=group:BUNDLE 0 1
                a=extmap-allow-mixed
                a=msid-semantic: WMS stream
                m={type} 9 UDP/TLS/RTP/SAVPF 96 97 98 99 100 101 102 121 127 120 125 107 108 109 35 36 124 119 123 118 114 115 116
                c=IN IP4 0.0.0.0
                a=rtcp:9 IN IP4 0.0.0.0
                """;
        }
    }
}
