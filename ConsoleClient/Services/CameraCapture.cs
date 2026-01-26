using OpenCvSharp;

namespace ConsoleClient.Services
{
    public class CameraCapture : IDisposable
    {
        private VideoCapture? _capture;
        private readonly int _cameraIndex;
        private readonly int _width;
        private readonly int _height;
        private readonly int _fps;
        private bool _isRunning;
        private Thread? _captureThread;
        private readonly Queue<Mat> _frameBuffer = new();
        private readonly object _bufferLock = new();
        private const int MaxFrameBufferSize = 5;

        public event EventHandler<Mat>? FrameCaptured;
        public bool IsRunning => _isRunning;

        public CameraCapture(int cameraIndex = 0, int width = 640, int height = 480, int fps = 30)
        {
            _cameraIndex = cameraIndex;
            _width = width;
            _height = height;
            _fps = fps;
        }

        public bool Initialize()
        {
            try
            {
                _capture = new VideoCapture(_cameraIndex);
                
                if (!_capture.IsOpened())
                {
                    Console.WriteLine("? Failed to open camera");
                    return false;
                }

                _capture.Set(VideoCaptureProperties.FrameWidth, _width);
                _capture.Set(VideoCaptureProperties.FrameHeight, _height);
                _capture.Set(VideoCaptureProperties.Fps, _fps);

                Console.WriteLine($"? Camera initialized ({_width}x{_height} @ {_fps}fps)");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Camera initialization error: {ex.Message}");
                return false;
            }
        }

        public void StartCapture()
        {
            if (_isRunning || _capture == null)
                return;

            _isRunning = true;
            _captureThread = new Thread(CaptureLoop)
            {
                IsBackground = true,
                Name = "CameraCaptureThread"
            };
            _captureThread.Start();
            Console.WriteLine("??  Camera capture started");
        }

        public void StopCapture()
        {
            _isRunning = false;
            _captureThread?.Join(5000);
            Console.WriteLine("??  Camera capture stopped");
        }

        public Mat? GetLatestFrame()
        {
            lock (_bufferLock)
            {
                if (_frameBuffer.Count > 0)
                {
                    return _frameBuffer.Dequeue();
                }
            }
            return null;
        }

        public int GetBufferedFrameCount()
        {
            lock (_bufferLock)
            {
                return _frameBuffer.Count;
            }
        }

        private void CaptureLoop()
        {
            try
            {
                var frame = new Mat();
                int capturedFrames = 0;

                while (_isRunning && _capture != null)
                {
                    if (!_capture.Read(frame))
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    capturedFrames++;

                    // Add to buffer
                    lock (_bufferLock)
                    {
                        if (_frameBuffer.Count >= MaxFrameBufferSize)
                        {
                            var oldFrame = _frameBuffer.Dequeue();
                            oldFrame.Dispose();
                        }

                        var frameCopy = frame.Clone();
                        _frameBuffer.Enqueue(frameCopy);
                    }

                    FrameCaptured?.Invoke(this, frame);

                    // Log every 30 frames (~1 second at 30fps)
                    if (capturedFrames % 30 == 0)
                    {
                        Console.WriteLine($"?? Captured {capturedFrames} frames ({_frameBuffer.Count} buffered)");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error in capture loop: {ex.Message}");
            }
        }

        public void Dispose()
        {
            StopCapture();

            lock (_bufferLock)
            {
                while (_frameBuffer.Count > 0)
                {
                    _frameBuffer.Dequeue().Dispose();
                }
            }

            _capture?.Dispose();
        }
    }
}
