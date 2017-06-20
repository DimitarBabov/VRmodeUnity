//#if UNITY_EDITOR  
using System;
using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
//using UnityEditor.Experimental.EditorVR.Helpers;
using System.Reflection;
using UnityEngine.VR;
#if ENABLE_STEAMVR_INPUT
using Valve.VR;
#endif
using UnityEditor;
using UnityObject = UnityEngine.Object;


[InitializeOnLoad]
sealed class VRView : EditorWindow
{
	const string k_ShowDeviceView = "VRView.ShowDeviceView";
	const string k_UseCustomPreviewCamera = "VRView.UseCustomPreviewCamera";
	const string k_LaunchOnExitPlaymode = "VRView.LaunchOnExitPlaymode";

	DrawCameraMode m_RenderMode = DrawCameraMode.Textured;



	Camera m_CustomPreviewCamera;

	[NonSerialized]
	Camera m_Camera;

	LayerMask? m_CullingMask;
	RenderTexture m_TargetTexture;
	bool m_ShowDeviceView;
	EditorWindow[] m_EditorWindows;
	static VRView s_ActiveView;
	//OpenVR stuff
	TrackedDevicePose_t[] renderPoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
	TrackedDevicePose_t[] gamePoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
	VRTextureBounds_t[] textureBounds;
	Texture_t VR_textureLeftEye, VR_textureRightEye;
	RenderTexture renderTextureLeftEye;
	RenderTexture renderTextureRightEye;

	CVRSystem hmd;



	Transform m_CameraRig;

	bool m_HMDReady;
	bool m_UseCustomPreviewCamera;

	public static Transform cameraRig
	{
		get
		{
			if (s_ActiveView)
				return s_ActiveView.m_CameraRig;

			return null;
		}
	}

	public static Camera viewerCamera
	{
		get
		{
			if (s_ActiveView)
				return s_ActiveView.m_Camera;

			return null;
		}
	}

	public static VRView activeView
	{
		get { return s_ActiveView; }
	}

	public static bool showDeviceView
	{
		get { return s_ActiveView && s_ActiveView.m_ShowDeviceView; }
	}

	public static LayerMask cullingMask
	{
		set
		{
			if (s_ActiveView)
				s_ActiveView.m_CullingMask = value;
		}
	}
	// To allow for alternate previews (e.g. smoothing)
	public static Camera customPreviewCamera
	{
		set
		{
			if (s_ActiveView)
				s_ActiveView.m_CustomPreviewCamera = value;
		}
		get
		{
			return s_ActiveView && s_ActiveView.m_UseCustomPreviewCamera ?
				s_ActiveView.m_CustomPreviewCamera : null;
		}
	}

		public static event Action viewEnabled;
		public static event Action viewDisabled;
		public static event Action<EditorWindow> beforeOnGUI;
		public static event Action<EditorWindow> afterOnGUI;
		public static event Action<bool> hmdStatusChange;

	public Rect guiRect { get; private set; }

	static VRView GetWindow()
	{
		return GetWindow<VRView>(true);
	}

	public static Coroutine StartCoroutine(IEnumerator routine)
	{
		if (s_ActiveView && s_ActiveView.m_CameraRig)
		{
			var mb = s_ActiveView.m_CameraRig.GetComponent<EditorMonoBehaviour>();
			return mb.StartCoroutine(routine);
		}

		return null;
	}

	// Life cycle management across playmode switches is an odd beast indeed, and there is a need to reliably relaunch
	// EditorVR after we switch back out of playmode (assuming the view was visible before a playmode switch). So,
	// we watch until playmode is done and then relaunch.  
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

		Assert.IsNull(s_ActiveView, "Only one EditorVR should be active");

		autoRepaintOnSceneChange = true;
		s_ActiveView = this;

		GameObject cameraGO = EditorUtility.CreateGameObjectWithHideFlags("VRCamera", HideFlags.HideAndDontSave, typeof(Camera));
		m_Camera = cameraGO.GetComponent<Camera>();
		m_Camera.useOcclusionCulling = false;
		m_Camera.enabled = false;
		m_Camera.cameraType = CameraType.VR;

