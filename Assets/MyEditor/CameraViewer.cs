// Simple script that lets you render the main camera in an editor Window.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Valve.VR;

[InitializeOnLoad]
public class CameraViewer : EditorWindow
{

	static Camera vr_camera;
	RenderTexture renderTexture;
	DrawCameraMode m_RenderMode = DrawCameraMode.Textured;
	public VRTextureBounds_t[] textureBounds { get; private set; }

	
	public CVRCompositor compositor { get; private set; }
	public CVROverlay overlay { get; private set; }

	[MenuItem("Example/CameraViewer")]
	
	
	
	public void Awake()
	{
		renderTexture = new RenderTexture((int)position.width,
				(int)position.height,
				(int)RenderTextureFormat.ARGB32);
	}

	public void Update()
	{
		if (vr_camera != null)
		{
			vr_camera.targetTexture = renderTexture;
			vr_camera.Render();
			vr_camera.targetTexture = null;
		}
		if (renderTexture.width != position.width ||
			renderTexture.height != position.height)
			renderTexture = new RenderTexture((int)position.width,
					(int)position.height,
					(int)RenderTextureFormat.ARGB32);
	}

	void OnGUI()
	{
		GUI.DrawTexture(new Rect(0.0f, 0.0f, position.width, position.height), renderTexture);
	}


	public void VR_render(Camera vr_cam)
	{
		TrackedDevicePose_t[] renderPoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
		TrackedDevicePose_t[] gamePoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];


		var texture = new Texture_t();

		

		EditorWindow editorWindow = GetWindow(typeof(CameraViewer));
		editorWindow.autoRepaintOnSceneChange = true;
		editorWindow.Show();

		var initOpenVR = (!SteamVR.active && !SteamVR.usingNativeSupport);

		var error = EVRInitError.None;
		CVRSystem hmd = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);
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




		if (initOpenVR)
		{

			if (OpenVR.Compositor.CanRenderScene())
			{

				OpenVR.Compositor.WaitGetPoses(renderPoseArray, gamePoseArray);
			

				texture.handle = vr_cam.activeTexture.GetNativeTexturePtr();

				texture.eType = SteamVR.instance.textureType;
				texture.eColorSpace = EColorSpace.Auto;
				
				OpenVR.Compositor.Submit(EVREye.Eye_Left, ref texture, ref textureBounds[0], EVRSubmitFlags.Submit_Default);
				OpenVR.Compositor.Submit(EVREye.Eye_Right, ref texture, ref textureBounds[1], EVRSubmitFlags.Submit_Default);
			}
		}

	}

	void VR_init()
	{

	}
}