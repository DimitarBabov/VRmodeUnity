using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class VR_WebCam : MonoBehaviour {

	public WebCamTexture webcam_texture;
	

	void Start()
	{

		webcam_texture = new WebCamTexture("Logitech HD Pro Webcam C920", 1920, 1080);
		webcam_texture.Play();
		
	}
	

	private void OnApplicationQuit()
	{
		if (webcam_texture.isPlaying == true)
			webcam_texture.Stop();
	}

	private void OnDisable()
	{
		if (webcam_texture.isPlaying == true)
			webcam_texture.Stop();
	}
	
}
