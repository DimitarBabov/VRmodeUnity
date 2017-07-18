#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Timers;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.VR;
#if ENABLE_STEAMVR_INPUT
using Valve.VR;
#endif
using UnityEditor;

//[ExecuteInEditMode]
sealed class VRView
{
	#region FRAME RATE GENERATOR DECLARATIONS
	public IntPtr interactionWindow;
	IntPtr hMainWindow;
	IntPtr oldWndProcPtr;
	IntPtr newWndProcPtr;
	WndProcDelegate newWndProc;
	bool isFrameGeneratorRunning = false;
	private System.Diagnostics.Process m_process = null;
	Timer timer;
	
	[DllImport("user32.dll")]
	static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

	[DllImport("user32.dll")]
	static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	private static extern System.IntPtr GetActiveWindow();

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	static extern IntPtr SendNotifyMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

	#endregion

	const string k_ShowDeviceView = "VRView.ShowDeviceView";
	const string k_UseCustomPreviewCamera = "VRView.UseCustomPreviewCamera";
	const string k_LaunchOnExitPlaymode = "VRView.LaunchOnExitPlaymode";

	//OpenVR stuff
	CVRSystem hmd;
	VR_Overlay overlay;	
	VR_CameraRig vr_cam;
	VR_DesktopManager desktop;	
	Texture scene_cutout;
	Texture scene_mouse;
	Rect scene_rect = new Rect();
	Rect scene_mouse_rect = new Rect();	
	public static bool viewDisabled = true;
	public Rect guiRect { get; private set; }

	static VRView CreateInstance()
	{
		return new VRView();
	}

	public void Enable()
	{
		
		if (Application.isPlaying)
			return;

		StartFrameRateGenerator();

		viewDisabled = false;

		EditorApplication.playmodeStateChanged += OnPlaymodeStateChanged;
		//EditorApplication.update += Update;
		
		//Initialize VR
		var error = EVRInitError.None;

		hmd = OpenVR.System;
//
		if (hmd == null)
			hmd = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);//Make it VRApplication_Scene if go back 

		//OVERLAYS
		if (overlay == null)
		{
			overlay = new VR_Overlay();
			overlay.Create("Desktop", "Desktop Overlay");
		}
		
		//VR camera
		
		if (vr_cam == null)
		{
			vr_cam = new VR_CameraRig();
			vr_cam.Create();
			vr_cam.SetTexture();
		}

		if (desktop == null)
		{
			//VR_desktop
			desktop = new VR_DesktopManager();
			desktop.Start();
		}
		
		//temporary texture assignment........................................................................
		scene_cutout = Editor.FindObjectOfType<texture_sample>().sampleTexture;
		scene_mouse = Editor.FindObjectOfType<texture_sample>().mouseTexture;