		GameObject rigGO = EditorUtility.CreateGameObjectWithHideFlags("VRCameraRig", HideFlags.HideAndDontSave, typeof(EditorMonoBehaviour));
		m_CameraRig = rigGO.transform;
		m_Camera.transform.parent = m_CameraRig;
		m_Camera.nearClipPlane = 0.01f;
		m_Camera.farClipPlane = 1000f;

		// Generally, we want to be at a standing height, so default to that
		const float kHeadHeight = 1.7f;
		Vector3 position = m_CameraRig.position;
		position.y = kHeadHeight;
		m_CameraRig.position = position;
		m_CameraRig.rotation = Quaternion.identity;

		m_ShowDeviceView = EditorPrefs.GetBool(k_ShowDeviceView, false);
		m_UseCustomPreviewCamera = EditorPrefs.GetBool(k_UseCustomPreviewCamera, false);

		// Disable other views to increase rendering performance for EditorVR
		SetOtherViewsEnabled(false);

		// VRSettings.enabled latches the reference pose for the current camera
		Camera currentCamera = Camera.main;
		Camera.SetupCurrent(m_Camera);

		renderTextureLeftEye = new RenderTexture(1500,1500,24);
		renderTextureLeftEye.format = RenderTextureFormat.ARGBHalf;
		renderTextureLeftEye.antiAliasing = 2;
		renderTextureLeftEye.Create();

		currentCamera.targetTexture = renderTextureLeftEye;
		currentCamera.Render();

		renderTextureLeftEye = currentCamera.targetTexture;

	

		renderTextureRightEye = currentCamera.targetTexture;
		VR_init();

		InputTracking.Recenter();



