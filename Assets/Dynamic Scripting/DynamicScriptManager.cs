namespace DynamicScripting
{
	using System;
	using Mono.CSharp;
	using UnityEngine;
	using System.Collections.Generic;
	using System.IO;

	/// <summary>
	/// Represents a set of dynamic C# scripts to be attached to the same GameObject.
	/// </summary>
	public class DynamicScriptManager : MonoBehaviour 
	{
		/// <summary>
		/// The script files to be used. (Dragged and dropped in the Unity Editor.)
		/// Unity supports: .txt, .html, .htm, .xml, .bytes, .json, .csv, .yaml, .fnt
		/// </summary>
		public List<TextAsset> scriptAssets;

		/// <summary>
		/// The list of loaded and compiled scripts that the manager cares about.
		/// </summary>
		private List<DynamicScript> scripts;

		/// <summary>
		/// All of the currently loaded and compiled scripts, across ALL managers.
		/// </summary>
		private static Dictionary<string, DynamicScript> allScripts = new Dictionary<string, DynamicScript>();

		/// <summary>
		/// Compiles the scripts for the first time and sets up the file watchers.
		/// </summary>
		void Start() 
		{
			scripts = new List<DynamicScript>();

			foreach (var asset in scriptAssets) 
			{
				// TODO: Use the filepath to the asset instead of the name.
				DynamicScript script;
				if (allScripts.ContainsKey(asset.name))
				{
					script = allScripts[asset.name];
				}
				else
				{
					// TODO: Get filepath from Test Asset (if possible).  For now, assume it is in the StreamingAssets folder
					script = new DynamicScript(Application.streamingAssetsPath + "/" + asset.name + ".txt");
					allScripts.Add(asset.name, script);
				}

				scripts.Add(script);
				
				script.Attach(this.gameObject);
			}
		}

		/// <summary>
		/// Recompiles the scripts if there were any detected changes.
		/// </summary>
		void Update() 
		{
			foreach (var script in scripts) 
			{
				script.Update();
			}
		}

		/// <summary>
		/// Unsubscribes the file watchers when the component is destroyed.
		/// </summary>
		void OnDestroy()
		{
			foreach (var script in scripts) 
			{
				script.StopWatching();
				script.Detach(this.gameObject);
			}
		}
	}
}