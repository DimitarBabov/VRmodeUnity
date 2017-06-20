﻿#if UNITY_EDITOR
using UnityEngine;


	class EditingContextManagerSettings : ScriptableObject
	{
		[SerializeField]
		string m_DefaultContextName;

		public string defaultContextName
		{
			get { return m_DefaultContextName; }
			set { m_DefaultContextName = value; }
		}
	}
#endif