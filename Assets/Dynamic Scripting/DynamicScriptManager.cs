namespace DynamicScripting
{
	using System;
	using Mono.CSharp;
	using UnityEngine;
	using System.Collections;
	using System.IO;

	/// <summary>
	/// Represents a dynamic C# script.
	/// </summary>
	public class DynamicScriptManager : MonoBehaviour 
	{
		/// <summary>
		/// The script file to be used. (Dragged and dropped in the Unity Editor.)
		/// Unity supports: .txt, .html, .htm, .xml, .bytes, .json, .csv, .yaml, .fnt
		/// </summary>
		public TextAsset scriptAsset;

		//public string filepath;

		DynamicScript script;

		/// <summary>
		/// Compiles the script for the first time and sets up the file watcher.
		/// </summary>
		void Start() 
		{
			// TODO: Get filepath from Test Asset (if possible)
			script = new DynamicScript(Application.streamingAssetsPath + "/" + scriptAsset.name + ".txt", this.name);
		}

		/// <summary>
		/// Recompiles the script if there were any detected changes.
		/// </summary>
		void Update() 
		{
			if (script != null) 
			{
				script.Update ();
			}
		}

		/// <summary>
		/// Unsubscribes the file watcher when the component is destroyed.
		/// </summary>
		void OnDestroy()
		{
			if (script != null) 
			{
				script.StopWatching ();
			}
		}
	}
}
