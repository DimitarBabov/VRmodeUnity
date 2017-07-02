﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using Valve.VR;

public class VR_CameraRig : Object
{
	
	private CVRSystem hmd;
	

	TrackedDevicePose_t[] renderPoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
	TrackedDevicePose_t[] gamePoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

	VRTextureBounds_t[] textureBounds;

	Texture_t VR_textureLeftEye, VR_textureRightEye;
	GameObject Head, LeftEye, RightEye;
	RenderTexture renderTextureLeftEye;
	RenderTexture renderTextureRightEye;
	Camera leftEyeCam;
	Camera rightEyeCam;

	public void Create()
	{
		hmd = OpenVR.System;
		if (hmd == null)
		{
			Debug.LogError("OpenVR is not initialized");
			return;
		}

		Head = EditorUtility.CreateGameObjectWithHideFlags("VRHead", HideFlags.HideAndDontSave);

		LeftEye = EditorUtility.CreateGameObjectWithHideFlags("VRCameraLeftEye", HideFlags.HideAndDontSave, typeof(Camera));
		leftEyeCam = LeftEye.GetComponent<Camera>();
		leftEyeCam.cameraType = CameraType.SceneView;
		leftEyeCam.nearClipPlane = 0.01f;
		leftEyeCam.farClipPlane = 1000f;

		RightEye = EditorUtility.CreateGameObjectWithHideFlags("VRCameraRightEye", HideFlags.HideAndDontSave, typeof(Camera));
		rightEyeCam = RightEye.GetComponent<Camera>();
		rightEyeCam.cameraType = CameraType.SceneView;
		rightEyeCam.nearClipPlane = 0.01f;
		rightEyeCam.farClipPlane = 1000f;

		//Create left eye texture
		renderTextureLeftEye = new RenderTexture(1520, 1680, 24);
		renderTextureLeftEye.format = RenderTextureFormat.ARGB32;
		renderTextureLeftEye.antiAliasing = 2;
		renderTextureLeftEye.Create();

		leftEyeCam.targetTexture = renderTextureLeftEye;
		leftEyeCam.Render();

		//Create right eye texture
		renderTextureRightEye = new RenderTexture(1520, 1680, 24);
		renderTextureRightEye.format = RenderTextureFormat.ARGB32;
		renderTextureRightEye.antiAliasing = 2;
		renderTextureRightEye.Create();

		rightEyeCam.targetTexture = renderTextureRightEye;
		rightEyeCam.Render();
		//Initialize the transforms
		UpdateTransform();

	}

	public void Destroy()
	{
		if (LeftEye)			
			DestroyImmediate(LeftEye, true);

		if (RightEye)
			DestroyImmediate(RightEye, true);
	}


	public void UpdateTransform()
	{
		SteamVR_Utils.RigidTransform pose_head = new SteamVR_Utils.RigidTransform(renderPoseArray[0].mDeviceToAbsoluteTracking);
		SteamVR_Utils.RigidTransform pose_left_to_head = new SteamVR_Utils.RigidTransform(hmd.GetEyeToHeadTransform(EVREye.Eye_Left));
		SteamVR_Utils.RigidTransform pose_right_to_head = new SteamVR_Utils.RigidTransform(hmd.GetEyeToHeadTransform(EVREye.Eye_Right));

		leftEyeCam.transform.localPosition = pose_head.TransformPoint(pose_left_to_head.pos);
		leftEyeCam.transform.localRotation = pose_head.rot * pose_left_to_head.rot;

		rightEyeCam.transform.localPosition = pose_head.TransformPoint(pose_right_to_head.pos);
		rightEyeCam.transform.localRotation = pose_head.rot * pose_right_to_head.rot;

		Head.transform.localPosition = pose_head.pos;
		Head.transform.localRotation = pose_head.rot;

	}

	public void SetTexture()
	{
		renderPoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
		gamePoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
		VR_textureLeftEye = new Texture_t();
		VR_textureRightEye = new Texture_t();

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

		// Account for textures being upside-down in Unity.
		textureBounds[0].vMin = 1.0f - textureBounds[0].vMin;
		textureBounds[0].vMax = 1.0f - textureBounds[0].vMax;
		textureBounds[1].vMin = 1.0f - textureBounds[1].vMin;
		textureBounds[1].vMax = 1.0f - textureBounds[1].vMax;

		// Grow the recommended size to account for the overlapping fov
		sceneWidth = sceneWidth / Mathf.Max(textureBounds[0].uMax - textureBounds[0].uMin, textureBounds[1].uMax - textureBounds[1].uMin);
		sceneHeight = sceneHeight / Mathf.Max(textureBounds[0].vMax - textureBounds[0].vMin, textureBounds[1].vMax - textureBounds[1].vMin);

		float aspect = tanHalfFov.x / tanHalfFov.y;
		float fieldOfView = 2.0f * Mathf.Atan(tanHalfFov.y) * Mathf.Rad2Deg;
		leftEyeCam.aspect = aspect;
		rightEyeCam.aspect = aspect;
		leftEyeCam.fieldOfView = fieldOfView;
		rightEyeCam.fieldOfView = fieldOfView;
	}

	public void Render()
	{
		UpdateTransform();

		var compositor = OpenVR.Compositor;
		if(compositor == null)
		{
			Debug.LogError("OpenVR compositor is not initialized");
			return;
		}


		leftEyeCam.Render();
		rightEyeCam.Render();


		VR_textureLeftEye.handle = renderTextureLeftEye.GetNativeTexturePtr();
		VR_textureRightEye.handle = renderTextureRightEye.GetNativeTexturePtr();

		//texture.eType = SteamVR.instance.textureType;
		VR_textureLeftEye.eColorSpace = EColorSpace.Auto;
		VR_textureRightEye.eColorSpace = EColorSpace.Auto;


		if (!SteamVR.active && !SteamVR.usingNativeSupport)
		{

			if (compositor.CanRenderScene())
			{
				compositor.Submit(EVREye.Eye_Left, ref VR_textureLeftEye, ref textureBounds[0], EVRSubmitFlags.Submit_Default);
				compositor.Submit(EVREye.Eye_Right, ref VR_textureRightEye, ref textureBounds[1], EVRSubmitFlags.Submit_Default);
				compositor.WaitGetPoses(renderPoseArray, gamePoseArray);
			}
		}
	}
}
