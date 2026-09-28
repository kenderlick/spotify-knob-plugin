using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpotifyKnob
{
    public class PluginSettings
    {
        public int volumeStep = 5;
        public bool openIfClosed = true;
        public string volumeMode = "app"; // "app", "master", "keys"
        public bool showVolumeFeedback = true;
    }

    public static class Program
    {
        private static ClientWebSocket _ws;
        private static string _pluginUUID = "";
        private static string _port = "";
        private static string _registerEvent = "";

        private static readonly Dictionary<string, PluginSettings> _contextSettings = new Dictionary<string, PluginSettings>();
        private static readonly Dictionary<string, string> _lastContextImage = new Dictionary<string, string>();
        
        private static readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private static Timer _renderTimer;
        private static StreamWriter _logWriter;
        private static readonly object _logLock = new object();
        private static readonly object _stateLock = new object();

        // Knob Click & Double-Click Timing
        private static DateTime _lastKnobClickTime = DateTime.MinValue;

        // Track Selector Mode
        private static bool _isTrackSelectMode = false;
        private static DateTime _trackModeExpireTime = DateTime.MinValue;
        private const double TRACK_MODE_TIMEOUT_SEC = 4.0;

        // Transient UI states (volume popup, opening)
        private static string _transientType = null;
        private static DateTime _transientStartTime = DateTime.MinValue;
        private static int _transientDurationMs = 1800;
        private static int _transientFadeMs = 500;

        // Current Spotify state
        private static int _currentVolume = 50;
        private static SpotifyTrackInfo _currentTrack = new SpotifyTrackInfo();
        private static DateTime _lastTrackPollTime = DateTime.MinValue;

        // Marquee scrolling state
        private static int _marqueePixelOffset = 0;
        private static int _marqueePauseFrames = 8;
        private static string _prevTitle = "";
        private static bool _marqueeEndPause = false;

        [STAThread]
        public static void Main(string[] args)
        {
            SetupLogging();
            LogInfo("=== SpotifyKnob (C# Native Engine v1.8) Starting ===");
            LogInfo("Args: " + string.Join(" ", args));

            ParseArguments(args);

            if (string.IsNullOrEmpty(_port) || string.IsNullOrEmpty(_pluginUUID) || string.IsNullOrEmpty(_registerEvent))
            {
                LogError("Missing required parameters (-port, -pluginUUID, or -registerEvent). Terminating.");
                return;
            }

            try
            {
                // Smooth render timer (250ms tick ~ 4 FPS: zero CPU, no socket overload)
                _renderTimer = new Timer(OnRenderTick, null, 300, 250);

                RunWebSocketLoopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                LogError("Fatal WebSocket Exception: " + ex);
            }
            finally
            {
                if (_renderTimer != null)
                {
                    _renderTimer.Dispose();
                }
            }

            LogInfo("SpotifyKnob process exiting.");
        }

        private static void ParseArguments(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                string lower = arg.ToLowerInvariant();

                if (lower == "-port" || lower == "--port")
                {
                    if (i + 1 < args.Length) _port = args[++i];
                }
                else if (lower == "-pluginuuid" || lower == "--pluginuuid" || lower == "-uuid" || lower == "--uuid")
                {
                    if (i + 1 < args.Length) _pluginUUID = args[++i];
                }
                else if (lower == "-registerevent" || lower == "--registerevent" || lower == "-event" || lower == "--event")
                {
                    if (i + 1 < args.Length) _registerEvent = args[++i];
                }
                else if (lower.StartsWith("-port=") || lower.StartsWith("--port="))
                {
                    _port = arg.Substring(arg.IndexOf('=') + 1);
                }
                else if (lower.StartsWith("-pluginuuid=") || lower.StartsWith("--pluginuuid="))
                {
                    _pluginUUID = arg.Substring(arg.IndexOf('=') + 1);
                }
                else if (lower.StartsWith("-registerevent=") || lower.StartsWith("--registerevent="))
                {
                    _registerEvent = arg.Substring(arg.IndexOf('=') + 1);
                }
            }
            LogInfo(string.Format("Parsed: Port={0}, UUID={1}, Event={2}", _port, _pluginUUID, _registerEvent));
        }

        private static async Task RunWebSocketLoopAsync()
        {
            _ws = new ClientWebSocket();
            var uri = new Uri(string.Format("ws://127.0.0.1:{0}", _port));
            LogInfo("Connecting to StreamDock at: " + uri);

            await _ws.ConnectAsync(uri, CancellationToken.None);
            LogInfo("Connected to StreamDock WebSocket successfully.");

            // Send registration message
            var regJson = string.Format("{{\"event\":\"{0}\",\"uuid\":\"{1}\"}}", _registerEvent, _pluginUUID);
            await SendRawJsonAsync(regJson);
            LogInfo("Registration message sent: " + regJson);

            var buffer = new byte[65536];
            while (_ws.State == WebSocketState.Open)
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    LogInfo("WebSocket close received.");
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                HandleMessage(message);
            }
        }

        private static void HandleMessage(string jsonStr)
        {
            try
            {
                var json = SimpleJson.Parse(jsonStr);
                if (json == null) return;

                string ev = json.GetString("event", "").ToLowerInvariant();
                string action = json.GetString("action", "");
                string context = json.GetString("context", "");
                var payload = json.GetObject("payload");

                LogInfo(string.Format("Received event: '{0}', context: '{1}'", ev, context));

                switch (ev)
                {
                    case "willappear":
                        {
                            var settings = ParseSettings(payload != null ? payload.GetObject("settings") : null);
                            lock (_stateLock)
                            {
                                _contextSettings[context] = settings;
                                _lastContextImage.Remove(context);
                            }
                            LogInfo(string.Format("Context {0} willAppear. Step: {1}%, Mode: {2}", context, settings.volumeStep, settings.volumeMode));
                            
                            // Clear plain text label on appearance
                            SetTitle(context, "");
                            TriggerRender();
                            break;
                        }

                    case "willdisappear":
                        {
                            lock (_stateLock)
                            {
                                _contextSettings.Remove(context);
                                _lastContextImage.Remove(context);
                            }
                            LogInfo(string.Format("Context {0} willDisappear", context));
                            break;
                        }

                    case "didreceivesettings":
                        {
                            if (payload != null)
                            {
                                var settings = ParseSettings(payload.GetObject("settings"));
                                lock (_stateLock)
                                {
                                    _contextSettings[context] = settings;
                                }
                                LogInfo(string.Format("Settings updated for {0}: Step={1}%, Mode={2}", context, settings.volumeStep, settings.volumeMode));
                            }
                            break;
                        }

                    // Physical Knob button / Keypad button
                    case "dialdown":
                    case "keydown":
                    case "dialpress":
                        HandleKnobPress(context);
                        break;

                    // Touch Screen Tap
                    case "touchtap":
                    case "touchstart":
                    case "touchdown":
                        HandleTouchTap(context);
                        break;

                    case "rotateclockwise":
                        HandleRotate(context, 1);
                        break;

                    case "rotatecounterclockwise":
                        HandleRotate(context, -1);
                        break;

                    case "dialrotate":
                        int ticks = payload != null ? payload.GetInt("ticks", 1) : 1;
                        if (ticks == 0) ticks = 1;
                        HandleRotate(context, ticks);
                        break;

                    case "sendtoplugin":
                        if (payload != null)
                        {
                            string cmd = payload.GetString("command", "");
                            HandlePropertyInspectorCommand(context, cmd);
                        }
                        break;

                    case "propertyinspectordidappear":
                        {
                            var settings = GetSettings(context);
                            bool running = SpotifyController.IsSpotifyRunning();
                            int vol = AudioController.GetSpotifyVolume();
                            SendToPropertyInspector(context, string.Format(
                                "{{\"event\":\"init\",\"spotifyRunning\":{0},\"currentVolume\":{1},\"settings\":{{\"volumeStep\":{2},\"openIfClosed\":{3},\"volumeMode\":\"{4}\",\"showVolumeFeedback\":{5}}}}}",
                                running ? "true" : "false",
                                vol >= 0 ? vol : 50,
                                settings.volumeStep,
                                settings.openIfClosed ? "true" : "false",
                                settings.volumeMode,
                                settings.showVolumeFeedback ? "true" : "false"
                            ));
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                LogError("HandleMessage error: " + ex);
            }
        }

        private static void HandleKnobPress(string context)
        {
            var now = DateTime.UtcNow;
            double msSinceLast = (now - _lastKnobClickTime).TotalMilliseconds;

            if (msSinceLast < 450)
            {
                // Double click on Knob -> Activate Track Selection Mode
                _lastKnobClickTime = DateTime.MinValue;
                LogInfo("Knob Double-Click detected -> Activating Track Selection Mode...");
                ActivateTrackMode(context);
            }
            else
            {
                // Single click on Knob -> Instant Play/Pause toggle
                _lastKnobClickTime = now;
                LogInfo("Knob Single-Click detected -> Toggling Play/Pause...");
                HandleTogglePlayPause(context);
            }
        }

        private static void HandleTouchTap(string context)
        {
            LogInfo("Touch Tap on LCD screen detected -> Toggling Track Selection Mode...");
            ToggleTrackMode(context);
        }

        private static void ActivateTrackMode(string context)
        {
            lock (_stateLock)
            {
                _isTrackSelectMode = true;
                _trackModeExpireTime = DateTime.UtcNow.AddSeconds(TRACK_MODE_TIMEOUT_SEC);
            }
            // Ensure playback is running so user can listen while changing tracks
            SpotifyController.EnsurePlay();
            lock (_stateLock)
            {
                _currentTrack.IsPlaying = true;
            }
            TriggerRender();
        }

        private static void ToggleTrackMode(string context)
        {
            bool currentlyActive;
            lock (_stateLock)
            {
                currentlyActive = _isTrackSelectMode && (DateTime.UtcNow <= _trackModeExpireTime);
                _isTrackSelectMode = !currentlyActive;
                if (_isTrackSelectMode)
                {
                    _trackModeExpireTime = DateTime.UtcNow.AddSeconds(TRACK_MODE_TIMEOUT_SEC);
                }
            }

            if (!currentlyActive)
            {
                SpotifyController.EnsurePlay();
                lock (_stateLock)
                {
                    _currentTrack.IsPlaying = true;
                }
            }
            TriggerRender();
        }

        private static void HandleTogglePlayPause(string context)
        {
            var settings = GetSettings(context);
            bool running = SpotifyController.IsSpotifyRunning();
            LogInfo(string.Format("Toggle Play/Pause: Spotify running={0}, OpenIfClosed={1}", running, settings.openIfClosed));

            // Clicking knob turns off track selection mode if active
            lock (_stateLock)
            {
                _isTrackSelectMode = false;
            }

            if (!running)
            {
                if (settings.openIfClosed)
                {
                    LogInfo("Spotify is closed -> Launching Spotify...");
                    SetTransientState("opening", 2500, 600);
                    SpotifyController.LaunchSpotify();
                }
                else
                {
                    ShowAlert(context);
                }
            }
            else
            {
                var currentTrack = SpotifyController.GetTrackInfo();
                bool wasPlaying = currentTrack.IsPlaying;

                LogInfo("Spotify is running -> Toggling Play/Pause (wasPlaying=" + wasPlaying + ")...");
                SpotifyController.TogglePlayPause();

                // Immediately update local track playing state for fast visual response on top icon
                lock (_stateLock)
                {
                    _currentTrack.IsPlaying = !wasPlaying;
                }
                TriggerRender();
            }
        }

        private static void HandleRotate(string context, int ticks)
        {
            bool isTrackMode = false;
            lock (_stateLock)
            {
                if (_isTrackSelectMode)
                {
                    if (DateTime.UtcNow <= _trackModeExpireTime)
                    {
                        isTrackMode = true;
                        // Extend timeout on every rotation tick
                        _trackModeExpireTime = DateTime.UtcNow.AddSeconds(TRACK_MODE_TIMEOUT_SEC);
                    }
                    else
                    {
                        _isTrackSelectMode = false;
                    }
                }
            }

            if (isTrackMode)
            {
                if (ticks > 0)
                {
                    LogInfo("Track Mode: Next Track (ticks=" + ticks + ")");
                    SpotifyController.NextTrack();
                }
                else
                {
                    LogInfo("Track Mode: Previous Track (ticks=" + ticks + ")");
                    SpotifyController.PreviousTrack();
                }
                TriggerRender();
                return;
            }

            // Normal Volume Control
            var settings = GetSettings(context);
            int step = settings.volumeStep > 0 ? settings.volumeStep : 5;
            float delta = (ticks * step) / 100.0f;

            LogInfo(string.Format("Rotate Volume: ticks={0}, step={1}%, delta={2:F2}, mode={3}", ticks, step, delta, settings.volumeMode));

            int newVol = -1;
            switch ((settings.volumeMode ?? "app").ToLowerInvariant())
            {
                case "master":
                    newVol = AudioController.AdjustMasterVolume(delta);
                    break;
                case "keys":
                    SpotifyController.SendVolumeKeys(ticks > 0);
                    newVol = AudioController.GetSpotifyVolume();
                    break;
                default: // "app"
                    newVol = AudioController.AdjustSpotifyVolume(delta);
                    break;
            }

            if (newVol >= 0)
            {
                _currentVolume = newVol;
            }
            else
            {
                _currentVolume = Math.Max(0, Math.Min(100, _currentVolume + (ticks * step)));
            }

            // Show volume overlay for 1800ms with 500ms fade out
            SetTransientState("volume", 1800, 500);
        }

        private static void SetTransientState(string type, int durationMs, int fadeMs)
        {
            lock (_stateLock)
            {
                _transientType = type;
                _transientStartTime = DateTime.UtcNow;
                _transientDurationMs = durationMs;
                _transientFadeMs = fadeMs;
            }
            TriggerRender();
        }

        private static void HandlePropertyInspectorCommand(string context, string command)
        {
            LogInfo("PI Command: " + command);
            switch (command)
            {
                case "playPause":
                    HandleTogglePlayPause(context);
                    break;
                case "nextTrack":
                    SpotifyController.NextTrack();
                    break;
                case "prevTrack":
                    SpotifyController.PreviousTrack();
                    break;
                case "toggleTrackMode":
                    ToggleTrackMode(context);
                    break;
                case "openSpotify":
                    SpotifyController.LaunchSpotify();
                    SetTransientState("opening", 2500, 600);
                    break;
                case "volUp":
                    {
                        int vol = AudioController.AdjustSpotifyVolume(0.05f);
                        if (vol >= 0) _currentVolume = vol;
                        SetTransientState("volume", 1800, 500);
                        SendToPropertyInspector(context, string.Format("{{\"event\":\"volumeUpdate\",\"volume\":{0}}}", _currentVolume));
                        break;
                    }
                case "volDown":
                    {
                        int vol = AudioController.AdjustSpotifyVolume(-0.05f);
                        if (vol >= 0) _currentVolume = vol;
                        SetTransientState("volume", 1800, 500);
                        SendToPropertyInspector(context, string.Format("{{\"event\":\"volumeUpdate\",\"volume\":{0}}}", _currentVolume));
                        break;
                    }
                case "getStatus":
                    {
                        bool running = SpotifyController.IsSpotifyRunning();
                        int vol = AudioController.GetSpotifyVolume();
                        SendToPropertyInspector(context, string.Format("{{\"event\":\"status\",\"spotifyRunning\":{0},\"currentVolume\":{1}}}", running ? "true" : "false", vol >= 0 ? vol : _currentVolume));
                        break;
                    }
            }
        }

        private static void TriggerRender()
        {
            Task.Run(() => OnRenderTick(null));
        }

        private static void OnRenderTick(object state)
        {
            try
            {
                List<string> contexts;
                lock (_stateLock)
                {
                    contexts = new List<string>(_contextSettings.Keys);
                }
                if (contexts.Count == 0) return;

                // Check Track Mode timeout status
                bool trackModeActive = false;
                lock (_stateLock)
                {
                    if (_isTrackSelectMode)
                    {
                        if (DateTime.UtcNow <= _trackModeExpireTime)
                        {
                            trackModeActive = true;
                        }
                        else
                        {
                            _isTrackSelectMode = false;
                            LogInfo("Track selection mode timeout expired. Returned to normal volume mode.");
                        }
                    }
                }

                // Poll Spotify track info every ~400ms (fast poll for instant title update on song change)
                if ((DateTime.UtcNow - _lastTrackPollTime).TotalMilliseconds >= 400)
                {
                    _lastTrackPollTime = DateTime.UtcNow;
                    _currentTrack = SpotifyController.GetTrackInfo();
                    int vol = AudioController.GetSpotifyVolume();
                    if (vol >= 0) _currentVolume = vol;
                }

                // Compute Marquee Scrolling
                string currentTitle = _currentTrack != null ? _currentTrack.Title : "";
                if (currentTitle != _prevTitle)
                {
                    _prevTitle = currentTitle;
                    _marqueePixelOffset = 0;
                    _marqueePauseFrames = 8;
                    _marqueeEndPause = false;
                }
                else if (!string.IsNullOrEmpty(currentTitle))
                {
                    int totalTextWidth = (int)(currentTitle.Length * 11.5);
                    int maxScroll = Math.Max(0, totalTextWidth - 100);

                    if (maxScroll > 0)
                    {
                        if (_marqueePauseFrames > 0)
                        {
                            _marqueePauseFrames--;
                        }
                        else if (!_marqueeEndPause)
                        {
                            _marqueePixelOffset += 4; // 4px per tick = 16px/sec
                            if (_marqueePixelOffset >= maxScroll)
                            {
                                _marqueeEndPause = true;
                                _marqueePauseFrames = 8;
                            }
                        }
                        else
                        {
                            _marqueePixelOffset = 0;
                            _marqueeEndPause = false;
                            _marqueePauseFrames = 8;
                        }
                    }
                    else
                    {
                        _marqueePixelOffset = 0;
                    }
                }

                // Compute Dissolve / Fade-Out for Overlays (Volume / Opening)
                string overlayType = null;
                double overlayOpacity = 0.0;

                lock (_stateLock)
                {
                    if (!string.IsNullOrEmpty(_transientType))
                    {
                        double elapsed = (DateTime.UtcNow - _transientStartTime).TotalMilliseconds;
                        int solidDuration = _transientDurationMs - _transientFadeMs;

                        if (elapsed < solidDuration)
                        {
                            overlayType = _transientType;
                            overlayOpacity = 1.0;
                        }
                        else if (elapsed < _transientDurationMs)
                        {
                            overlayType = _transientType;
                            double progress = (elapsed - solidDuration) / (double)_transientFadeMs;
                            overlayOpacity = Math.Max(0.0, 1.0 - progress);
                        }
                        else
                        {
                            _transientType = null;
                            overlayType = null;
                            overlayOpacity = 0.0;
                        }
                    }
                }

                // Generate SVG Image
                string targetImage = SvgRenderer.RenderComposite(
                    _currentTrack.IsRunning,
                    _currentTrack.IsPlaying,
                    _currentTrack.Title,
                    _currentTrack.Artist,
                    _marqueePixelOffset,
                    overlayType,
                    overlayOpacity,
                    _currentVolume,
                    trackModeActive
                );

                string fbTitle = _currentTrack.IsPlaying ? _currentTrack.Artist : "Spotify";
                string fbVal = _currentTrack.IsPlaying ? _currentTrack.Title : (_currentTrack.IsRunning ? "In Pausa" : "Chiuso");

                foreach (var ctx in contexts)
                {
                    bool changed = false;
                    lock (_stateLock)
                    {
                        string lastImg;
                        if (!_lastContextImage.TryGetValue(ctx, out lastImg) || lastImg != targetImage)
                        {
                            _lastContextImage[ctx] = targetImage;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        SetImage(ctx, targetImage);
                        SendFeedback(ctx, string.Format("{{\"title\":\"{0}\",\"value\":\"{1}\",\"indicator\":{2}}}", 
                            EscapeJson(fbTitle), 
                            EscapeJson(fbVal), 
                            _currentVolume));
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("OnRenderTick error: " + ex.Message);
            }
        }

        private static string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\\", "\\\\")
                      .Replace("\"", "\\\"")
                      .Replace("\r", "")
                      .Replace("\n", "\\n");
        }

        private static PluginSettings ParseSettings(SimpleJson.JsonObject obj)
        {
            var s = new PluginSettings();
            if (obj != null)
            {
                s.volumeStep = obj.GetInt("volumeStep", 5);
                s.openIfClosed = obj.GetBool("openIfClosed", true);
                s.volumeMode = obj.GetString("volumeMode", "app");
                s.showVolumeFeedback = obj.GetBool("showVolumeFeedback", true);
            }
            if (s.volumeStep <= 0) s.volumeStep = 5;
            if (string.IsNullOrEmpty(s.volumeMode)) s.volumeMode = "app";
            return s;
        }

        private static PluginSettings GetSettings(string context)
        {
            lock (_stateLock)
            {
                PluginSettings s;
                if (!string.IsNullOrEmpty(context) && _contextSettings.TryGetValue(context, out s))
                {
                    return s;
                }
            }
            return new PluginSettings();
        }

        public static async Task SendRawJsonAsync(string json)
        {
            if (_ws == null || _ws.State != WebSocketState.Open) return;

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await _sendLock.WaitAsync();
                try
                {
                    if (_ws != null && _ws.State == WebSocketState.Open)
                    {
                        await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                finally
                {
                    _sendLock.Release();
                }
            }
            catch (Exception ex)
            {
                LogError("SendRawJsonAsync error: " + ex.Message);
            }
        }

        public static void SetImage(string context, string base64Image)
        {
            string msg = string.Format("{{\"event\":\"setImage\",\"context\":\"{0}\",\"payload\":{{\"image\":\"{1}\",\"target\":0}}}}", context, base64Image);
            Task.Run(() => SendRawJsonAsync(msg));
        }

        public static void SetTitle(string context, string title)
        {
            string msg = string.Format("{{\"event\":\"setTitle\",\"context\":\"{0}\",\"payload\":{{\"title\":\"{1}\",\"target\":0}}}}", context, title);
            Task.Run(() => SendRawJsonAsync(msg));
        }

        public static void ShowOk(string context)
        {
            string msg = string.Format("{{\"event\":\"showOk\",\"context\":\"{0}\"}}", context);
            Task.Run(() => SendRawJsonAsync(msg));
        }

        public static void ShowAlert(string context)
        {
            string msg = string.Format("{{\"event\":\"showAlert\",\"context\":\"{0}\"}}", context);
            Task.Run(() => SendRawJsonAsync(msg));
        }

        public static void SendFeedback(string context, string payloadJson)
        {
            string msg = string.Format("{{\"event\":\"setFeedback\",\"context\":\"{0}\",\"payload\":{1}}}", context, payloadJson);
            Task.Run(() => SendRawJsonAsync(msg));
        }

        public static void SendToPropertyInspector(string context, string payloadJson)
        {
            string msg = string.Format("{{\"event\":\"sendToPropertyInspector\",\"context\":\"{0}\",\"payload\":{1}}}", context, payloadJson);
            Task.Run(() => SendRawJsonAsync(msg));
        }

        private static void SetupLogging()
        {
            try
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string binDir = Path.GetDirectoryName(exePath);
                string baseDir = Path.GetDirectoryName(binDir);
                string logDir = Path.Combine(baseDir, "log");
                Directory.CreateDirectory(logDir);

                string today = DateTime.Now.ToString("yyyy-MM-dd");
                string logFilePath = Path.Combine(logDir, string.Format("SpotifyKnob_{0}.log", today));
                _logWriter = new StreamWriter(new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
            }
            catch { }
        }

        public static void LogInfo(string msg)
        {
            string line = string.Format("[{0}] [info] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), msg);
            Console.WriteLine(line);
            lock (_logLock)
            {
                if (_logWriter != null)
                {
                    _logWriter.WriteLine(line);
                }
            }
        }

        public static void LogError(string msg)
        {
            string line = string.Format("[{0}] [error] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), msg);
            Console.WriteLine(line);
            lock (_logLock)
            {
                if (_logWriter != null)
                {
                    _logWriter.WriteLine(line);
                }
            }
        }
    }
}
