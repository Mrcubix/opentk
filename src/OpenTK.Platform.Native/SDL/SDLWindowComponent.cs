﻿using OpenTK.Core.Platform;
using OpenTK.Core.Utility;
using OpenTK.Mathematics;
using OpenTK.Platform.Native.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static OpenTK.Platform.Native.SDL.SDL;

namespace OpenTK.Platform.Native.SDL
{
    public class SDLWindowComponent : IWindowComponent
    {
        internal static readonly Dictionary<uint, SDLWindow> WindowDict = new Dictionary<uint, SDLWindow>();

        /// <inheritdoc/>
        public string Name => nameof(SDLWindowComponent);

        /// <inheritdoc/>
        public PalComponents Provides => PalComponents.Window;

        /// <inheritdoc/>
        public ILogger? Logger { get; set; }

        /// <inheritdoc/>
        public void Initialize(PalComponents which)
        {
            if (which != PalComponents.Window)
            {
                throw new PalException(this, "SDLWindowComponent can only initialize the Window component.");
            }

            // Load SDLLib
            int result = SDL_Init(SDL_INIT.SDL_INIT_VIDEO | SDL_INIT.SDL_INIT_EVENTS);

            if (result < 0)
            {
                string error = SDL_GetError();
                throw new PalException(this, $"SDL Error: {error}");
            }
        }

        /// <inheritdoc/>
        public bool CanSetIcon => true;

        /// <inheritdoc/>
        public bool CanGetDisplay => true;

        /// <inheritdoc/>
        public bool CanSetCursor => throw new NotImplementedException();

        /// <inheritdoc/>
        public bool CanCaptureCursor => throw new NotImplementedException();

        /// <inheritdoc/>
        public IReadOnlyList<PlatformEventType> SupportedEvents => throw new NotImplementedException();

        /// <inheritdoc/>
        public IReadOnlyList<WindowStyle> SupportedStyles => throw new NotImplementedException();

        /// <inheritdoc/>
        public IReadOnlyList<WindowMode> SupportedModes => throw new NotImplementedException();

        private List<string> drops = new List<string>();

