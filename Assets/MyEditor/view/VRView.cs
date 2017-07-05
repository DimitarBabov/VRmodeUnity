#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.VR;
#if ENABLE_STEAMVR_INPUT
using Valve.VR;
#endif
using UnityEditor;

sealed class VRView 
{
	const string k_ShowDeviceView = "VRView.ShowDeviceView";
	const string k_UseCustomPreviewCamera = "VRView.UseCustomPreviewCamera";
	const string k_LaunchOnExitPlaymode = "VRView.LaunchOnExitPlaymode";
	
	//OpenVR stuff
	CVRSystem hmd;
	VR_Overlay overlay;	
	VR_CameraRig vr_cam;
	VR_DesktopManager desktop;

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

		viewDisabled = false;

		EditorApplication.playmodeStateChanged += OnPlaymodeStateChanged;
		EditorApplication.update += Update;


		//Initialize VR
		var error = EVRInitError.None;

		hmd = OpenVR.System;

		if (hmd == null)
			hmd = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);

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
		overlay.texture = desktop.main_texture;

	}

	public void Disable()
	{
		viewDisabled = true;

		EditorApplication.playmodeStateChanged -= OnPlaymodeStateChanged;
		EditorApplication.update -= Update;
		
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
		if (Application.isPlaying)
			return;

		// If code is compiling, then we need to clean up the window resources before classes get re-initialized
		if (EditorApplication.isCompiling)
		{
			Disable();
			return;
		}

		desktop.Update();
		vr_cam.Update();
		overlay.UpdateOverlay();

	}

	

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

	[MenuItem("VR Mode/Reposition VR camera %q", false)]
	static void CloseEditorVR()
	{
		Debug.Log("VR camera repositioned...");
		//VR_Overlay.instance.reposition();
	}
}
#endif