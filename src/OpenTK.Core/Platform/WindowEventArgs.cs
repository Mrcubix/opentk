using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace OpenTK.Core.Platform
{
    /// <summary>
    /// Base class for OpenTK window event arguments.
    /// </summary>
    public class WindowEventArgs : EventArgs
    {
    }

    public class FocusEventArgs : WindowEventArgs
    {
        public bool GotFocus { get; private set; }

        public FocusEventArgs(bool gotFocus)
        {
            GotFocus = gotFocus;
        }
    }

    public class WindowMoveEventArgs : WindowEventArgs
    {
        public WindowHandle Window { get; private set; }

        public Vector2i WindowPosition { get; private set; }

        public Vector2i ClientAreaPosition { get; private set; }

        public WindowMoveEventArgs(WindowHandle window, Vector2i windowPosition, Vector2i clientAreaPosition)
        {
            Window = window;
            WindowPosition = windowPosition;
            ClientAreaPosition = clientAreaPosition;
        }
    }

    public class WindowResizeEventArgs : WindowEventArgs
    {
        public WindowHandle Window { get; private set; }

        public Vector2i NewSize { get; private set; }

        public WindowResizeEventArgs(WindowHandle window, Vector2i newSize)
        {
            Window = window;
            NewSize = newSize;
        }
    }

    public class WindowModeChangeEventArgs : WindowEventArgs
    {
        public WindowHandle Window { get; private set; }

        public WindowMode NewMode { get; private set; }

        public WindowModeChangeEventArgs(WindowHandle window, WindowMode newMode)
        {
            Window = window;
            NewMode = newMode;
        }
    }

    public class WindowDpiChangeEventArgs : WindowEventArgs
    {
        public WindowHandle Window { get; private set; }

        public int DpiX { get; private set; }

        public int DpiY { get; private set; }

        public float ScaleX { get; private set; }

        public float ScaleY { get; private set; }

        public WindowDpiChangeEventArgs(WindowHandle window, int dpiX, int dpiY, float scaleX, float scaleY)
        {
            Window = window;
            DpiX = dpiX;
            DpiY = dpiY;
            ScaleX = scaleX;
            ScaleY = scaleY;
        }
    }

    public class KeyDownEventArgs : WindowEventArgs
    {
        // FIXME: These properties are for testing with the Win32 backend.

        public ulong VirtualKey { get; private set; }

        public bool WasDown { get; private set; }

        public bool Extended { get; private set; }

        public KeyDownEventArgs(ulong virtualKey, bool wasDown, bool extended)
        {
            VirtualKey = virtualKey;
            WasDown = wasDown;
            Extended = extended;
        }
    }

    public class KeyUpEventArgs : WindowEventArgs
    {
        // FIXME: These properties are for testing with the Win32 backend.

        public ulong VirtualKey { get; private set; }

        public bool Extended { get; private set; }

        public KeyUpEventArgs(ulong virtualKey, bool extended)
        {
            VirtualKey = virtualKey;
            Extended = extended;
        }
    }

    public class TextInputEventArgs : WindowEventArgs
    {
        public string Text { get; private set; }

        public TextInputEventArgs(string text)
        {
            Text = text;
        }
    }

    public class MouseEnterEventArgs : WindowEventArgs
    {
        public bool Entered { get; set; }

        public MouseEnterEventArgs(bool entered)
        {
            Entered = entered;
        }
    }

    public class MouseMoveEventArgs : WindowEventArgs
    {
        public int DeltaX { get; private set; }

        public int DeltaY { get; private set; }

        public MouseMoveEventArgs(int deltaX, int deltaY)
        {
            DeltaX = deltaX;
            DeltaY = deltaY;
        }
    }

    // FIXME: Maybe make MouseButtonDown and MouseButtonUp the same event type?
    public class MouseButtonDownEventArgs : WindowEventArgs
    {
        public MouseButton Button { get; private set; }

        public MouseButtonDownEventArgs(MouseButton button)
        {
            Button = button;
        }
    }

    public class MouseButtonUpEventArgs : WindowEventArgs
    {
        public MouseButton Button { get; private set; }

        public MouseButtonUpEventArgs(MouseButton button)
        {
            Button = button;
        }
    }

    public class ScrollEventArgs : WindowEventArgs
    {
        /// <summary>
        /// How much the mouse wheel has moved.
        /// </summary>
        public Vector2 Delta { get; private set; }

        /// <summary>
        /// The distance to move screen content.
        /// This takes into account user settings for scrolling speed.
        /// Measured in vertical lines and horizontal characters.
        /// </summary>
        // FIXME? Explain this better. Also does this exist on other platforms?
        public Vector2 Distance { get; private set; }

        public ScrollEventArgs(Vector2 delta, Vector2 distance)
        {
            Delta = delta;
            Distance = distance;
        }
    }

    public class CloseEventArgs : WindowEventArgs
    {
        public WindowHandle Window { get; private set; }

        public CloseEventArgs(WindowHandle window)
        {
            Window = window;
        }
    }

    public class FileDropEventArgs : WindowEventArgs
    {
        public IReadOnlyList<string> FilePaths { get; private set; }

        public Vector2i Position { get; private set; }

        public bool DroppedInWindow { get; private set; }

        public FileDropEventArgs(IReadOnlyList<string> filePaths, Vector2i position, bool droppedInWindow)
        {
            FilePaths = filePaths;
            Position = position;
            DroppedInWindow = droppedInWindow;
        }
    }
}
