
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DrawLine))]
public class DrawLineEditor : Editor
{
	// draw lines between a chosen game object
	// and a selection of added game objects
	

	void OnSceneGUI()
	{
		
		
		// get the chosen game object
		DrawLine t = target as DrawLine;

		if (t == null || t.gameObjects == null)
			return;

		// grab the center of the parent
		Vector3 center = t.transform.position;

		// iterate over game objects added to the array...
		for (int i = 0; i < t.gameObjects.Length; i++)
		{
			// ... and draw a line between them
			if (t.gameObjects[i] != null)
				Handles.DrawLine(center, t.gameObjects[i].transform.position);
			//UnityEditor.Handles.SetCamera
		}
	}
}