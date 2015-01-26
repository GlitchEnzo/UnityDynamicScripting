#define TIMING
#define DEBUG

namespace DynamicScripting
{
	using System;
	using Mono.CSharp;
	using UnityEngine;
	using System.Collections;
	using System.IO;

#if TIMING
	using System.Diagnostics;
	using Debug = UnityEngine.Debug;
#endif

	/// <summary>
	/// Represents a dynamic C# script.
	/// </summary>
	public class DynamicScript : MonoBehaviour 
	{
		/// <summary>
		/// The script file to be used. (Dragged and dropped in the Unity Editor.)
		/// Unity supports: .txt, .html, .htm, .xml, .bytes, .json, .csv, .yaml, .fnt
		/// </summary>
		public TextAsset script;

		//public string filepath;

		/// <summary>
		/// The name of the MonoBehaviour contained in the script file.
		/// </summary>
		private string behaviourName;

		/// <summary>
		/// The C# compiler as a service used to compile the script file.
		/// </summary>
		private static Evaluator evaluator;

		/// <summary>
		/// The file watcher used to detect when the script file changes.
		/// </summary>
	    private FileSystemWatcher watcher;

		/// <summary>
		/// Flag that is marked true when a change in the script file is detected.
		/// </summary>
		private bool changed = false;

#if TIMING
		/// <summary>
		/// Used to measure the timing of various parts of the dynamic scripting.
		/// </summary>
		private Stopwatch stopwatch = new Stopwatch();
#endif

		/// <summary>
		/// Compiles the script for the first time and sets up the file watcher.
		/// </summary>
		void Start() 
		{
			// TODO: Get filepath from Test Asset (if possible)

			// Set up file watcher
	        watcher = new FileSystemWatcher(Application.streamingAssetsPath, script.name + ".txt");
			watcher.Changed -= FileChanged;
	        watcher.Changed += FileChanged;
	        watcher.EnableRaisingEvents = true;

			if (evaluator == null)
			{
				CompilerContext compilerContext = new CompilerContext(new CompilerSettings(), new UnityReportPrinter());
				evaluator = new Evaluator(compilerContext);
				LoadAllAssemblies();
			}

	        CompileScript();
		}

		/// <summary>
		/// Compiles the script and attaches it the current GameObject.
		/// </summary>
	    void CompileScript()
	    {
#if TIMING
			stopwatch.Reset();
			stopwatch.Start();
#endif

			//string code = System.IO.File.ReadAllText(filepath);
			string code = script.text;

	        // Find the behaviour class name
			int indexOfClass = code.IndexOf("class") + 5;
			int indexOfColon = code.IndexOf(":");
			behaviourName = code.Substring(indexOfClass, indexOfColon - indexOfClass).Trim();
			//Debug.Log(behaviourName);

	        evaluator.Compile(code);
	        evaluator.Run("GameObject.Find(\"" + this.name + "\").AddComponent<" + behaviourName + ">();");	

#if TIMING
			stopwatch.Stop();
			Debug.Log(string.Format("Compiling took {0} ms", stopwatch.ElapsedMilliseconds));
#endif
	    }

		/// <summary>
		/// Loads all of the currently loaded/referenced assemblies in order to allow the script to access them.
		/// </summary>
		void LoadAllAssemblies()
		{
#if TIMING
			stopwatch.Reset();
			stopwatch.Start();
#endif

			foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies()) 
			{
				//Debug.Log("Loading: " + assembly);
				evaluator.ReferenceAssembly(assembly);
			}

#if TIMING
			stopwatch.Stop();
			Debug.Log(string.Format("Loading assemblies took {0} ms", stopwatch.ElapsedMilliseconds));
#endif
		}

		/// <summary>
		/// Called whenever the script file changes.  The event fires on a separate thread from the Unity main thread
		/// so a flag must be set to recompile the code and reattach the behaviour on the main thread.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		/// <param name="e">The event arguments.</param>
	    void FileChanged(object sender, FileSystemEventArgs e)
	    {
#if DEBUG
	        Debug.Log(string.Format("{0}: {1}", e.ChangeType, e.FullPath));
#endif
			changed = true;
	    }

		/// <summary>
		/// Recompiles the script if there were any detected changes.
		/// </summary>
		void Update() 
		{
			if (changed)
			{
				changed = false;

				evaluator.Run("GameObject.Destroy(GameObject.Find(\"" + this.name + "\").GetComponent<" + behaviourName + ">());");
				CompileScript();
			}

#if DEBUG
			// refresh the script
			if (Input.GetKeyDown(KeyCode.F5))
			{
				evaluator.Run("GameObject.Destroy(GameObject.Find(\"" + this.name + "\").GetComponent<" + behaviourName + ">());");
				CompileScript();
			}

			// remove the script
			if (Input.GetKeyDown(KeyCode.Delete))
			{
			    Component scriptComponent = gameObject.GetComponent(behaviourName);
				Debug.Log(scriptComponent == null);
	            evaluator.Run("GameObject.Destroy(GameObject.Find(\"" + this.name + "\").GetComponent<" + behaviourName + ">());");
			}

			// add the script
	        if (Input.GetKeyDown(KeyCode.Insert))
	        {
	            CompileScript();
	        }
#endif
		}

		/// <summary>
		/// Unsubscribes the file watcher when the component is destroyed.
		/// </summary>
		void OnDestroy()
		{
			if (watcher != null)
			{
				// unsubscribe the file watcher
				watcher.Changed -= FileChanged;
			}
		}
	}
}
