// Starts the default camera and assigns the texture to the current renderer
using UnityEngine;
using UnityEditor;
using System.Collections;

//[InitializeOnLoad]
[ExecuteInEditMode]
public class xxx :MonoBehaviour
{ 
	

	WebCamTexture webcamTexture;

	void Start()
	{   

		webcamTexture = new WebCamTexture("Logitech HD Pro Webcam C920");
		webcamTexture.Play(); 
		
		//Renderer renderer =GetComponent<Renderer>();
		//renderer.sharedMaterial.mainTexture = webcamTexture;

		//EditorApplication.update += update_cam;
	}
	private void Update()
	{
		if (webcamTexture.isPlaying == false)
			webcamTexture.Play();

	}
	//private void update_cam()
	//{
		//GetComponent<Renderer>().sharedMaterial.mainTexture = webcamTexture;
	//}

	private void OnApplicationQuit()
	{
		if (webcamTexture.isPlaying == true)
			webcamTexture.Stop();
	}

	private void OnDisable()
	{
		if(webcamTexture.isPlaying ==true)
			webcamTexture.Stop();
	}

}