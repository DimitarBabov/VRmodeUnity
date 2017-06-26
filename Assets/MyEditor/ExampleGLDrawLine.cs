/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class ExampleGLDrawLine : MonoBehaviour
{
	private Material mat;
	private Shader shader;
	private Vector3 startVertex;
	private Vector3 mousePos;

	
	private void Start()
	{
		shader = new Shader();
		shader = Shader.Find("Unlit/Texture");
		mat = new Material(shader);
		mat.color = Color.black;
	}

	void Update()
	{
		

	}
	private void OnDrawGizmos()
	{
		Gizmos.DrawCube(Vector3.zero, Vector3.one);
	}
	void OnPostRender()
	{
		
			
			
			if (!mat)
		{
			Debug.LogError("Please Assign a material on the inspector");
			return;
		}
		GL.PushMatrix();
		mat.SetPass(0);
		GL.LoadOrtho();
		GL.Begin(GL.LINES);
		GL.Color(Color.red);
		GL.Vertex(new Vector3(0.4f,0.4f,1f));
		GL.Vertex(Vector3.zero);
		GL.End();
		GL.PopMatrix();
	}
	
}
*/