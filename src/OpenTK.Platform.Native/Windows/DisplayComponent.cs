﻿using OpenTK.Core.Platform;
using OpenTK.Core.Utility;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenTK.Platform.Native.Windows
{
    public class DisplayComponent : IDisplayComponent
    {
        public string Name => "Win32DisplayComponent";

        public PalComponents Provides => PalComponents.Display;

        public ILogger? Logger { get; set; }

        private static List<HMonitor> _displays = new List<HMonitor>();

        internal static HMonitor? FindMonitor(IntPtr hMonitor)
        {
            foreach (var display in _displays)
            {
                if (display.Monitor == hMonitor)
                {
                    return display;
                }
            }

            return null;
        }

        private static IntPtr FindMonitorHandle(string adapterName)
        {
            IntPtr monitorHandle = default;

            bool FindHandleCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref Win32.RECT lprcMonitor, IntPtr dwData)
            {
                Win32.MONITORINFOEX info = default;
                info.cbSize = (uint)Marshal.SizeOf<Win32.MONITORINFOEX>();
                if (Win32.GetMonitorInfo(hMonitor, ref info))
                {
                    if (adapterName == info.szDevice)
                    {
                        monitorHandle = hMonitor;
                    }
                }

                return true;
            }

            bool success = Win32.EnumDisplayMonitors(IntPtr.Zero, in Unsafe.NullRef<Win32.RECT>(), FindHandleCallback, IntPtr.Zero);
            if (success == false)
            {
                throw new Exception("EnumDisplayMonitors failed.");
            }

            return monitorHandle;
        }

        internal static void UpdateMonitors()
        {
            List<HMonitor> newDisplays = new List<HMonitor>();

            // Create a list with all current displays,
            // we will remove from this list as we enumerate displays.
            List<HMonitor> removedDisplays = new List<HMonitor>(_displays);

            HMonitor? oldPrimary = _displays.Find(h => h.IsPrimary);

            const uint ENUM_CURRENT_SETTINGS = unchecked((uint)-1);

            Win32.DISPLAY_DEVICE adapter = default;
            adapter.cb = (uint)Marshal.SizeOf<Win32.DISPLAY_DEVICE>();
            const int EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001;
            for (uint index = 0; Win32.EnumDisplayDevices(null, index, ref adapter, EDD_GET_DEVICE_INTERFACE_NAME); index++)
            {
                if (adapter.StateFlags.HasFlag(DisplayDeviceStateFlags.Active) == false)
                {
                    continue;
                }

                // Console.WriteLine($"Adapter Name: {adapter.DeviceName}");
                // Console.WriteLine($"Adapter String: {adapter.DeviceString}");
                // Console.WriteLine($"Adapter State flags: {adapter.StateFlags}");

                // Create/Update the cache of display settigns data
                Win32.DEVMODE lpDevMode = default;
                lpDevMode.dmSize = (ushort)Marshal.SizeOf<Win32.DEVMODE>();
                Win32.EnumDisplaySettings(adapter.DeviceName, 0, ref lpDevMode);

                lpDevMode = default;
                lpDevMode.dmSize = (ushort)Marshal.SizeOf<Win32.DEVMODE>();
                if (Win32.EnumDisplaySettings(adapter.DeviceName, ENUM_CURRENT_SETTINGS, ref lpDevMode))
                {
                    // Console.WriteLine("Current settings:");
                    // Console.WriteLine($"  DeviceName: {lpDevMode.dmDeviceName}");
                    // Console.WriteLine($"  Position: {lpDevMode.dmPosition}");
                    // Console.WriteLine($"  BitsPerPel: {lpDevMode.dmBitsPerPel}");
                    // Console.WriteLine($"  PelsWidth: {lpDevMode.dmPelsWidth}");
                    // Console.WriteLine($"  PelsHeight: {lpDevMode.dmPelsHeight}");
                    // Console.WriteLine($"  DisplayFrequency: {lpDevMode.dmDisplayFrequency}");
                    // Console.WriteLine($"  DisplayFlags: {lpDevMode.dmDisplayFlags}");
                    // Console.WriteLine("  " + lpDevMode.dmFields);
                }

                IntPtr hMonitor = FindMonitorHandle(adapter.DeviceName);

                Win32.RECT workArea = default;

                Win32.MONITORINFOEX monitorInfo = default;
                monitorInfo.cbSize = (uint)Marshal.SizeOf<Win32.MONITORINFOEX>();
                if (Win32.GetMonitorInfo(hMonitor, ref monitorInfo))
                {
                    workArea = monitorInfo.rcWork;
                }

                Win32.DISPLAY_DEVICE monitor = default;
                monitor.cb = (uint)Marshal.SizeOf<Win32.DISPLAY_DEVICE>();

                for (uint monitorIndex = 0; Win32.EnumDisplayDevices(adapter.DeviceName, monitorIndex, ref monitor, 0); monitorIndex++)
                {
                    // Console.WriteLine($"  Monitor: {monitor.DeviceName}");
                    // Console.WriteLine($"  Monitor String: {monitor.DeviceString}");
                    // Console.WriteLine($"  Monitor State Flags: {monitor.StateFlags}");

                    HMonitor? info = null;
                    foreach (var display in _displays)
                    {
                        if (monitor.DeviceName == display.Name)
                        {
                            // This monitor already exists.
                            removedDisplays.Remove(display);
                            info = display;
                            break;
                        }
                    }

                    if (info == null)
                    {
                        // This display didn't exist before, which means it's now conncted.
                        info = new HMonitor()
                        {
                            Monitor = hMonitor,
                            Name = monitor.DeviceName,
                            AdapterName = adapter.DeviceName,
                            PublicName = monitor.DeviceString,
                            IsPrimary = adapter.StateFlags.HasFlag(DisplayDeviceStateFlags.PrimaryDevice),
                            Position = lpDevMode.dmPosition,
                            RefreshRate = (int)lpDevMode.dmDisplayFrequency,
                            Resolution = new DisplayResolution((int)lpDevMode.dmPelsWidth, (int)lpDevMode.dmPelsHeight),
                            DpiX = -1,
                            DpiY = -1,
                            WorkArea = workArea,
                        };

                        newDisplays.Add(info);
                    }
                    else
                    {
                        info.Monitor = hMonitor;

                        Debug.Assert(info.Name == monitor.DeviceName);
                        Debug.Assert(info.PublicName == monitor.DeviceString);

                        info.IsPrimary = adapter.StateFlags.HasFlag(DisplayDeviceStateFlags.PrimaryDevice);

                        info.Position = lpDevMode.dmPosition;
                        info.RefreshRate = (int)lpDevMode.dmDisplayFrequency;
                        info.Resolution = new DisplayResolution((int)lpDevMode.dmPelsWidth, (int)lpDevMode.dmPelsHeight);
                        info.WorkArea = workArea;
                    }

                    monitorIndex++;
                }
            }

            // Console.WriteLine();
            // Console.WriteLine();

            // FIXME: Maybe we should just send all of the data at once to the user.

            foreach (var removed in removedDisplays)
            {
                _displays.Remove(removed);

                // FIXME: Add event!
                // EventQueue.Raise(removed, PlatformEventType.MonitorRemoved, null);
                Console.WriteLine($"Removed: {removed.Name} (WasPrimary: {removed.IsPrimary}, Refresh: {removed.RefreshRate}, Res: {removed.Resolution})");
            }

            foreach (var connected in newDisplays)
            {
                _displays.Add(connected);

                // FIXME: Add event!
                // EventQueue.Raise(connected, PlatformEventType.MonitorConnected, null)
                Console.WriteLine($"Connected: {connected.Name} (IsPrimary: {connected.IsPrimary}, Refresh: {connected.RefreshRate}, Res: {connected.Resolution})");
            }

            HMonitor? primary = null;
            foreach (var display in _displays)
            {
                if (display.IsPrimary)
                {
                    primary = display;
                    break;
                }
            }

            // Place the primary monitor at the beginning of the list.
            if (primary != null)
            {
                int index = _displays.IndexOf(primary);
                _displays.RemoveAt(index);
                _displays.Insert(0, primary);

                if (primary != oldPrimary)
                {
                    Console.WriteLine("New primary monitor!");
                }
            }
            else
            {
                Console.WriteLine("Could not find primary monitor!");
            }
        }

        public void Initialize(PalComponents which)
        {
            if (which != PalComponents.Display)
            {
                throw new Exception("DisplayComponent can only initialize the Display component.");
            }

            // FIXME: Make the DPI awareness a user-set property!

            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0))
            {
                bool success = Win32.SetProcessDpiAwarenessContext(new IntPtr((int)DpiAwarenessContext.PerMonitorAwareV2));
                if (success == false)
                {
                    throw new Win32Exception();
                }
            }
            else if (OperatingSystem.IsWindowsVersionAtLeast(6, 3)) // Windows 8.1
            {
                // FIXME: Figure out what kind of awareness
                int result = Win32.SetProcessDpiAwareness(ProcessDPIAwareness.PerMonitorDpiAware);
                if (result == Win32.E_INVALIDARG)
                {
                    throw new Exception("SetProcessDpiAwareness failed with E_INVALIDARG");
                }
                else if (result == Win32.E_ACCESSDENIED)
                {
                    throw new Exception("SetProcessDpiAwareness failed with E_ACCESSDENIED");
                }
                else if (result != Win32.S_OK)
                {
                    throw new Exception($"SetProcessDpiAwareness failed with unknown error {result}");
                }
            }
            else
            {
                // Windows vista function for setting the process as DPI aware
                // Equivalent to `SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_SYSTEM_AWARE)`
                bool success = Win32.SetProcessDPIAware();
                if (success == false)
                {
                    throw new Exception("SetProcessDPIAware failed.");
                }
            }

            UpdateMonitors();
        }

        public bool CanSetVideoMode => throw new NotImplementedException();

        public bool CanGetVirtualPosition => true;

        public int GetDisplayCount()
        {
            int count = Win32.GetSystemMetrics(SystemMetric.CMonitors);
            if (count == 0)
            {
                throw new Exception("GetSystemMetrics(SM_CMONITOR) failed.");
            }

            return count;
        }

        // FIXME: Indices for monitors is ill defined.
        // Need to look more into documentation for the monitor API.

        // FIXME: You probably shouldn't "create" a monitor handle.
        public DisplayHandle Create(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), $"Monitor index cannot be negative. {index}");
            if (index >= _displays.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Monitor index cannot be larger or equal to the number of displays. Index: {index}, Display count: {_displays.Count}");

            return _displays[index];
        }

        public DisplayHandle CreatePrimary()
        {
            for (int i = 0; i < _displays.Count; i++)
            {
                if (_displays[i].IsPrimary)
                {
                    return _displays[i];
                }
            }

            return null;
        }

        // FIXME: You probably also don't Destroy a monitor
        public void Destroy(DisplayHandle handle)
        {
            // We basically don't need to do anything here.
            // Just check that we got the right kind of handle back.
            HMonitor hmonitor = handle.As<HMonitor>(this);
        }

        public bool IsPrimary(DisplayHandle handle)
        {
            HMonitor hmonitor = handle.As<HMonitor>(this);

            return hmonitor.IsPrimary;
        }

        public string GetName(DisplayHandle handle)
        {
            HMonitor hmonitor = handle.As<HMonitor>(this);

            return hmonitor.PublicName;
        }

        public void GetVideoMode(DisplayHandle handle, out VideoMode mode)
        {
            HMonitor hmonitor = handle.As<HMonitor>(this);

            GetDisplayScale(handle, out float scaleX, out float scaleY);

            // FIXME: DPI
            mode = new VideoMode(
                hmonitor.Resolution.ResolutionX,
                hmonitor.Resolution.ResolutionY,
                hmonitor.RefreshRate,
                scaleX,
                96);
        }

        public void SetVideoMode(DisplayHandle handle, in VideoMode mode)
        {
            throw new NotImplementedException();
        }

        public int GetSupportedVideoModeCount(DisplayHandle handle)
        {
            HMonitor hmonitor = handle.As<HMonitor>(this);

            // FIXME: Calling this function with 0 rebuilds the cache, which means that between
            // a call to GetSupportedVideoModeCount and GetSupportedVideoModes the cache could have changed.
            // This is not great... would be good if we could combine the calls into one.
            int modeIndex = 0;
            Win32.DEVMODE lpDevMode = default;
            lpDevMode.dmSize = (ushort)Marshal.SizeOf<Win32.DEVMODE>();
            while (Win32.EnumDisplaySettings(hmonitor.AdapterName, (uint)modeIndex++, ref lpDevMode))
            {
            }

            return modeIndex - 1;
        }

        public void GetSupportedVideoModes(DisplayHandle handle, Span<VideoMode> modes)
        {
            HMonitor hmonitor = handle.As<HMonitor>(this);

            // FIXME: Should the scale really be part of the video mode?
            // Is it something that can be set independently of video mode?
            GetDisplayScale(handle, out float scaleX, out float scaleY);

            int modeIndex = 0;
            Win32.DEVMODE lpDevMode = default;
            lpDevMode.dmSize = (ushort)Marshal.SizeOf<Win32.DEVMODE>();
            while (Win32.EnumDisplaySettings(hmonitor.AdapterName, (uint)modeIndex++, ref lpDevMode))
            {
                // FIXME: What do we do with duplicated video modes?
                // For now we keep them, but there is no possibility of
                // differentiating them.
                // Should we decide or should we pass platform specific info to the user?

                const DM RequiredFields = DM.PelsWidth | DM.PelsHeight | DM.DisplayFrequency;

                if ((lpDevMode.dmFields & RequiredFields) != RequiredFields)
                    throw new PalException(this, $"Adapter setting {modeIndex - 1} didn't have all required fields set. dmFields={lpDevMode.dmFields}, requiredFields={RequiredFields}");

                // FIXME: Scale and DPI
                modes[modeIndex - 1] = new VideoMode(
                    (int)lpDevMode.dmPelsWidth,
                    (int)lpDevMode.dmPelsHeight,
                    lpDevMode.dmDisplayFrequency,
                    scaleX,
                    96);
            }
        }

        public void GetVirtualPosition(DisplayHandle handle, out int x, out int y)
        {
            HMonitor hmonitor = handle.As<HMonitor>(this);

            x = hmonitor.Position.X;
            y = hmonitor.Position.Y;
        }

        public void GetResolution(DisplayHandle handle, out int width, out int height)
        {
            HMonitor hmonitor = handle.As<HMonitor>(this);

            width = hmonitor.Resolution.ResolutionX;
            height = hmonitor.Resolution.ResolutionY;
        }

        public void GetWorkArea(DisplayHandle handle, out Box2i area)
        {
            HMonitor hmonitor = handle.As<HMonitor>(this);

            Win32.RECT workArea = hmonitor.WorkArea;

            area = new Box2i(workArea.left, workArea.top, workArea.right, workArea.bottom);
        }

        public void GetRefreshRate(DisplayHandle handle, out float refreshRate)
        {
            HMonitor hmonitor = handle.As<HMonitor>(this);

            refreshRate = hmonitor.RefreshRate;
        }

        public void GetDisplayScale(DisplayHandle handle, out float  scaleX, out float scaleY)
        {
            HMonitor hmonitor = handle.As<HMonitor>(this);

            int success = Win32.GetDpiForMonitor(hmonitor.Monitor, MonitorDpiType.EffectiveDpi, out uint dpiX, out uint dpiY);
            if (success != Win32.S_OK)
            {
                throw new Exception("GetDpiForMonitor failed.");
            }

            // This is the platform default DPI for windows.
            const float DefaultDPI = 96;

            scaleX = dpiX / DefaultDPI;
            scaleY = dpiY / DefaultDPI;
        }
    }
}