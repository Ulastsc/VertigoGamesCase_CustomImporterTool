using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEditorInternal;

public class CustomImporterTool : EditorWindow
{
    private string targetFolderPath = "";
    private string externalFolderPath = "";
    private string findString = "";
    private string replaceString = "";
    private List<string> externalFilesList = new List<string>();
    private bool targetFolderFoldout = true;
    private bool externalFilesFoldout = false;
    private bool namingFoldout = true;
    private ReorderableList reorderableList;
    private Vector2 scrollPos;

    [MenuItem("Tools/Custom Importer Tool")]
    public static void ShowWindow()
    {
        GetWindow<CustomImporterTool>("Custom Importer Tool");
    }

    private void OnEnable()
    {
        // Create a ReorderableList
        reorderableList = new ReorderableList(externalFilesList, typeof(string), true, true, false, false);

        // List Header
        reorderableList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "External Files");
        };

        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            if (index >= 0 && index < externalFilesList.Count)
            {
                var filePath = externalFilesList[index];
                string fileName = Path.GetFileName(filePath);

                // Manuel file name editing
                EditorGUI.BeginChangeCheck();
                fileName = EditorGUI.TextField(new Rect(rect.x, rect.y, rect.width - 60, EditorGUIUtility.singleLineHeight), fileName);

                // If the user makes a change the list updated
                if (EditorGUI.EndChangeCheck())
                {
                    externalFilesList[index] = Path.Combine(Path.GetDirectoryName(filePath), fileName);
                }

                if (GUI.Button(new Rect(rect.x + rect.width - 55, rect.y, 50, EditorGUIUtility.singleLineHeight), "X"))
                {
                    externalFilesList.RemoveAt(index);
                }
            }
        };
    }

    private void OnGUI()
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        GUIStyle textFieldStyle = new GUIStyle(EditorStyles.textField) { alignment = TextAnchor.MiddleLeft };
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fixedHeight = 40 };

        targetFolderFoldout = EditorGUILayout.Foldout(targetFolderFoldout, "Target Folder", true);
        if (targetFolderFoldout)
        {
            EditorGUILayout.BeginVertical("box");
            var draggedFolder = (DefaultAsset)EditorGUILayout.ObjectField("Target Folder", AssetDatabase.LoadAssetAtPath<DefaultAsset>(targetFolderPath), typeof(DefaultAsset), false);
            if (draggedFolder != null)
            {
                targetFolderPath = AssetDatabase.GetAssetPath(draggedFolder);
            }
            EditorGUILayout.EndVertical();
        }

        GUILayout.Space(10);

        // External Folder Path TextField moved here
        EditorGUILayout.BeginVertical("box");
        externalFolderPath = EditorGUILayout.TextField("External Assets Path", externalFolderPath, textFieldStyle);
        EditorGUILayout.EndVertical();



        externalFilesFoldout = EditorGUILayout.Foldout(externalFilesFoldout, "External Files", true);
        if (externalFilesFoldout)
        {
            EditorGUILayout.BeginVertical("box");
            if (externalFilesList.Count > 0)
            {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
                reorderableList.DoLayoutList();
                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("No files found", EditorStyles.helpBox);
            }
            EditorGUILayout.EndVertical();

        }
        // Select Folder Button
        if (GUILayout.Button("Pick Assets Folder", buttonStyle))
        {
            externalFolderPath = EditorUtility.OpenFolderPanel("Select External Assets Folder", "", "");
            LoadExternalFiles();
        }

        GUILayout.Space(10);

        namingFoldout = EditorGUILayout.Foldout(namingFoldout, "Naming (Find & Replace)", true);
        if (namingFoldout)
        {
            EditorGUILayout.BeginVertical("box");
            findString = EditorGUILayout.TextField("Find", findString, textFieldStyle);
            replaceString = EditorGUILayout.TextField("Replace", replaceString, textFieldStyle);
            EditorGUILayout.EndVertical();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Import", buttonStyle))
        {
            ProcessAssets();
        }


    }

    private void LoadExternalFiles()
    {
        externalFilesList.Clear();
        if (string.IsNullOrEmpty(externalFolderPath))
        {
            return;
        }

        try
        {
            string[] files = Directory.GetFiles(externalFolderPath, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                externalFilesList.Add(file);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading files from {externalFolderPath}: {ex.Message}");
        }
    }

    private void ProcessAssets()
    {
        if (string.IsNullOrEmpty(targetFolderPath) || externalFilesList.Count == 0)
        {
            Debug.LogError("Folders not selected");
            return;
        }

        string targetFolderName = Path.GetFileName(targetFolderPath);
        string newTargetFolderName = targetFolderName;

        if (!string.IsNullOrEmpty(findString) && targetFolderName.Contains(findString))
        {
            newTargetFolderName = targetFolderName.Replace(findString, replaceString);
        }

        string newFolderPath = Path.Combine(Path.GetDirectoryName(targetFolderPath), newTargetFolderName);

        if (Directory.Exists(newFolderPath))
        {
            Debug.LogError("A folder with the same name already exists.");
            return;
        }

        try
        {
            DirectoryCopy(targetFolderPath, newFolderPath, true);
            RenameAssetsInFolder(newFolderPath);
            MoveExternalAssets(newFolderPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during import process: {ex.Message}");
        }
    }

    private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        Directory.CreateDirectory(destDirName);
        string[] files = Directory.GetFiles(sourceDirName);
        foreach (string file in files)
        {
            string destFile = Path.Combine(destDirName, Path.GetFileName(file));
            if (!File.Exists(destFile))
            {
                File.Copy(file, destFile, true);
            }
        }

        if (copySubDirs)
        {
            string[] subDirs = Directory.GetDirectories(sourceDirName);
            foreach (string subDir in subDirs)
            {
                string destSubDir = Path.Combine(destDirName, Path.GetFileName(subDir));
                DirectoryCopy(subDir, destSubDir, copySubDirs);
            }
        }
    }

    private void MoveExternalAssets(string targetFolder)
    {
        foreach (string externalFile in externalFilesList)
        {
            string fileName = Path.GetFileName(externalFile);
            string targetFilePath = Path.Combine(targetFolder, fileName);
            if (!File.Exists(targetFilePath))
            {
                File.Copy(externalFile, targetFilePath, true);
            }
        }

        AssetDatabase.Refresh();
    }

    private void RenameAssetsInFolder(string folderPath)
    {
        var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            if (file.Contains(findString))
            {
                string newFileName = file.Replace(findString, replaceString);
                if (!File.Exists(newFileName))
                {
                    File.Move(file, newFileName);
                }
            }
        }

        AssetDatabase.Refresh();
    }
}