		//if (viewEnabled != null)
		//viewEnabled();
	}

	public void OnDisable()
	{
		if (viewDisabled != null)
			viewDisabled();

		EditorApplication.playmodeStateChanged -= OnPlaymodeStateChanged;

		//VRSettings.enabled = false;
		OpenVR.Shutdown();

		EditorPrefs.SetBool(k_ShowDeviceView, m_ShowDeviceView);
		EditorPrefs.SetBool(k_UseCustomPreviewCamera, m_UseCustomPreviewCamera);

		SetOtherViewsEnabled(true);

		if (m_CameraRig)
			DestroyImmediate(m_CameraRig.gameObject, true);

		Assert.IsNotNull(s_ActiveView, "EditorVR should have an active view");
		s_ActiveView = null;
	}

	void UpdateCameraTransform()
	{
		var cameraTransform = m_Camera.transform;
		cameraTransform.localPosition = InputTracking.GetLocalPosition(VRNode.Head);
		cameraTransform.localRotation = InputTracking.GetLocalRotation(VRNode.Head);
	}

	public void CreateCameraTargetTexture(ref RenderTexture renderTexture, Rect cameraRect, bool hdr)
	{
		bool useSRGBTarget = QualitySettings.activeColorSpace == ColorSpace.Linear;

		int msaa = Mathf.Max(1, QualitySettings.antiAliasing);

		RenderTextureFormat format = hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
		if (renderTexture != null)
		{
			bool matchingSRGB = renderTexture != null && useSRGBTarget == renderTexture.sRGB;

			if (renderTexture.format != format || renderTexture.antiAliasing != msaa || !matchingSRGB)
			{
				DestroyImmediate(renderTexture);
				renderTexture = null;
			}
		}

		Rect actualCameraRect = cameraRect;
		int width = (int)actualCameraRect.width;
		int height = (int)actualCameraRect.height;

		if (renderTexture == null)
		{
			renderTexture = new RenderTexture(0, 0, 24, format);
			renderTexture.name = "Scene RT";
			renderTexture.antiAliasing = msaa;
			renderTexture.hideFlags = HideFlags.HideAndDontSave;
		}
		if (renderTexture.width != width || renderTexture.height != height)
		{
			renderTexture.Release();
			renderTexture.width = width;
			renderTexture.height = height;
		}
		renderTexture.Create();
	}

	void PrepareCameraTargetTexture(Rect cameraRect)
	{
		// Always render camera into a RT
		CreateCameraTargetTexture(ref m_TargetTexture, cameraRect, false);
		m_Camera.targetTexture = m_ShowDeviceView ? m_TargetTexture : null;
		VRSettings.showDeviceView = !customPreviewCamera && m_ShowDeviceView;
	}

	void OnGUI()
	{
		if (beforeOnGUI != null)
			beforeOnGUI(this);

		var rect = guiRect;
		rect.x = 0;
		rect.y = 0;
		rect.width = position.width;
		rect.height = position.height;
		guiRect = rect;
		var cameraRect = EditorGUIUtility.PointsToPixels(guiRect);
		PrepareCameraTargetTexture(cameraRect);

		m_Camera.cullingMask = m_CullingMask.HasValue ? m_CullingMask.Value.value : UnityEditor.Tools.visibleLayers;

		DoDrawCamera(guiRect);

		Event e = Event.current;
		if (m_ShowDeviceView)
		{
			if (e.type == EventType.Repaint)
			{
				GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
				var renderTexture = customPreviewCamera && customPreviewCamera.targetTexture ? customPreviewCamera.targetTexture : m_TargetTexture;
				GUI.BeginGroup(guiRect);
				GUI.DrawTexture(guiRect, renderTexture, ScaleMode.StretchToFill, false);
				GUI.EndGroup();
				GL.sRGBWrite = false;
			}
		}

		GUILayout.BeginArea(guiRect);
		{
			if (GUILayout.Button("Toggle Device View", EditorStyles.toolbarButton))
				m_ShowDeviceView = !m_ShowDeviceView;

			if (m_CustomPreviewCamera)
			{
				GUILayout.FlexibleSpace();
				GUILayout.BeginHorizontal();
				{
					GUILayout.FlexibleSpace();
					m_UseCustomPreviewCamera = GUILayout.Toggle(m_UseCustomPreviewCamera, "Use Presentation Camera");
				}
				GUILayout.EndHorizontal();
			}
		}
		GUILayout.EndArea();

		if (afterOnGUI != null)
			afterOnGUI(this);
	}

	void DoDrawCamera(Rect rect)
	{
		if (!m_Camera.gameObject.activeInHierarchy)
			return;


		//UnityEditor.Handles.DrawCamera(rect, m_Camera, m_RenderMode);
		if (Event.current.type == EventType.Repaint)
		{
			GUI.matrix = Matrix4x4.identity; // Need to push GUI matrix back to GPU after camera rendering
			RenderTexture.active = null; // Clean up after DrawCamera
		}
	}

	private void OnPlaymodeStateChanged()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode)
		{
			EditorPrefs.SetBool(k_LaunchOnExitPlaymode, true);
			Close();
		}
	}

	private void Update()
	{
		// If code is compiling, then we need to clean up the window resources before classes get re-initialized
		if (EditorApplication.isCompiling)
		{
			Close();
			return;
		}

		// Force the window to repaint every tick, since we need live updating
		// This also allows scripts with [ExecuteInEditMode] to run

		//EditorApplication.SetSceneRepaintDirty();

		// Our camera is disabled, so it doesn't get automatically updated to HMD values until it renders
		UpdateCameraTransform();

		VR_render();

		UpdateHMDStatus();

		SetSceneViewsAutoRepaint(false);
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

	void SetGameViewsAutoRepaint(bool enabled)
	{
		var asm = Assembly.GetAssembly(typeof(UnityEditor.EditorWindow));
		var type = asm.GetType("UnityEditor.GameView");
		SetAutoRepaintOnSceneChanged(type, enabled);
	}

	void SetSceneViewsAutoRepaint(bool enabled)
	{
		SetAutoRepaintOnSceneChanged(typeof(SceneView), enabled);
	}

	void SetOtherViewsEnabled(bool enabled)
	{
		SetGameViewsAutoRepaint(enabled);
		SetSceneViewsAutoRepaint(enabled);
	}

	void SetAutoRepaintOnSceneChanged(Type viewType, bool enabled)
	{
		if (m_EditorWindows == null)
			m_EditorWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();

		var windowCount = m_EditorWindows.Length;
		var mouseOverWindow = EditorWindow.mouseOverWindow;
		for (int i = 0; i < windowCount; i++)
		{
			var window = m_EditorWindows[i];
			if (window.GetType() == viewType)
				window.autoRepaintOnSceneChange = enabled || (window == mouseOverWindow);
		}


	}



	public void VR_init()
	{
		renderPoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
		gamePoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

		var error = EVRInitError.None;
		hmd = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);
		//Debug.Log("Connected to " + hmd_TrackingSystemName + ":" + hmd_SerialNumber);

		var compositor = OpenVR.Compositor;
		var overlay = OpenVR.Overlay;

		// Setup render values
		uint w = 0, h = 0;
		hmd.GetRecommendedRenderTargetSize(ref w, ref h);
		float sceneWidth = (float)w;
		float sceneHeight = (float)h;

		float l_left = 0.0f, l_right = 0.0f, l_top = 0.0f, l_bottom = 0.0f;
		hmd.GetProjectionRaw(EVREye.Eye_Left, ref l_left, ref l_right, ref l_top, ref l_bottom);

		float r_left = 0.0f, r_right = 0.0f, r_top = 0.0f, r_bottom = 0.0f;
		hmd.GetProjectionRaw(EVREye.Eye_Right, ref r_left, ref r_right, ref r_top, ref r_bottom);

		Vector2 tanHalfFov = new Vector2(
			Mathf.Max(-l_left, l_right, -r_left, r_right),
			Mathf.Max(-l_top, l_bottom, -r_top, r_bottom));

		textureBounds = new VRTextureBounds_t[2];

		textureBounds[0].uMin = 0.5f + 0.5f * l_left / tanHalfFov.x;
		textureBounds[0].uMax = 0.5f + 0.5f * l_right / tanHalfFov.x;
		textureBounds[0].vMin = 0.5f - 0.5f * l_bottom / tanHalfFov.y;
		textureBounds[0].vMax = 0.5f - 0.5f * l_top / tanHalfFov.y;

		textureBounds[1].uMin = 0.5f + 0.5f * r_left / tanHalfFov.x;
		textureBounds[1].uMax = 0.5f + 0.5f * r_right / tanHalfFov.x;
		textureBounds[1].vMin = 0.5f - 0.5f * r_bottom / tanHalfFov.y;
		textureBounds[1].vMax = 0.5f - 0.5f * r_top / tanHalfFov.y;

		// Grow the recommended size to account for the overlapping fov
		sceneWidth = sceneWidth / Mathf.Max(textureBounds[0].uMax - textureBounds[0].uMin, textureBounds[1].uMax - textureBounds[1].uMin);
		sceneHeight = sceneHeight / Mathf.Max(textureBounds[0].vMax - textureBounds[0].vMin, textureBounds[1].vMax - textureBounds[1].vMin);

		float aspect = tanHalfFov.x / tanHalfFov.y;
		float fieldOfView = 2.0f * Mathf.Atan(tanHalfFov.y) * Mathf.Rad2Deg;

	}

	public void VR_render()
	{
		

		VR_textureLeftEye = new Texture_t();
		VR_textureRightEye = new Texture_t();
		VR_textureLeftEye.handle = renderTextureLeftEye.GetNativeTexturePtr();
		VR_textureRightEye.handle = renderTextureRightEye.GetNativeTexturePtr();

		//texture.eType = SteamVR.instance.textureType;
		VR_textureLeftEye.eColorSpace = EColorSpace.Auto;
		VR_textureRightEye.eColorSpace = EColorSpace.Auto;


		if (!SteamVR.active && !SteamVR.usingNativeSupport)
		{

			if (OpenVR.Compositor.CanRenderScene())
			{

			
				OpenVR.Compositor.Submit(EVREye.Eye_Left, ref VR_textureLeftEye, ref textureBounds[0], EVRSubmitFlags.Submit_Default);
				OpenVR.Compositor.Submit(EVREye.Eye_Right, ref VR_textureRightEye, ref textureBounds[1], EVRSubmitFlags.Submit_Default);
				OpenVR.Compositor.WaitGetPoses(renderPoseArray, gamePoseArray);
			}
		}
	}
}