		overlay.texture = new RenderTexture(desktop.main_texture.width, desktop.main_texture.height, 16);
		Graphics.Blit(desktop.main_texture, overlay.texture);
		
	}

	public void Disable()
	{
		viewDisabled = true;

		EditorApplication.playmodeStateChanged -= OnPlaymodeStateChanged;
		//EditorApplication.update -= Update;
		
		if (vr_cam != null )
		{
			vr_cam.Destroy();
			vr_cam = null;			
		}
		

		if (overlay != null)
		{
			overlay.Destroy();
			overlay = null;
		}
		

		OpenVR.Shutdown();
		StopFrameRateGenerator();
	}


	private void OnPlaymodeStateChanged()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode)
		{			
			EditorPrefs.SetBool(k_LaunchOnExitPlaymode, true);
			Disable();
		}


		if(!viewDisabled)
			Disable();

	}
	
	private void Update()
	{
		
		//copy desktop texture in overlay texture. This is neccesarry becasue desktop main texture is BGRA32 format and we need RGBA32
		Graphics.Blit(desktop.main_texture, overlay.texture);
		
		scene_rect = SceneView.GetWindow<SceneView>("Scene", false).position;

		if (desktop.isUnityWndOnTop())
		{
			if ((int)scene_rect.xMin > 0 || (int)scene_rect.yMin > 0)
				Graphics.CopyTexture(scene_cutout, 0, 0, 0, 0, (int)scene_rect.width, (int)scene_rect.height, overlay.texture, 0, 0, (int)scene_rect.xMin, (int)scene_rect.yMin);


			//make cursor visible when inside scene view window
			scene_mouse_rect.xMin = desktop.GetCursorPos().x;
			scene_mouse_rect.yMin = desktop.GetCursorPos().y;
			scene_mouse_rect.xMax = scene_mouse_rect.xMin + scene_mouse.width;
			scene_mouse_rect.yMax = scene_mouse_rect.yMin + scene_mouse.height;


			//show mouse cursor in the scene cutout
			if (scene_mouse_rect.xMin > scene_rect.xMin &&
				scene_mouse_rect.xMax < scene_rect.xMax &&
				scene_mouse_rect.yMin > scene_rect.yMin &&
				scene_mouse_rect.yMax < scene_rect.yMax)
			{

				Graphics.CopyTexture(scene_mouse, 0, 0, 0, 0, scene_mouse.width, scene_mouse.height, overlay.texture, 0, 0,
									(int)desktop.GetCursorPos().x, (int)desktop.GetCursorPos().y);
			}
		}
		// If code is compiling, then we need to clean up the window resources before classes get re-initialized
		if (EditorApplication.isCompiling)
		{
			Disable();
			return;
		}

		desktop.Update();
		vr_cam.Update();
		overlay.Update();

	}
	
	#region Frame Rate generator functions

	IntPtr wndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
	{
		if (msg == 1741)
		{
			//Debug.Log(msg);
			Update();
		}
		return CallWindowProc(oldWndProcPtr, hWnd, msg, wParam, lParam);
	}

	private void StartFrameRateGenerator()
	{
		if (isFrameGeneratorRunning)
			return;

		hMainWindow = GetActiveWindow();
		newWndProc = new WndProcDelegate(wndProc);
		newWndProcPtr = Marshal.GetFunctionPointerForDelegate(newWndProc);
		oldWndProcPtr = SetWindowLongPtr(hMainWindow, -4, newWndProcPtr);
		isFrameGeneratorRunning = true;

		//HackStart();
		
		timer = new Timer(15);
		
		timer.Elapsed += new System.Timers.ElapsedEventHandler(NextFrameReadyEvent);
		timer.Start();		
		
	}

	private void StopFrameRateGenerator()
	{
		//HackStop();
		Debug.Log("Uninstall Hook");
		if (!isFrameGeneratorRunning) return;
		SetWindowLongPtr(hMainWindow, -4, oldWndProcPtr);
		hMainWindow = IntPtr.Zero;
		oldWndProcPtr = IntPtr.Zero;
		newWndProcPtr = IntPtr.Zero;
		newWndProc = null;
		isFrameGeneratorRunning = false;
		
		timer.Stop();
		timer.Dispose();
		timer = null;
	}
	
	private void NextFrameReadyEvent(object source, System.Timers.ElapsedEventArgs e)
	{
		
		SendNotifyMessage(hMainWindow, 1741, IntPtr.Zero, IntPtr.Zero);
	}
	/*
	public void HackStart()
	{
		HackStop();

		string exePath = "Assets\\MyEditor\\Hack\\VrDesktopMirrorWorkaround.exe";
		if (System.IO.File.Exists(exePath))
		{
			m_process = new System.Diagnostics.Process();
			m_process.StartInfo.FileName = exePath;
			m_process.StartInfo.CreateNoWindow = true;
			m_process.StartInfo.UseShellExecute = true;
			m_process.StartInfo.Arguments = hMainWindow.ToString();
			m_process.Start();
		}
		else
		{
			Debug.Log("VR Desktop Mirror Hack exe not found: " + exePath);
		}
	}
	
	public void HackStop()
	{


		if (m_process != null)
		{
			if (m_process.HasExited == false)
			{
				m_process.Kill();
			}
		}
		m_process = null;
	}
	
	*/
#endregion
	public static VRView instance;

	[MenuItem("VR Mode/Enable VR %e", false)]
	static void EditorVR()
	{
		if (Application.isPlaying)
			return;

		viewDisabled = !viewDisabled;

		// Using a utility window improves performance by saving from the overhead of DockArea.OnGUI()
		
		if (instance == null)
		{
			instance = CreateInstance();
			instance.Enable();
			return;
		}

		if(viewDisabled)
		{
			instance.Disable();
		}else
		{
			instance.Enable();
		}

		


		//EditorWindow.GetWindow<VRView>(true, "VR Mode", true);
	}
		

	[MenuItem("VR Mode/Enable Edit VR %e", true)]
	static bool ShouldShowEditorVR()
	{
		
		return PlayerSettings.virtualRealitySupported;
	}


	[MenuItem("VR Mode/Reposition VR camera %z", false)]
	static void RepositionEditorVR()
	{
		Debug.Log("VR camera repositioned...");
		VR_Overlay.instance.reposition(VR_CameraRig.instance.transform);
	}

	[MenuItem("VR Mode/Reposition VR camera %x", false)]
	static void ZoomOutEditorVR()
	{
		Debug.Log("VR camera zoomed out...");
		VR_Overlay.instance.ZoomOut(VR_CameraRig.instance.transform);
	}

	[MenuItem("VR Mode/Reposition VR camera %c", false)]
	static void ZoomInEditorVR()
	{
		Debug.Log("VR camera zoomed in...");
		VR_Overlay.instance.ZoomIn(VR_CameraRig.instance.transform);
	}
	
}
#endif