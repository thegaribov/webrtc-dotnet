using OpenCvSharp;

namespace ConsoleClient.Services
{
    public class VideoStreamEncoder : IDisposable
    {
        private readonly string _targetUserId;
        private readonly ConferenceConnection _connection;
        private readonly int _quality;
        private bool _isStreaming;
        private Thread? _streamThread;
        private readonly CameraCapture _camera;
        private int _frameCount;
        private DateTime _lastStatsTime;

        public bool IsStreaming => _isStreaming;
        public int FrameCount => _frameCount;

        public VideoStreamEncoder(ConferenceConnection connection, CameraCapture camera, string targetUserId, int quality = 80)
        {
            _connection = connection;
            _camera = camera;
            _targetUserId = targetUserId;
            _quality = Math.Clamp(quality, 1, 100);
            _lastStatsTime = DateTime.UtcNow;
        }

        public void StartStreaming()
        {
            if (_isStreaming)
                return;

            _isStreaming = true;
            _frameCount = 0;
            _lastStatsTime = DateTime.UtcNow;

            _streamThread = new Thread(StreamingLoop)
            {
                IsBackground = true,
                Name = "VideoStreamThread"
            };
            _streamThread.Start();
            Console.WriteLine($"?? Video streaming started to {_targetUserId.Substring(0, 8)}...");
        }

        public void StopStreaming()
        {
            _isStreaming = false;
            _streamThread?.Join(5000);
            Console.WriteLine("?? Video streaming stopped");
        }

        private void StreamingLoop()
        {
            try
            {
                while (_isStreaming)
                {
                    var frame = _camera.GetLatestFrame();
                    if (frame == null)
                    {
                        Thread.Sleep(33);
                        continue;
                    }

                    try
                    {
                        byte[] buffer;
                        Cv2.ImEncode(".jpg", frame, out buffer, new int[] { (int)ImwriteFlags.JpegQuality, _quality });

                        var frameData = new VideoFrameData
                        {
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            FrameIndex = _frameCount,
                            Width = frame.Width,
                            Height = frame.Height,
                            EncodedData = Convert.ToBase64String(buffer),
                            QualityLevel = _quality,
                            BytesSize = buffer.Length
                        };

                        SendFrameAsync(frameData).Wait(100);

                        _frameCount++;

                        var now = DateTime.UtcNow;
                        if ((now - _lastStatsTime).TotalSeconds >= 5)
                        {
                            var fps = _frameCount / (now - _lastStatsTime).TotalSeconds;
                            Console.WriteLine($"?? Video Stream: {_frameCount} frames, {fps:F1} fps, {frameData.BytesSize} bytes/frame");
                            _frameCount = 0;
                            _lastStatsTime = now;
                        }
                    }
                    finally
                    {
                        frame.Dispose();
                    }

                    Thread.Sleep(33);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error in streaming loop: {ex.Message}");
            }
        }

        private async Task SendFrameAsync(VideoFrameData frameData)
        {
            try
            {
                await _connection.SendVideoFrameAsync(_targetUserId, frameData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error sending frame: {ex.Message}");
            }
        }

        public void Dispose()
        {
            StopStreaming();
        }
    }
}
