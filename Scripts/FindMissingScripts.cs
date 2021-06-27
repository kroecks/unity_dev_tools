﻿using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

public class FindMissingScriptsWindow : EditorWindow
{
    public static Queue<GameObject> scheduledObjects = new Queue<GameObject>();

    public static HashSet<GameObject> found = new HashSet<GameObject>();
    
    [MenuItem("Window/Dev Tools/Find Missing Scripts Window")]
    public static void ShowWindow()
    {
        GetWindow(typeof(FindMissingScriptsWindow));
    }
 
    public void OnGUI()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Find Missing Scripts"))
        {
            EditorCoroutineUtility.StartCoroutine(FindInAll(), this);
        }
            
        if (found != null && found.Count > 0 && GUILayout.Button("Remove All"))
        {
            EditorCoroutineUtility.StartCoroutine(RemoveFromAll(), this);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (scheduledObjects.Count > 0)
        {
            GUILayout.BeginVertical();
            GUILayout.Label($"-------------------------------------");
            GUILayout.Label($"Searching! - QueueSize: " + scheduledObjects.Count);
            GUILayout.Label($"-------------------------------------");
            GUILayout.EndVertical();
        }
        
        {
            GUILayout.BeginVertical();

            GUILayout.Label($"-------------------------------------");
            GUILayout.Label($"Found Objects: " + found.Count);
            GUILayout.Label($"-------------------------------------");
            GUILayout.EndVertical();
            
            

        }
        GUILayout.EndHorizontal();

        
        if (found != null)
        {
            // Can't remove from our map while we're iterating over it
            Queue<GameObject> pendingRemoval = new Queue<GameObject>();
            
            foreach (var go in found)
            {
                GUILayout.BeginHorizontal();
                string name = go != null ? go.name : "MISSING";
                GUILayout.Label(name);
                if (GUILayout.Button("Select"))
                {
                    Selection.activeObject = go;
                }

                if (GUILayout.Button("Remove"))
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    pendingRemoval.Enqueue(go);
                }
                GUILayout.EndHorizontal();
            }

            while (pendingRemoval.Count > 0)
            {
                found.Remove(pendingRemoval.Dequeue());
            }
        }
    }
    
    public IEnumerator FindInAll()
    {
        found = new HashSet<GameObject>();
        
        var foundObjects = Resources.LoadAll<GameObject>("/");
        foreach (var go in foundObjects)
        {
            scheduledObjects.Enqueue(go);
        }

        while (scheduledObjects.Count > 0)
        {
            var pending = scheduledObjects.Dequeue();
            FindInGO(pending);
            yield return new WaitForEndOfFrame();
        }
    }

    public IEnumerator RemoveFromAll()
    {
        Queue<GameObject> localQueue = new Queue<GameObject>();
        foreach (var go in found)
        {
            localQueue.Enqueue(go);
        }
        
        while (localQueue.Count > 0)
        {
            var current = localQueue.Dequeue();
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(current);
            yield return new WaitForEndOfFrame();
        }
    }
 
    private static void FindInGO(GameObject g)
    {
        Component[] components = g.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                if (!found.Contains(g))
                    found.Add(g);
            }
        }

        int childCount = g.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            FindInGO(g.transform.GetChild(i).gameObject);
        }
    }
}