        /// <inheritdoc/>
        public unsafe void ProcessEvents(bool waitForEvents = false)
        {
            SDLEvent @event;
            int result = SDL_PollEvent(&@event);

            if (result == 1)
            {
                switch (@event.Type)
                {
                    case SDL_EventType.SDL_QUIT:
                        // FIXME: Do we need to do anything here?
                        break;
                    case SDL_EventType.SDL_WINDOWEVENT:
                        {
                            SDL_WindowEvent windowEvent = @event.Window;
                            SDLWindow sdlWindow = WindowDict[windowEvent.windowID];

                            switch (windowEvent.@event)
                            {
                                case SDL_WindowEventID.SDL_WINDOWEVENT_SHOWN:
                                    {
                                        // FIXME: What do we do here? Do we check for a change in window mode?
                                        // Do we just always send a changed event?
                                    }
                                    break;
                                case SDL_WindowEventID.SDL_WINDOWEVENT_HIDDEN:
                                    {
                                        EventQueue.Raise(sdlWindow, PlatformEventType.WindowModeChange, new WindowModeChangeEventArgs(sdlWindow, WindowMode.Hidden));

                                        break;
                                    }
                                case SDL_WindowEventID.SDL_WINDOWEVENT_EXPOSED:
                                    break;
                                case SDL_WindowEventID.SDL_WINDOWEVENT_MOVED:
                                    {
                                        Vector2i newPosition = new Vector2i(windowEvent.data1, windowEvent.data2);

                                        // FIXME: Client area position!
                                        EventQueue.Raise(sdlWindow, PlatformEventType.WindowMove, new WindowMoveEventArgs(sdlWindow, newPosition, default));

                                        break;
                                    }
                                case SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED:
                                    {
                                        Vector2i newSize = new Vector2i(windowEvent.data1, windowEvent.data2);

                                        // FIXME: Client area position!
                                        EventQueue.Raise(sdlWindow, PlatformEventType.WindowResize, new WindowResizeEventArgs(sdlWindow, newSize));

                                        break;
                                    }
                                case SDL_WindowEventID.SDL_WINDOWEVENT_MINIMIZED:
                                    {
                                        EventQueue.Raise(sdlWindow, PlatformEventType.WindowModeChange, new WindowModeChangeEventArgs(sdlWindow, WindowMode.Minimized));
                                        break;
                                    }
                                case SDL_WindowEventID.SDL_WINDOWEVENT_MAXIMIZED:
                                    {
                                        EventQueue.Raise(sdlWindow, PlatformEventType.WindowModeChange, new WindowModeChangeEventArgs(sdlWindow, WindowMode.Maximized));
                                        break;
                                    }
                                case SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED:
                                    {
                                        EventQueue.Raise(sdlWindow, PlatformEventType.WindowModeChange, new WindowModeChangeEventArgs(sdlWindow, WindowMode.Normal));
                                        break;
                                    }
                                case SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                                    {
                                        EventQueue.Raise(sdlWindow, PlatformEventType.MouseEnter, new MouseEnterEventArgs(sdlWindow, true));
                                        break;
                                    }
                                case SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                                    {
                                        EventQueue.Raise(sdlWindow, PlatformEventType.MouseEnter, new MouseEnterEventArgs(sdlWindow, false));
                                        break;
                                    }
                                case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
                                    {
                                        EventQueue.Raise(sdlWindow, PlatformEventType.Focus, new FocusEventArgs(sdlWindow, true));
                                        break;
                                    }
                                case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                                    {
                                        EventQueue.Raise(sdlWindow, PlatformEventType.Focus, new FocusEventArgs(sdlWindow, false));
                                        break;
                                    }
                                case SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                                    {
                                        EventQueue.Raise(sdlWindow, PlatformEventType.Close, new CloseEventArgs(sdlWindow));

                                        break;
                                    }
                                case SDL_WindowEventID.SDL_WINDOWEVENT_TAKE_FOCUS:
                                    {
                                        // FIXME: Might be an interesting event to expose
                                        // See https://github.com/libsdl-org/SDL/commit/dc532c70e8086030b67794c62bc41922c3d5386c
                                        break;
                                    }
                                case SDL_WindowEventID.SDL_WINDOWEVENT_HIT_TEST:
                                    {
                                        break;
                                    }
                                case SDL_WindowEventID.SDL_WINDOWEVENT_ICCPROF_CHANGED:
                                    {
                                        // TODO: This would be great event to add to the PAL2 api.
                                        break;
                                    }
                                case SDL_WindowEventID.SDL_WINDOWEVENT_DISPLAY_CHANGED:
                                    {
                                        // FIXME: Expose an event like this.
                                        break;
                                    }
                                default:
                                    break;
                            }
                            break;
                        }
                    case SDL_EventType.SDL_MOUSEMOTION:
                        {
                            SDL_MouseMotionEvent mouseMotion = @event.MouseMotion;
                            SDLWindow sdlWindow = WindowDict[mouseMotion.windowID];

                            EventQueue.Raise(sdlWindow, PlatformEventType.MouseMove, new MouseMoveEventArgs(sdlWindow, new Vector2(mouseMotion.x, mouseMotion.y)));

                            break;
                        }
                    case SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    case SDL_EventType.SDL_MOUSEBUTTONUP:
                        {
                            SDL_MouseButtonEvent buttonEvent = @event.MouseButton;

                            SDLWindow sdlWindow = WindowDict[buttonEvent.windowID];

                            MouseButton button = buttonEvent.button switch
                            {
                                SDL_BUTTON.SDL_BUTTON_LEFT => MouseButton.Button1,
                                SDL_BUTTON.SDL_BUTTON_MIDDLE => MouseButton.Button3,
                                SDL_BUTTON.SDL_BUTTON_RIGHT => MouseButton.Button2,
                                SDL_BUTTON.SDL_BUTTON_X1 => MouseButton.Button4,
                                SDL_BUTTON.SDL_BUTTON_X2 => MouseButton.Button5,

                                // FIXME: Maybe don't throw an error here...
                                _ => throw new PalException(this, $"Got unknown mouse button: {buttonEvent.which}"),
                            };

                            if (buttonEvent.type == SDL_EventType.SDL_MOUSEBUTTONDOWN)
                            {
                                EventQueue.Raise(sdlWindow, PlatformEventType.MouseDown, new MouseButtonDownEventArgs(sdlWindow, button));
                            }
                            else
                            {
                                EventQueue.Raise(sdlWindow, PlatformEventType.MouseUp, new MouseButtonUpEventArgs(sdlWindow, button));
                            }

                            break;
                        }
                    case SDL_EventType.SDL_MOUSEWHEEL:
                        {
                            SDL_MouseWheelEvent mouseWheel = @event.MouseWheel;
                            SDLWindow sdlWindow = WindowDict[mouseWheel.windowID];

                            // FIXME: Account for SDL_MouseWheelEvent.direction
                            // we could also not abstract this and provide both scroll and mose wheel values in the event.

                            // FIXME: What are the different directions??
                            Vector2 scroll = new Vector2(mouseWheel.x, mouseWheel.y);
                            // FIXME: I don't think this is distance! We want to precisely determine what should be put in the distance field.
                            Vector2 distance = new Vector2(mouseWheel.preciseX, mouseWheel.preciseY);

                            EventQueue.Raise(sdlWindow, PlatformEventType.Scroll, new ScrollEventArgs(sdlWindow, scroll, distance));

                            break;
                        }
                    case SDL_EventType.SDL_CLIPBOARDUPDATE:
                        {
                            //SDLWindow sdlWindow = WindowDict[@event.windowID];
                            ClipboardFormat newFormat;
                            if (SDL_HasClipboardText() == 1) newFormat = ClipboardFormat.Text;
                            else newFormat = ClipboardFormat.None;

                            EventQueue.Raise(null, PlatformEventType.ClipboardUpdate, new ClipboardUpdateEventArgs(newFormat));

                            break;
                        }
                    case SDL_EventType.SDL_DROPBEGIN:
                    case SDL_EventType.SDL_DROPCOMPLETE:
                    case SDL_EventType.SDL_DROPFILE:
                        {
                            SDL_DropEvent dropEvent = @event.DropEvent;

                            if (@event.Type == SDL_EventType.SDL_DROPBEGIN)
                            {
                                drops.Clear();
                            }
                            else if (@event.Type == SDL_EventType.SDL_DROPFILE)
                            {
                                drops.Add(Marshal.PtrToStringUTF8((IntPtr)dropEvent.file)!);
                            }
                            else if (@event.Type == SDL_EventType.SDL_DROPCOMPLETE)
                            {
                                SDLWindow sdlWindow = WindowDict[dropEvent.windowID];

                                // FIXME: Should we use SDL_GetMouseState here instead and calculate the global position?
                                // The documentation says SDL_GetGlobalMouseState might be slower than SDL_GetMouseState.
                                SDL_GetGlobalMouseState(out int mouseX, out int mouseY);

                                EventQueue.Raise(sdlWindow, PlatformEventType.FileDrop, new FileDropEventArgs(sdlWindow, drops.ToList(), new Vector2i(mouseX, mouseY)));
                            }
                            break;
                        }
                    default:
                        Console.WriteLine($"SDL event type: {@event.Type}");
                        break;
                }
            }
        }

