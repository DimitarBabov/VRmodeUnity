#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.VR;
#if ENABLE_STEAMVR_INPUT
using Valve.VR;
#endif
using UnityEditor;


sealed class VRView : EditorWindow
{
	const string k_ShowDeviceView = "VRView.ShowDeviceView";
	const string k_UseCustomPreviewCamera = "VRView.UseCustomPreviewCamera";
	const string k_LaunchOnExitPlaymode = "VRView.LaunchOnExitPlaymode";
	bool m_ShowDeviceView;


	static VRView s_ActiveView;
	//OpenVR stuff
	CVRSystem hmd;
	CVRCompositor compositor;
	

	VR_Overlay overlay;

	VR_CameraRig vr_cam;
	VR_DesktopManager desktop;

	Transform m_CameraRig;

	bool m_HMDReady;
	

	public static Transform cameraRig
	{
		get
		{
			if (s_ActiveView)
				return s_ActiveView.m_CameraRig;

			return null;
		}
	}
	public static VRView activeView
	{
		get { return s_ActiveView; }
	}

	public static event Action viewEnabled;
	public static event Action viewDisabled;
	public static event Action<EditorWindow> beforeOnGUI;
	
	public static event Action<bool> hmdStatusChange;

	public Rect guiRect { get; private set; }

	static VRView GetWindow()
	{
		return GetWindow<VRView>(true);
	}


	
	static void ReopenOnExitPlaymode()
	{
		bool launch = EditorPrefs.GetBool(k_LaunchOnExitPlaymode, false);
		if (!launch || !EditorApplication.isPlaying)
		{
			EditorPrefs.DeleteKey(k_LaunchOnExitPlaymode);
			EditorApplication.update -= ReopenOnExitPlaymode;
			if (launch)
				GetWindow<VRView>();
		}
		
	}



	public void OnEnable()
	{
		EditorApplication.playmodeStateChanged += OnPlaymodeStateChanged;
		EditorApplication.update += UpdateBackground;

		Assert.IsNull(s_ActiveView, "Only one EditorVR should be active");


		//Initialize VR
		VR_init();		
		
	}

	public void OnDisable()
	{
		if (viewDisabled != null)
			viewDisabled();

		EditorApplication.playmodeStateChanged -= OnPlaymodeStateChanged;
		EditorApplication.update -= UpdateBackground;

		OpenVR.Shutdown();

		EditorPrefs.SetBool(k_ShowDeviceView, m_ShowDeviceView);

		if (m_CameraRig)
			DestroyImmediate(m_CameraRig.gameObject, true);

		if (vr_cam)
			vr_cam.Destroy();


		if(overlay!=null)
			overlay.Destroy();

		if (desktop != null)
			desktop.Stop();


	}

	

	

	void OnGUI()
	{
		
		//Will render texture on window...
		
	}

	

	private void OnPlaymodeStateChanged()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode)
		{			
			EditorPrefs.SetBool(k_LaunchOnExitPlaymode, true);
			Close();
		}
	}
	
	private void UpdateBackground()
	{

		// If code is compiling, then we need to clean up the window resources before classes get re-initialized
		if (EditorApplication.isCompiling)
		{
			Close();
			return;
		}

		VR_render();

		UpdateHMDStatus();

	}

	void UpdateHMDStatus()
	{
		if (hmdStatusChange != null)
		{
			var ready = GetIsUserPresent();
			if (m_HMDReady != ready)
			{
				m_HMDReady = ready;
				hmdStatusChange(ready);
			}
		}
	}

	static bool GetIsUserPresent()
	{
#if ENABLE_OVR_INPUT
			if (VRSettings.loadedDeviceName == "Oculus")
				return OVRPlugin.userPresent;
#endif
#if ENABLE_STEAMVR_INPUT
			if (VRSettings.loadedDeviceName == "OpenVR")
				return OpenVR.System.GetTrackedDeviceActivityLevel(0) == EDeviceActivityLevel.k_EDeviceActivityLevel_UserInteraction;
#endif
		return true;
	}
	
	public void VR_init()
	{

		var error = EVRInitError.None;
		hmd = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);
		
	    //OVERLAY
		overlay = new VR_Overlay();
		overlay.Create();

		
		
		//VR camera
		vr_cam = new VR_CameraRig();
		vr_cam.Create();
		vr_cam.SetTexture();

		//VR_desktop
		desktop = new VR_DesktopManager();
		desktop.Start();

		//temporary texture assignment........................................................................
		overlay.texture = desktop.main_texture;
	}

	public void VR_render()
	{
		if (Application.isPlaying)
			return;

		desktop.Update();
		vr_cam.Render();
		overlay.UpdateOverlay();


	}

	[MenuItem("VR Mode/Enable Edit VR %e", false)]
	static void ShowEditorVR()
	{
		// Using a utility window improves performance by saving from the overhead of DockArea.OnGUI()
		EditorWindow.GetWindow<VRView>(true, "VR Mode", true);
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