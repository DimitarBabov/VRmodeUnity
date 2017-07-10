

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class VR_DesktopManager  {

    public static VR_DesktopManager Instance;

    public static bool ActionInThisFrame = false;
    
    [Tooltip("Keyboard key to show/drag/hide")]
    public KeyCode KeyboardShow = KeyCode.LeftControl;
    [Tooltip("Keyboard key to zoom")]
    public KeyCode KeyboardZoom = KeyCode.LeftAlt;

    [Tooltip("Distance of the screen if showed with keyboard/mouse. Change it at runtime with 'Show' + Mouse Wheel")]
    public float KeyboardDistance = 1;
    
    [Tooltip("If EnableZoomWithMenu is true, it's the distance between camera and monitor in Zoom Mode")]
    public float KeyboardZoomDistance = 0.5f;
    [Tooltip("If EnableZoomWithMenu is true, it's the distance between controller and monitor in Zoom Mode")]
    public float ControllerZoomDistance = 0.1f;
    [Tooltip("Monitor Scale Factor")]
    public float ScreenScaleFactor = 0.00025f;
    [Tooltip("Show a line between the controller and the cursor pointer on monitor.")]
    public bool ShowLine = true;
    [Tooltip("Monitor texture filtering")]
    public FilterMode TextureFilterMode = FilterMode.Point;
    [Tooltip("Monitor Color Space")]
    public bool LinearColorSpace = false;
    [Tooltip("Multimonitor - 0 for all, otherwise screen number 1..x")]
    public int MultiMonitorScreen = 0;
    [Tooltip("Distance offset between monitors if MultiMonitorScreen==0")]
    public Vector3 MultiMonitorPositionOffset = new Vector3(1,0,0);
    
    [Tooltip("Render Scale - Supersampling. GPU intensive if >1")]
    [Range(1f, 2f)]
    public float RenderScale = 1.0f;

    

    private System.Diagnostics.Process m_process = null;

    [DllImport("user32.dll")]
    private static extern System.IntPtr GetActiveWindow();

	private IntPtr unity_wnd;
	[DllImport("user32.dll")]
	private static extern System.Boolean ShowWindow(System.IntPtr hwnd, int show_flag);
	[DllImport("user32.dll")]
	private static extern System.IntPtr GetForegroundWindow();

	[DllImport("DesktopCapture")]
    private static extern void DesktopCapturePlugin_Initialize();
    [DllImport("DesktopCapture")]
    private static extern int DesktopCapturePlugin_GetNDesks();
    [DllImport("DesktopCapture")]
    private static extern int DesktopCapturePlugin_GetWidth(int iDesk);
    [DllImport("DesktopCapture")]
    private static extern int DesktopCapturePlugin_GetHeight(int iDesk);
    [DllImport("DesktopCapture")]
    private static extern int DesktopCapturePlugin_GetNeedReInit();
    [DllImport("DesktopCapture")]
    private static extern bool DesktopCapturePlugin_IsPointerVisible(int iDesk);
    [DllImport("DesktopCapture")]
    private static extern int DesktopCapturePlugin_GetPointerX(int iDesk);
    [DllImport("DesktopCapture")]
    private static extern int DesktopCapturePlugin_GetPointerY(int iDesk);
    [DllImport("DesktopCapture")]
    private static extern int DesktopCapturePlugin_SetTexturePtr(int iDesk, IntPtr ptr);
    [DllImport("DesktopCapture")]
    private static extern IntPtr DesktopCapturePlugin_GetRenderEventFunc();
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public SendInputEventType type;
        public MouseKeybdhardwareInputUnion mkhi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }
    
    [StructLayout(LayoutKind.Explicit)]
    struct MouseKeybdhardwareInputUnion
    {
        [FieldOffset(0)]
        public MouseInputData mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct HARDWAREINPUT
    {
        public int uMsg;
        public short wParamL;
        public short wParamH;
    }
    struct MouseInputData
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public MouseEventFlags dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
    [Flags]
    enum MouseEventFlags : uint
    {
        MOUSEEVENTF_MOVE = 0x0001,
        MOUSEEVENTF_LEFTDOWN = 0x0002,
        MOUSEEVENTF_LEFTUP = 0x0004,
        MOUSEEVENTF_RIGHTDOWN = 0x0008,
        MOUSEEVENTF_RIGHTUP = 0x0010,
        MOUSEEVENTF_MIDDLEDOWN = 0x0020,
        MOUSEEVENTF_MIDDLEUP = 0x0040,
        MOUSEEVENTF_XDOWN = 0x0080,
        MOUSEEVENTF_XUP = 0x0100,
        MOUSEEVENTF_WHEEL = 0x0800,
        MOUSEEVENTF_VIRTUALDESK = 0x4000,
        MOUSEEVENTF_ABSOLUTE = 0x8000
    }
    enum SendInputEventType : int
    {
        InputMouse,
        InputKeyboard,
        InputHardware
    }

    public enum SPIF
    {
        None = 0x00,
        /// <summary>Writes the new system-wide parameter setting to the user profile.</summary>
        SPIF_UPDATEINIFILE = 0x01,
        /// <summary>Broadcasts the WM_SETTINGCHANGE message after updating the user profile.</summary>
        SPIF_SENDCHANGE = 0x02,
        /// <summary>Same as SPIF_SENDCHANGE.</summary>
        SPIF_SENDWININICHANGE = 0x02
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref IntPtr pvParam, SPIF fWinIni); // T = any type

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, SPIF fWinIni);

    public const uint SPI_SETMOUSETRAILS = 0x005D;
    public const uint SPI_GETMOUSETRAILS = 0x005E;


    private static int needReinit = 0;
	private static bool needMinimizeUnityWnd = true;


	public Texture2D main_texture;


    private bool m_forceMouseTrail = false; // Otherwise cursor is not visible

	
    // Use this for initialization
    public void Start () {

		unity_wnd = GetActiveWindow();
		Instance = this;

        ReInit();

		if (GetMouseTrailEnabled() == false)
		{
			m_forceMouseTrail = true;
			SetMouseTrailEnabled(true);
		}

		//yield return new WaitForSeconds(1);
		//StartCoroutine(OnRender());
	}

   

    public void Stop()
    {
        if (m_forceMouseTrail)
            SetMouseTrailEnabled(false);

	}

    // Update is called once per frame
    public void Update () {

		GL.IssuePluginEvent(DesktopCapturePlugin_GetRenderEventFunc(), 0);

		ActionInThisFrame = false;

        if (UnityEngine.VR.VRSettings.renderScale != RenderScale)
            UnityEngine.VR.VRSettings.renderScale = RenderScale;

        needReinit = DesktopCapturePlugin_GetNeedReInit();
		

		//Work arround issues with dragging/flickering the overlay
		//The issue appears when switching to a diffrent window in the desktop. Then
		//Editor application.update function is not called that freaquently which causes flickering in the VR
		//When the unity main window is minimized the the issue disapears. This is a temporary fix!!!
		IntPtr top_wnd = GetForegroundWindow();
		if(needMinimizeUnityWnd && unity_wnd != null && top_wnd != null && unity_wnd != top_wnd)
		{
			ShowWindow(unity_wnd, 2);
			needMinimizeUnityWnd = false;
		}else
		{
			needMinimizeUnityWnd = true;
		}

		

		if (needReinit > 10000)
            ReInit();
/*
        if(Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.VR.InputTracking.Recenter();    
        }
		
        foreach (VdmDesktop monitor in Monitors)
        {
            monitor.HideLine();

            monitor.CheckKeyboardAndMouse();             
       
        }*/
    }
        
    public float GetScreenWidth(int screen)
    {
        return DesktopCapturePlugin_GetWidth(screen);
    }

    public float GetScreenHeight(int screen)
    {
        return DesktopCapturePlugin_GetHeight(screen);
    }

    public bool IsScreenPointerVisible(int screen)
    {
        return DesktopCapturePlugin_IsPointerVisible(screen);
    }

    public int GetScreenPointerX(int screen)
    {
        return DesktopCapturePlugin_GetPointerX(screen);
    }

    public int GetScreenPointerY(int screen)
    {
        return DesktopCapturePlugin_GetPointerY(screen);
    }
    
    public void SetCursorPos(float x, float y)
    {
        int iX = (int) x;
        int iY = (int) y;
        SetCursorPos(iX, iY);
    }

    public Vector2 GetCursorPos()
    {
        POINT p;
        GetCursorPos(out p);
        return new Vector2(p.X, p.Y);
    }

    private void ReInit()
    {
        DesktopCapturePlugin_Initialize();

       int width = DesktopCapturePlugin_GetWidth(0);
       int height = DesktopCapturePlugin_GetHeight(0);
		main_texture = new Texture2D(width, height, TextureFormat.BGRA32, false, LinearColorSpace);

       DesktopCapturePlugin_SetTexturePtr(0, main_texture.GetNativeTexturePtr());
		
            
          
    }


    public bool GetMouseTrailEnabled()
    {
        IntPtr Current = new IntPtr(0);
        SystemParametersInfo(SPI_GETMOUSETRAILS, 0, ref Current, SPIF.None);

        return (Current.ToInt32() >= 2);
    }

    public void SetMouseTrailEnabled(bool v)
    {
        IntPtr NullIntPtr = new IntPtr(0);
        if (v)
            SystemParametersInfo(SPI_SETMOUSETRAILS, 2, NullIntPtr, SPIF.None);
        else
            SystemParametersInfo(SPI_SETMOUSETRAILS, 0, NullIntPtr, SPIF.None);
    }



}
