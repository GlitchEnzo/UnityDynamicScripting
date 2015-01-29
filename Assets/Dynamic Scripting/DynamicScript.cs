////#define TIMING
////#define DEBUG

namespace DynamicScripting
{
    using System;
    using System.Collections.Generic;
#if TIMING
    using System.Diagnostics;
#endif
    using System.IO;
    using Mono.CSharp;
    using UnityEngine;

#if TIMING
    using Debug = UnityEngine.Debug;
#endif

    /// <summary>
    /// Represents a loaded and compiled C# script.  It maintains a file watcher and automatically recompiles when the file changes.
    /// </summary>
    public class DynamicScript
    {
        /// <summary>
        /// The name of the class contained in the C# script file.
        /// </summary>
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
        private bool changed;

        /// <summary>
        /// The <see cref="Type"/> of the loaded and compiled C# script class.
        /// </summary>
        private Type scriptType;

#if TIMING
        /// <summary>
        /// Used to measure the timing of various parts of the dynamic scripting.
        /// </summary>
        private Stopwatch stopwatch = new Stopwatch();
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicScript"/> class.
        /// </summary>
        /// <param name="filepath">The full filepath to the C# script file.</param>
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
        /// Gets the full filepath to the C# script file..
        /// </summary>
        public string Filepath { get; private set; }

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
                ////CompileScript();
                AttachAll();
            }
#endif
        }

        /// <summary>
        /// Adds the C# script as a component to the given game object and adds the game object to an internal list.
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to attach the C# script to.</param>
        public void Attach(GameObject gameObject)
        {
            if (!gameObjects.Contains(gameObject))
            {
                gameObjects.Add(gameObject);
            }

            ////gameObject.AddComponent(scriptType);

            evaluator.Run("GameObject.Find(\"" + gameObject.name + "\").AddComponent<" + className + ">();");
        }

        /// <summary>
        /// Removes the C# script from the given game object and removes the game object from an internal list.
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to remove the C# script from.</param>
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
            ////string code = script.text;

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

            ////Debug.Log("Script Type = " + scriptType);
            ////Debug.Log("BaseType = " + scriptType.BaseType);
            ////Debug.Log("BaseType.BaseType = " + scriptType.BaseType.BaseType);

            ////Debug.Log("Assembly = " + scriptType.Assembly);

            ////Debug.Log("Declaring Type = " + scriptType.DeclaringType);

            ////Debug.Log(scriptType.GetNestedTypes().Length);

            ////foreach (var type in scriptType.Assembly.GetTypes())
            ////{
            ////    Debug.Log("Assembly Type = " + type);
            ////}

            ////foreach (var module in scriptType.Assembly.GetModules())
            ////{
            ////    foreach (var type in module.GetTypes())
            ////    {
            ////        Debug.Log("Module Type = " + type.FullName);
            ////    }
            ////}

            ////foreach (var member in scriptType.GetMembers()) 
            ////{
            ////  Debug.Log ("Member = " + member);
            ////}

            AttachAll();

#if TIMING
            stopwatch.Stop();
            Debug.Log(string.Format("Compiling {0} took {1} ms", className, stopwatch.ElapsedMilliseconds));
#endif
        }

        /// <summary>
        /// Loads all of the currently loaded/referenced assemblies in order to allow the script to access them.
        /// </summary>
        private void LoadAllAssemblies()
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
        private void FileChanged(object sender, FileSystemEventArgs e)
        {
#if DEBUG
            Debug.Log(string.Format("{0}: {1}", e.ChangeType, e.FullPath));
#endif
            changed = true;
        }

        /// <summary>
        /// Add the C# script as a component to the entire list of game objects.
        /// </summary>
        private void AttachAll()
        {
            foreach (var gameObject in gameObjects)
            {
                evaluator.Run("GameObject.Find(\"" + gameObject.name + "\").AddComponent<" + className + ">();");
            }
        }

        /// <summary>
        /// Remove the C# script from the entire list of game objects.
        /// </summary>
        private void DetachAll()
        {
            foreach (var gameObject in gameObjects)
            {
                evaluator.Run("GameObject.Destroy(GameObject.Find(\"" + gameObject.name + "\").GetComponent<" + className + ">());");
            }
        }
    }
}