        /// <inheritdoc/>
        public WindowHandle Create(GraphicsApiHints hints)
        {
            OpenGLGraphicsApiHints settings = (OpenGLGraphicsApiHints)hints;

            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, settings.Version.Major);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, settings.Version.Minor);

            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DOUBLEBUFFER, settings.DoubleBuffer ? 1 : 0);

            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_RED_SIZE, settings.RedColorBits);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_GREEN_SIZE, settings.GreenColorBits);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_BLUE_SIZE, settings.BlueColorBits);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_ALPHA_SIZE, settings.AlphaColorBits);

            byte depthBits;
            switch (settings.DepthBits)
            {
                case ContextDepthBits.Depth24: depthBits = 24; break;
                case ContextDepthBits.Depth32: depthBits = 32; break;
                default: throw new InvalidEnumArgumentException(nameof(settings.DepthBits), (int)settings.DepthBits, settings.DepthBits.GetType());
            }

            byte stencilBits;
            switch (settings.StencilBits)
            {
                case ContextStencilBits.Stencil1: stencilBits = 1; break;
                case ContextStencilBits.Stencil8: stencilBits = 8; break;
                default: throw new InvalidEnumArgumentException(nameof(settings.StencilBits), (int)settings.StencilBits, settings.StencilBits.GetType());
            }

            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DEPTH_SIZE, depthBits);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_STENCIL_SIZE, stencilBits);

            if (settings.Multisamples > 0)
            {
                SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_MULTISAMPLEBUFFERS, 1);
                SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_MULTISAMPLESAMPLES, settings.Multisamples);
            }

            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_FRAMEBUFFER_SRGB_CAPABLE, settings.sRGBFramebuffer ? 1 : 0);

            // FIXME: Shared context, should we make the one that we want current and then set the old one again?
            // GetCurrentContext
            // SetCurrentContext
            //SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_SHARE_WITH_CURRENT_CONTEXT, 1);
            // SetCurrentContext

            SDL_GLprofile profile = 0;
            switch (settings.Profile)
            {
                case OpenGLProfile.None:
                    profile = 0;
                    break;
                case OpenGLProfile.Core:
                    profile = SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE;
                    break;
                case OpenGLProfile.Compatibility:
                    profile = SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_COMPATIBILITY;
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(settings.Profile), (int)settings.Profile, typeof(OpenGLProfile));
                    break;
            }
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_FLAGS, (int)profile);

            SDL_GLcontextFlag flags = 0;
            if (settings.DebugFlag) flags |= SDL_GLcontextFlag.SDL_GL_CONTEXT_DEBUG_FLAG;
            if (settings.ForwardCompatibleFlag) flags |= SDL_GLcontextFlag.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG;
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_FLAGS, (int)flags);

            SDL_WindowPtr window = SDL_CreateWindow("", SDL_WINDOWPOS_UNDEFINED, SDL_WINDOWPOS_UNDEFINED, 0, 0, SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL_WindowFlags.SDL_WINDOW_HIDDEN);

            uint id = SDL_GetWindowID(window);

            SDLWindow sdlWindow = new SDLWindow(window, id);

            WindowDict.Add(id, sdlWindow);

            return sdlWindow;
        }

        /// <inheritdoc/>
        public void Destroy(WindowHandle handle)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            WindowDict.Remove(window.WindowID);

            SDL_DestroyWindow(window.Window);

            window.Destroyed = true;
        }

        /// <inheritdoc/>
        public bool IsWindowDestroyed(WindowHandle handle)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            return window.Destroyed;
        }

        /// <inheritdoc/>
        public string GetTitle(WindowHandle handle)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            return SDL_GetWindowTitle(window.Window);
        }

        /// <inheritdoc/>
        public void SetTitle(WindowHandle handle, string title)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            SDL_SetWindowTitle(window.Window, title);
        }

        /// <inheritdoc/>
        public IconHandle GetIcon(WindowHandle handle)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            // FIXME: What is the default icon??
            if (window.Icon == null)
            {
                Logger?.LogWarning("Trying to read the default window icon. SDL 2 doesn't support this.");
                return new SDLIcon();
            }
            
            return window.Icon;
        }

        /// <inheritdoc/>
        public unsafe void SetIcon(WindowHandle handle, IconHandle icon)
        {
            SDLWindow window = handle.As<SDLWindow>(this);
            SDLIcon sdlIcon = icon.As<SDLIcon>(this);

            window.Icon = sdlIcon;
            SDL_SetWindowIcon(window.Window, sdlIcon.Surface);
        }

        /// <inheritdoc/>
        public void GetPosition(WindowHandle handle, out int x, out int y)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            SDL_GetWindowPosition(window.Window, out x, out y);
        }

        /// <inheritdoc/>
        public void SetPosition(WindowHandle handle, int x, int y)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            // FIXME: This sets the client position!!
            SDL_SetWindowPosition(window.Window, x, y);
        }

        /// <inheritdoc/>
        public void GetSize(WindowHandle handle, out int width, out int height)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            // FIXME: This sets the client position!!
            SDL_GetWindowSize(window.Window, out width, out height);
        }

        /// <inheritdoc/>
        public void SetSize(WindowHandle handle, int width, int height)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            SDL_SetWindowSize(window.Window, width, height);
        }

        /// <inheritdoc/>
        public void GetClientPosition(WindowHandle handle, out int x, out int y)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            SDL_GetWindowPosition(window.Window, out x, out y);
        }

        /// <inheritdoc/>
        public void SetClientPosition(WindowHandle handle, int x, int y)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            SDL_SetWindowPosition(window.Window, x, y);
        }

        /// <inheritdoc/>
        public void GetClientSize(WindowHandle handle, out int width, out int height)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void SetClientSize(WindowHandle handle, int width, int height)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void GetMaxClientSize(WindowHandle handle, out int? width, out int? height)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            int w, h;

            // FIXME: Is this client area or windowEvent??

            SDL_GetWindowMaximumSize(window.Window, out w, out h);

            width = w != 0 ? w : null;
            height = h != 0 ? h : null;
        }

        /// <inheritdoc/>
        public void SetMaxClientSize(WindowHandle handle, int? width, int? height)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            // FIXME: Is this client area or windowEvent??

            SDL_SetWindowMaximumSize(window.Window, width ?? 0, height ?? 0);
        }

        /// <inheritdoc/>
        public void GetMinClientSize(WindowHandle handle, out int? width, out int? height)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            int w, h;

            SDL_GetWindowMinimumSize(window.Window, out w, out h);

            width = w != 0 ? w : null;
            height = h != 0 ? h : null;
        }

        /// <inheritdoc/>
        public void SetMinClientSize(WindowHandle handle, int? width, int? height)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            SDL_SetWindowMinimumSize(window.Window, width ?? 0, height ?? 0);
        }

        /// <inheritdoc/>
        public DisplayHandle GetDisplay(WindowHandle handle)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            int index = SDL_GetWindowDisplayIndex(window.Window);

            // FIXME: We should probably call SDLDisplayComponent.Create or something like that...
            return new SDLDisplay(index);
        }

        /// <inheritdoc/>
        public WindowMode GetMode(WindowHandle handle)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            // FIXME:

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void SetMode(WindowHandle handle, WindowMode mode)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            switch (mode)
            {
                case WindowMode.Hidden:
                    SDL_HideWindow(window.Window);
                    break;
                case WindowMode.Minimized:
                    SDL_MinimizeWindow(window.Window);
                    break;
                case WindowMode.Normal:
                    SDL_ShowWindow(window.Window);
                    SDL_RestoreWindow(window.Window);
                    break;
                case WindowMode.Maximized:
                    SDL_MaximizeWindow(window.Window);
                    break;
                case WindowMode.WindowedFullscreen:
                    throw new NotImplementedException();
                case WindowMode.ExclusiveFullscreen:
                    throw new NotImplementedException();
                default:
                    throw new InvalidEnumArgumentException(nameof(mode), (int)mode, typeof(WindowMode));
            }
        }

        /// <inheritdoc/>
        public WindowStyle GetBorderStyle(WindowHandle handle)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            SDL_WindowFlags flags = SDL_GetWindowFlags(window.Window);

            bool hasBorder = flags.HasFlag(SDL_WindowFlags.SDL_WINDOW_BORDERLESS) == false;
            bool resizable = flags.HasFlag(SDL_WindowFlags.SDL_WINDOW_RESIZABLE);

            if (hasBorder && resizable)
            {
                return WindowStyle.ResizableBorder;
            }
            else if (hasBorder && resizable == false)
            {
                return WindowStyle.FixedBorder;
            }
            else if (hasBorder == false)
            {
                return WindowStyle.Borderless;
            }

            // FIXME: Toolbox windows.
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void SetBorderStyle(WindowHandle handle, WindowStyle style)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            switch (style)
            {
                case WindowStyle.Borderless:
                    SDL_SetWindowBordered(window.Window, 0);
                    // FIXME: Maybe this borderless should not be resizable?
                    SDL_SetWindowResizable(window.Window, 1);
                    break;
                case WindowStyle.FixedBorder:
                    SDL_SetWindowBordered(window.Window, 1);
                    SDL_SetWindowResizable(window.Window, 0);
                    break;
                case WindowStyle.ResizableBorder:
                    SDL_SetWindowBordered(window.Window, 1);
                    SDL_SetWindowResizable(window.Window, 1);
                    break;
                case WindowStyle.ToolBox:
                    throw new NotImplementedException();
                default:
                    throw new InvalidEnumArgumentException(nameof(style), (int)style, typeof(WindowStyle));
            }
        }

        /// <inheritdoc/>
        public void SetAlwaysOnTop(WindowHandle handle, bool floating)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            SDL_SetWindowAlwaysOnTop(window.Window, floating ? 1 : 0);
        }

        /// <inheritdoc/>
        public bool IsAlwaysOnTop(WindowHandle handle)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            SDL_WindowFlags flags = SDL_GetWindowFlags(window.Window);

            return flags.HasFlag(SDL_WindowFlags.SDL_WINDOW_ALWAYS_ON_TOP);
        }

        /// <inheritdoc/>
        public void SetHitTestCallback(WindowHandle handle, HitTest? test)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            window.HitTest = test;
            if (test == null)
            {
                SDL_SetWindowHitTest(window.Window, null, IntPtr.Zero);
            }
            else
            {
                SDL_SetWindowHitTest(window.Window, SDL_HitTest, IntPtr.Zero);
            }
            
            static SDL_HitTestResult SDL_HitTest(SDL_WindowPtr win, in SDL_Point pt, IntPtr data)
            {
                SDLWindow window = WindowDict[SDL_GetWindowID(win)];

                if (window.HitTest != null)
                {
                    HitType result = window.HitTest(window, new Vector2(pt.x, pt.y));
                    return result switch
                    {
                        HitType.Default => throw new NotSupportedException("SDL2 doesn't support HitType.Default. Consider removing the hit test callback to get default behaviour."),
                        HitType.Normal => SDL_HitTestResult.SDL_HITTEST_NORMAL,
                        HitType.Draggable => SDL_HitTestResult.SDL_HITTEST_DRAGGABLE,
                        HitType.ResizeTopLeft => SDL_HitTestResult.SDL_HITTEST_RESIZE_TOPLEFT,
                        HitType.ResizeTop => SDL_HitTestResult.SDL_HITTEST_RESIZE_TOP,
                        HitType.ResizeTopRight => SDL_HitTestResult.SDL_HITTEST_RESIZE_TOPRIGHT,
                        HitType.ResizeRight => SDL_HitTestResult.SDL_HITTEST_RESIZE_RIGHT,
                        HitType.ResizeBottomRight => SDL_HitTestResult.SDL_HITTEST_RESIZE_BOTTOMRIGHT,
                        HitType.ResizeBottom => SDL_HitTestResult.SDL_HITTEST_RESIZE_BOTTOM,
                        HitType.ResizeBottomLeft => SDL_HitTestResult.SDL_HITTEST_RESIZE_BOTTOMLEFT,
                        HitType.ResizeLeft => SDL_HitTestResult.SDL_HITTEST_RESIZE_LEFT,
                        _ => throw new InvalidEnumArgumentException("return", (int)result, typeof(HitType)),
                    };
                }
                else
                {
                    throw new InvalidOperationException("The window hit-test has been removed.");
                }
            }
        }

        /// <inheritdoc/>
        public void SetCursor(WindowHandle handle, CursorHandle? cursor)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            if (cursor == null)
            {
                SDL_ShowCursor(0 /* SDL_DISABLE */);
            }
            else
            {
                SDL_ShowCursor(1 /* SDL_ENABLE */);
            }

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public CursorCaptureMode GetCursorCaptureMode(WindowHandle handle)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            bool grabbed = SDL_GetWindowGrab(window.Window) == 1;

            // FIXME: CursorCaptureMode.Locked!

            if (grabbed)
            {
                return CursorCaptureMode.Confined;
            }
            else
            {
                return CursorCaptureMode.Normal;
            }

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void SetCursorCaptureMode(WindowHandle handle, CursorCaptureMode mode)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            switch (mode)
            {
                case CursorCaptureMode.Normal:
                    SDL_SetWindowGrab(window.Window, 0);
                    break;
                case CursorCaptureMode.Confined:
                    SDL_SetWindowGrab(window.Window, 1);
                    break;
                case CursorCaptureMode.Locked:
                    // FIXME: Use SDL_SetRelativeMouseMode in some way...
                    throw new NotImplementedException();
                default:
                    throw new InvalidEnumArgumentException(nameof(mode), (int)mode, typeof(CursorCaptureMode));
            }
        }

        /// <inheritdoc/>
        public void FocusWindow(WindowHandle handle)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            SDL_RaiseWindow(window.Window);
        }

        /// <inheritdoc/>
        public void RequestAttention(WindowHandle handle)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            SDL_FlashWindow(window.Window, SDL_FlashOperation.SDL_FLASH_UNTIL_FOCUSED);
        }

        /// <inheritdoc/>
        public void ScreenToClient(WindowHandle handle, int x, int y, out int clientX, out int clientY)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            // FIXME: How to do this??

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void ClientToScreen(WindowHandle handle, int clientX, int clientY, out int x, out int y)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            // FIXME: How to do this??

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void SwapBuffers(WindowHandle handle)
        {
            SDLWindow window = handle.As<SDLWindow>(this);

            SDL_GL_SwapWindow(window.Window);
        }
    }
}
