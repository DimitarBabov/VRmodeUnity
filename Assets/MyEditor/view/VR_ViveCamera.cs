using UnityEngine;
using System.Collections;
using Valve.VR;
using System;
using System.Runtime.InteropServices;

public class VR_ViveCamera
{

	public uint index;
	public GameObject monitor;
	CVRTrackedCamera trcam_instance = null;
	ulong pTrackedCamera = 0;
	IntPtr pBuffer = (IntPtr)null;
	byte[] buffer = null;
	uint buffsize = 0;
	public Texture2D texture = null;
	CameraVideoStreamFrameHeader_t pFrameHeader;
	EVRTrackedCameraError camerror = EVRTrackedCameraError.None;
	uint prevFrameSequence = 0;

	public void Create () {
		uint width = 0, height = 0;
		bool pHasCamera = false;

		trcam_instance = OpenVR.TrackedCamera;

		if(trcam_instance == null) {
			Debug.LogError("Error getting TrackedCamera");
		} else {			
			camerror = trcam_instance.HasCamera ((uint)index, ref pHasCamera);
			if(camerror != EVRTrackedCameraError.None) {
				Debug.LogError("HasCamera: EVRTrackedCameraError="+camerror);
				return;
			}
			if (pHasCamera) {
				camerror = trcam_instance.GetCameraFrameSize ((uint)index, EVRTrackedCameraFrameType.MaximumUndistorted, ref width, ref height, ref buffsize);
				if (camerror != EVRTrackedCameraError.None) {
					Debug.LogError("GetCameraFrameSize: EVRTrackedCameraError=" + camerror);
				} else {
					Debug.Log("width=" + width + " height=" + height + " buffsize=" + buffsize);
					texture = new Texture2D ((int)width, (int)height, TextureFormat.RGBA32, false);

					buffer = new byte[buffsize];
					pBuffer = Marshal.AllocHGlobal ((int)buffsize);

					camerror = trcam_instance.AcquireVideoStreamingService ((uint)index, ref pTrackedCamera);
					if (camerror != EVRTrackedCameraError.None) {
						Debug.LogError("AcquireVideoStreamingService: EVRTrackedCameraError=" + camerror);
					}
				}
			} else {
				Debug.Log("no camera found, only Vive Pre and later supported");
			}
		}
	}

	public void Update () {
		// first get header only
		if (trcam_instance == null)
			return;
		camerror = trcam_instance.GetVideoStreamFrameBuffer(pTrackedCamera,  EVRTrackedCameraFrameType.Undistorted, (IntPtr)null, 0, ref pFrameHeader, (uint)Marshal.SizeOf(typeof(CameraVideoStreamFrameHeader_t)));
		if(camerror != EVRTrackedCameraError.None) {
//			Debug.LogError("GetVideoStreamFrameBuffer: EVRTrackedCameraError="+camerror);
			return;
		}
		//if frame hasn't changed don't copy buffer
		if (pFrameHeader.nFrameSequence == prevFrameSequence) {
			return;
		}
		// now get header and buffer
		camerror = trcam_instance.GetVideoStreamFrameBuffer(pTrackedCamera,  EVRTrackedCameraFrameType.Undistorted, pBuffer, buffsize, ref pFrameHeader, (uint)Marshal.SizeOf(typeof(CameraVideoStreamFrameHeader_t)));
		if(camerror != EVRTrackedCameraError.None) {
			Debug.LogError("GetVideoStreamFrameBuffer: EVRTrackedCameraError="+camerror);
			return;
		}
		prevFrameSequence = pFrameHeader.nFrameSequence;

		Marshal.Copy(pBuffer, buffer, 0, (int)buffsize);
		texture.LoadRawTextureData(buffer);
		texture.Apply();
	}

	public void Destroy() {
		if (pTrackedCamera != 0) {
			trcam_instance.ReleaseVideoStreamingService(pTrackedCamera);
		}
	}
}
