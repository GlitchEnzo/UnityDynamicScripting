#define TIMING
//#define DEBUG

namespace DynamicScripting
{
	using System;
	using Mono.CSharp;
	using UnityEngine;
	using System.Collections.Generic;
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

		private string className;

		/// <summary>
		/// Gets the list of game objects that this script is attached to.
		/// </summary>
		/// <value>The game objects.</value>
		private List<GameObject> gameObjects; 
		
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

		private Type scriptType;

#if TIMING
		/// <summary>
		/// Used to measure the timing of various parts of the dynamic scripting.
		/// </summary>
		private Stopwatch stopwatch = new Stopwatch();
#endif

		public DynamicScript(string filepath)
		{
			Filepath = filepath;

			gameObjects = new List<GameObject>();

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
			className = code.Substring(indexOfClass, indexOfColon - indexOfClass).Trim();
#if DEBUG
			Debug.Log(string.Format("Compiling {0}", className));
#endif
			
			evaluator.Compile(code);

			scriptType = (Type)evaluator.Evaluate(string.Format("typeof({0});", className));

			//Debug.Log ("Type = " + scriptType);
			//Debug.Log ("Is Class = " + scriptType.IsClass);
			Debug.Log ("Assembly = " + scriptType.Assembly);
			foreach (var type in scriptType.Assembly.GetTypes()) 
			{
				Debug.Log ("Assembly Type = " + type);
			}
//			foreach (var type in scriptType.GetNestedTypes()) 
//			{
//				Debug.Log ("Type = " + type);
//			}
			foreach (var module in scriptType.Assembly.GetModules()) 
			{
				Debug.Log("Module: " + module.Name);
				foreach (var type in module.GetTypes())
				{
					Debug.Log("Module Type = " + type.FullName);
				}
			}
//			foreach (var member in scriptType.GetMembers()) 
//			{
//				Debug.Log ("Member = " + member);
//            }

			AttachAll();	
            
#if TIMING
            stopwatch.Stop();
			Debug.Log(string.Format("Compiling {0} took {1} ms", className, stopwatch.ElapsedMilliseconds));
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
				
				DetachAll();
				CompileScript();
			}
			
#if DEBUG
			// refresh the script
			if (Input.GetKeyDown(KeyCode.F5))
			{
				DetachAll();
				CompileScript();
			}
			
			// remove the script
            if (Input.GetKeyDown(KeyCode.Delete))
            {
				DetachAll();
            }
            
            // add the script
            if (Input.GetKeyDown(KeyCode.Insert))
            {
                //CompileScript();
				AttachAll();
            }
#endif
        }

		private void AttachAll()
		{
			foreach (var gameObject in gameObjects) 
			{
				evaluator.Run("GameObject.Find(\"" + gameObject.name + "\").AddComponent<" + className + ">();");
			}
		}

		private void DetachAll()
		{
			foreach (var gameObject in gameObjects) 
			{
				evaluator.Run("GameObject.Destroy(GameObject.Find(\"" + gameObject.name + "\").GetComponent<" + className + ">());");
            }
        }

		public void Attach(GameObject gameObject)
		{
			if (!gameObjects.Contains(gameObject)) 
			{
				gameObjects.Add(gameObject);
			}

			//gameObject.AddComponent(scriptType);

			evaluator.Run("GameObject.Find(\"" + gameObject.name + "\").AddComponent<" + className + ">();");
		}
		
		public void Detach(GameObject gameObject)
		{
			if (gameObjects.Contains(gameObject)) 
			{
				gameObjects.Remove(gameObject);
            }

			evaluator.Run("GameObject.Destroy(GameObject.Find(\"" + gameObject.name + "\").GetComponent<" + className + ">());");
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