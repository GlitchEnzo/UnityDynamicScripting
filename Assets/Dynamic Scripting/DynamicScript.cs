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

	public class DynamicScript
	{
		/// <summary>
		/// Gets the full filepath to the C# script file..
		/// </summary>
		public string Filepath { get; private set; }

		private string gameObjectName;

		/// <summary>
		/// The name of the MonoBehaviour contained in the script file.
		/// </summary>
		private string behaviourName;
		
		/// <summary>
		/// The C# compiler as a service used to compile the script file.
		/// </summary>
		private Evaluator evaluator;
		
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

		public DynamicScript(string filepath, string gameObjectName)
		{
			Filepath = filepath;
			this.gameObjectName = gameObjectName;

			// Set up file watcher
			int index = filepath.LastIndexOf("/");
			watcher = new FileSystemWatcher(filepath.Substring(0, index), filepath.Substring(index + 1));
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
		private void CompileScript()
		{
#if TIMING
			stopwatch.Reset();
			stopwatch.Start();
#endif

			string code = System.IO.File.ReadAllText(Filepath);
			//string code = script.text;
			
			// TODO: Find the namespace name, if there is one
			
			// Find the behaviour class name
			int indexOfClass = code.IndexOf("class") + 5;
			int indexOfColon = code.IndexOf(":");
			behaviourName = code.Substring(indexOfClass, indexOfColon - indexOfClass).Trim();
#if DEBUG
			Debug.Log("Class name: " + behaviourName);
#endif
			
			evaluator.Compile(code);
			evaluator.Run("GameObject.Find(\"" + gameObjectName + "\").AddComponent<" + behaviourName + ">();");	
            
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
#if DEBUG
				Debug.Log("Loading: " + assembly);
#endif
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
		public void Update() 
		{
			if (changed)
			{
				changed = false;
				
				evaluator.Run("GameObject.Destroy(GameObject.Find(\"" + gameObjectName + "\").GetComponent<" + behaviourName + ">());");
				CompileScript();
			}
			
#if DEBUG
			// refresh the script
			if (Input.GetKeyDown(KeyCode.F5))
			{
				evaluator.Run("GameObject.Destroy(GameObject.Find(\"" + gameObjectName + "\").GetComponent<" + behaviourName + ">());");
				CompileScript();
			}
			
			// remove the script
            if (Input.GetKeyDown(KeyCode.Delete))
            {
				evaluator.Run("GameObject.Destroy(GameObject.Find(\"" + gameObjectName + "\").GetComponent<" + behaviourName + ">());");
            }
            
            // add the script
            if (Input.GetKeyDown(KeyCode.Insert))
            {
                CompileScript();
            }
#endif
        }
        
        /// <summary>
        /// Unsubscribes the file watcher.
        /// </summary>
        public void StopWatching()
        {
            if (watcher != null)
            {
                watcher.Changed -= FileChanged;
            }
        }
	}
}