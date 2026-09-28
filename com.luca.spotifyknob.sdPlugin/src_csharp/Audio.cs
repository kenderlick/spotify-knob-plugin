using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SpotifyKnob
{
    public static class AudioController
    {
        private enum EDataFlow
        {
            eRender = 0,
            eCapture = 1,
            eAll = 2
        }

        private enum ERole
        {
            eConsole = 0,
            eMultimedia = 1,
            eCommunications = 2
        }

        private const int CLSCTX_INPROC_SERVER = 0x1;

        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumeratorComObject { }

        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            [PreserveSig] int EnumAudioEndpoints(EDataFlow dataFlow, int dwStateMask, out IntPtr ppDevices);
            [PreserveSig] int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);
            [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);
            [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr pClient);
            [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr pClient);
        }

        [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [PreserveSig] int Activate([In] ref Guid iid, int dwClsCtx, IntPtr pActivationParams, out IntPtr ppInterface);
            [PreserveSig] int OpenPropertyStore(int stgmAccess, out IntPtr ppProperties);
            [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
            [PreserveSig] int GetState(out int pdwState);
        }

        [Guid("5BC64874-38B4-4D76-A4E8-7F11A4F44CEC"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioEndpointVolume
        {
            [PreserveSig] int RegisterControlChangeNotify(IntPtr pNotify);
            [PreserveSig] int UnregisterControlChangeNotify(IntPtr pNotify);
            [PreserveSig] int GetChannelCount(out uint pnChannelCount);
            [PreserveSig] int SetMasterVolumeLevel(float fLevelDB, [In] ref Guid pguidEventContext);
            [PreserveSig] int SetMasterVolumeLevelScalar(float fLevel, [In] ref Guid pguidEventContext);
            [PreserveSig] int GetMasterVolumeLevel(out float pfLevelDB);
            [PreserveSig] int GetMasterVolumeLevelScalar(out float pfLevel);
            [PreserveSig] int SetChannelVolumeLevel(uint nChannel, float fLevelDB, [In] ref Guid pguidEventContext);
            [PreserveSig] int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, [In] ref Guid pguidEventContext);
            [PreserveSig] int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
            [PreserveSig] int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
            [PreserveSig] int SetMute(bool bMute, [In] ref Guid pguidEventContext);
            [PreserveSig] int GetMute(out bool pbMute);
            [PreserveSig] int GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);
            [PreserveSig] int VolumeStepUp([In] ref Guid pguidEventContext);
            [PreserveSig] int VolumeStepDown([In] ref Guid pguidEventContext);
            [PreserveSig] int QueryHardwareSupport(out uint pdwHardwareSupportMask);
            [PreserveSig] int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
        }

        // COM delegates for direct VTable calls
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetSessionEnumeratorDelegate(IntPtr thisPtr, out IntPtr sessionEnum);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetCountDelegate(IntPtr thisPtr, out int count);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetSessionDelegate(IntPtr thisPtr, int index, out IntPtr session);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetProcessIdDelegate(IntPtr thisPtr, out uint pid);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetMasterVolumeDelegate(IntPtr thisPtr, out float level);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetMasterVolumeDelegate(IntPtr thisPtr, float level, [In] ref Guid eventContext);

        private static Guid IID_IAudioSessionManager2 = new Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F");
        private static Guid IID_IAudioSessionControl2 = new Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D");
        private static Guid IID_ISimpleAudioVolume = new Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8");
        private static Guid IID_IAudioEndpointVolume = new Guid("5BC64874-38B4-4D76-A4E8-7F11A4F44CEC");

        public static int AdjustSpotifyVolume(float delta)
        {
            var pids = SpotifyController.GetSpotifyProcessIds();
            if (pids.Count == 0)
            {
                return -1;
            }

            var pidSet = new HashSet<int>(pids);
            int resultVol = -1;

            try
            {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                IMMDevice device;
                enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out device);
                if (device == null)
                {
                    enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, out device);
                }
                if (device != null)
                {
                    IntPtr sessionManagerPtr;
                    if (device.Activate(ref IID_IAudioSessionManager2, CLSCTX_INPROC_SERVER, IntPtr.Zero, out sessionManagerPtr) == 0 && sessionManagerPtr != IntPtr.Zero)
                    {
                        // IAudioSessionManager2::GetSessionEnumerator is at vtable slot 5 (0-2: IUnknown, 3-4: IAudioSessionManager, 5: GetSessionEnumerator)
                        IntPtr smVtbl = Marshal.ReadIntPtr(sessionManagerPtr);
                        IntPtr getEnumPtr = Marshal.ReadIntPtr(smVtbl, 5 * IntPtr.Size);
                        var getSessionEnum = (GetSessionEnumeratorDelegate)Marshal.GetDelegateForFunctionPointer(getEnumPtr, typeof(GetSessionEnumeratorDelegate));

                        IntPtr sessionEnumPtr;
                        if (getSessionEnum(sessionManagerPtr, out sessionEnumPtr) == 0 && sessionEnumPtr != IntPtr.Zero)
                        {
                            IntPtr enumVtbl = Marshal.ReadIntPtr(sessionEnumPtr);
                            IntPtr getCountPtr = Marshal.ReadIntPtr(enumVtbl, 3 * IntPtr.Size);
                            IntPtr getSessionPtr = Marshal.ReadIntPtr(enumVtbl, 4 * IntPtr.Size);

                            var getCount = (GetCountDelegate)Marshal.GetDelegateForFunctionPointer(getCountPtr, typeof(GetCountDelegate));
                            var getSession = (GetSessionDelegate)Marshal.GetDelegateForFunctionPointer(getSessionPtr, typeof(GetSessionDelegate));

                            int count;
                            if (getCount(sessionEnumPtr, out count) == 0 && count > 0)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    IntPtr sessionControlPtr;
                                    if (getSession(sessionEnumPtr, i, out sessionControlPtr) == 0 && sessionControlPtr != IntPtr.Zero)
                                    {
                                        IntPtr control2Ptr;
                                        Marshal.QueryInterface(sessionControlPtr, ref IID_IAudioSessionControl2, out control2Ptr);
                                        if (control2Ptr != IntPtr.Zero)
                                        {
                                            // IAudioSessionControl2::GetProcessId is at vtable slot 14
                                            IntPtr c2Vtbl = Marshal.ReadIntPtr(control2Ptr);
                                            IntPtr getPidPtr = Marshal.ReadIntPtr(c2Vtbl, 14 * IntPtr.Size);
                                            var getPid = (GetProcessIdDelegate)Marshal.GetDelegateForFunctionPointer(getPidPtr, typeof(GetProcessIdDelegate));

                                            uint pid;
                                            if (getPid(control2Ptr, out pid) == 0 && pidSet.Contains((int)pid))
                                            {
                                                IntPtr volumePtr;
                                                Marshal.QueryInterface(sessionControlPtr, ref IID_ISimpleAudioVolume, out volumePtr);
                                                if (volumePtr != IntPtr.Zero)
                                                {
                                                    // ISimpleAudioVolume: SetMasterVolume (slot 3), GetMasterVolume (slot 4)
                                                    IntPtr volVtbl = Marshal.ReadIntPtr(volumePtr);
                                                    IntPtr setVolPtr = Marshal.ReadIntPtr(volVtbl, 3 * IntPtr.Size);
                                                    IntPtr getVolPtr = Marshal.ReadIntPtr(volVtbl, 4 * IntPtr.Size);

                                                    var getVol = (GetMasterVolumeDelegate)Marshal.GetDelegateForFunctionPointer(getVolPtr, typeof(GetMasterVolumeDelegate));
                                                    var setVol = (SetMasterVolumeDelegate)Marshal.GetDelegateForFunctionPointer(setVolPtr, typeof(SetMasterVolumeDelegate));

                                                    float curVol;
                                                    if (getVol(volumePtr, out curVol) == 0)
                                                    {
                                                        float targetVol = Math.Max(0.0f, Math.Min(1.0f, curVol + delta));
                                                        Guid emptyGuid = Guid.Empty;
                                                        if (setVol(volumePtr, targetVol, ref emptyGuid) == 0)
                                                        {
                                                            resultVol = (int)Math.Round(targetVol * 100.0f);
                                                            Program.LogInfo(string.Format("Spotify WASAPI volume set: {0:F0}% -> {1}%", curVol * 100, resultVol));
                                                        }
                                                    }
                                                    Marshal.Release(volumePtr);
                                                }
                                            }
                                            Marshal.Release(control2Ptr);
                                        }
                                        Marshal.Release(sessionControlPtr);
                                    }
                                }
                            }
                            Marshal.Release(sessionEnumPtr);
                        }
                        Marshal.Release(sessionManagerPtr);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogError("AdjustSpotifyVolume WASAPI error: " + ex.Message);
            }

            return resultVol;
        }

        public static int GetSpotifyVolume()
        {
            var pids = SpotifyController.GetSpotifyProcessIds();
            if (pids.Count == 0) return -1;

            var pidSet = new HashSet<int>(pids);
            try
            {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                IMMDevice device;
                enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out device);
                if (device == null)
                {
                    enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, out device);
                }
                if (device != null)
                {
                    IntPtr sessionManagerPtr;
                    if (device.Activate(ref IID_IAudioSessionManager2, CLSCTX_INPROC_SERVER, IntPtr.Zero, out sessionManagerPtr) == 0 && sessionManagerPtr != IntPtr.Zero)
                    {
                        IntPtr smVtbl = Marshal.ReadIntPtr(sessionManagerPtr);
                        IntPtr getEnumPtr = Marshal.ReadIntPtr(smVtbl, 5 * IntPtr.Size);
                        var getSessionEnum = (GetSessionEnumeratorDelegate)Marshal.GetDelegateForFunctionPointer(getEnumPtr, typeof(GetSessionEnumeratorDelegate));

                        IntPtr sessionEnumPtr;
                        if (getSessionEnum(sessionManagerPtr, out sessionEnumPtr) == 0 && sessionEnumPtr != IntPtr.Zero)
                        {
                            IntPtr enumVtbl = Marshal.ReadIntPtr(sessionEnumPtr);
                            IntPtr getCountPtr = Marshal.ReadIntPtr(enumVtbl, 3 * IntPtr.Size);
                            IntPtr getSessionPtr = Marshal.ReadIntPtr(enumVtbl, 4 * IntPtr.Size);

                            var getCount = (GetCountDelegate)Marshal.GetDelegateForFunctionPointer(getCountPtr, typeof(GetCountDelegate));
                            var getSession = (GetSessionDelegate)Marshal.GetDelegateForFunctionPointer(getSessionPtr, typeof(GetSessionDelegate));

                            int count;
                            if (getCount(sessionEnumPtr, out count) == 0 && count > 0)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    IntPtr sessionControlPtr;
                                    if (getSession(sessionEnumPtr, i, out sessionControlPtr) == 0 && sessionControlPtr != IntPtr.Zero)
                                    {
                                        IntPtr control2Ptr;
                                        Marshal.QueryInterface(sessionControlPtr, ref IID_IAudioSessionControl2, out control2Ptr);
                                        if (control2Ptr != IntPtr.Zero)
                                        {
                                            IntPtr c2Vtbl = Marshal.ReadIntPtr(control2Ptr);
                                            IntPtr getPidPtr = Marshal.ReadIntPtr(c2Vtbl, 14 * IntPtr.Size);
                                            var getPid = (GetProcessIdDelegate)Marshal.GetDelegateForFunctionPointer(getPidPtr, typeof(GetProcessIdDelegate));

                                            uint pid;
                                            if (getPid(control2Ptr, out pid) == 0 && pidSet.Contains((int)pid))
                                            {
                                                IntPtr volumePtr;
                                                Marshal.QueryInterface(sessionControlPtr, ref IID_ISimpleAudioVolume, out volumePtr);
                                                if (volumePtr != IntPtr.Zero)
                                                {
                                                    IntPtr volVtbl = Marshal.ReadIntPtr(volumePtr);
                                                    IntPtr getVolPtr = Marshal.ReadIntPtr(volVtbl, 4 * IntPtr.Size);
                                                    var getVol = (GetMasterVolumeDelegate)Marshal.GetDelegateForFunctionPointer(getVolPtr, typeof(GetMasterVolumeDelegate));

                                                    float curVol;
                                                    if (getVol(volumePtr, out curVol) == 0)
                                                    {
                                                        Marshal.Release(volumePtr);
                                                        Marshal.Release(control2Ptr);
                                                        Marshal.Release(sessionControlPtr);
                                                        Marshal.Release(sessionEnumPtr);
                                                        Marshal.Release(sessionManagerPtr);
                                                        return (int)Math.Round(curVol * 100.0f);
                                                    }
                                                    Marshal.Release(volumePtr);
                                                }
                                            }
                                            Marshal.Release(control2Ptr);
                                        }
                                        Marshal.Release(sessionControlPtr);
                                    }
                                }
                            }
                            Marshal.Release(sessionEnumPtr);
                        }
                        Marshal.Release(sessionManagerPtr);
                    }
                }
            }
            catch { }
            return -1;
        }

        public static int AdjustMasterVolume(float delta)
        {
            try
            {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                IMMDevice device;
                enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out device);
                if (device == null)
                {
                    enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, out device);
                }
                if (device != null)
                {
                    IntPtr endpointVolPtr;
                    if (device.Activate(ref IID_IAudioEndpointVolume, CLSCTX_INPROC_SERVER, IntPtr.Zero, out endpointVolPtr) == 0 && endpointVolPtr != IntPtr.Zero)
                    {
                        var endpointVol = (IAudioEndpointVolume)Marshal.GetObjectForIUnknown(endpointVolPtr);
                        if (endpointVol != null)
                        {
                            float curVol;
                            endpointVol.GetMasterVolumeLevelScalar(out curVol);
                            float targetVol = Math.Max(0.0f, Math.Min(1.0f, curVol + delta));
                            Guid emptyGuid = Guid.Empty;
                            endpointVol.SetMasterVolumeLevelScalar(targetVol, ref emptyGuid);
                            int newVol = (int)Math.Round(targetVol * 100.0f);
                            Program.LogInfo(string.Format("Master volume set: {0:F0}% -> {1}%", curVol * 100, newVol));
                            Marshal.Release(endpointVolPtr);
                            return newVol;
                        }
                        Marshal.Release(endpointVolPtr);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogError("AdjustMasterVolume error: " + ex.Message);
            }
            return -1;
        }
    }
}
