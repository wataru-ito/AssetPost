using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace AssetPost
{
	/// <summary>
	/// このウィンドウにアセットをドロップすると、プロジェクトの適切な所に配置される。
	/// どこに配置されるかは AssetPostman が決める。
	/// </summary>
	public class AssetPostWindow : EditorWindow
	{
		List<AssetPostman> m_postmanList;
		
		GUIStyle m_messageStyle;
		GUIStyle m_labelStyle;
		List<string> m_assetPathList = new List<string>();
		List<string> m_unknownFileList = new List<string>();


		//------------------------------------------------------
		// static function
		//------------------------------------------------------

		[MenuItem("Tools/AssetPost")]
		public static void Open()
		{
			GetWindow<AssetPostWindow>();
		}


		//------------------------------------------------------
		// unity system function
		//------------------------------------------------------

		private void OnEnable()
		{
			titleContent = new GUIContent("AssetPost");
			minSize = new Vector2(330f, 100f);

			InitGUI();
			InitPostman();
		}

		private void OnGUI()
		{
			GUILayout.FlexibleSpace();
			
			EditorGUILayout.LabelField("ドロップされたアセットを\n適切な場所へ配置します。", m_messageStyle, GUILayout.Height(m_messageStyle.fontSize * 3f));

			GUILayout.FlexibleSpace();

			DrawDeliveryInfo();
			DeliveryDropFiles();
		}


		//------------------------------------------------------
		// postman
		//------------------------------------------------------

		void InitPostman()
		{
			m_postmanList = new List<AssetPostman>();
			var postmanType = typeof(AssetPostman);
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var postmans = assembly.GetTypes()
					.Where(i => i.IsSubclassOf(postmanType) && !i.IsAbstract)
					.Select(i => Activator.CreateInstance(i) as AssetPostman);
				m_postmanList.AddRange(postmans);
			}
		}


		//------------------------------------------------------
		// delivery
		//------------------------------------------------------

		void DeliveryDropFiles()
		{
			var controlID = EditorGUIUtility.GetControlID(FocusType.Passive);
			var e = Event.current;
			switch (e.type)
			{
				case EventType.DragUpdated:
				case EventType.DragPerform:
					DragAndDrop.AcceptDrag();
					DragAndDrop.activeControlID = controlID;
					DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
					e.Use();
					break;

				case EventType.DragExited:
					DeliveryFiles(DragAndDrop.paths);
					e.Use();
					break;
			}
		}

		void DeliveryFiles(string[] filePath)
		{
			m_assetPathList.Clear();
			m_unknownFileList.Clear();
			AssetDatabase.StartAssetEditing();
			Array.ForEach(filePath, DeliveryFile);
			AssetDatabase.StopAssetEditing();
			AssetDatabase.Refresh();
		}

		void DeliveryFile(string filePath)
		{
			var fileName = Path.GetFileName(filePath);
			foreach (var postman in m_postmanList)
			{
				var assetPath = postman.Delivery(fileName);
				if (!string.IsNullOrEmpty(assetPath))
				{
					try
					{
						var dir = Path.GetDirectoryName(assetPath);
						if (!Directory.Exists(dir))
							Directory.CreateDirectory(dir);
						
						File.Copy(filePath, assetPath, true);

						m_assetPathList.Add(assetPath);
					}
					catch (Exception e)
					{
						Debug.LogError(e.Message);
					}
					return;
				}
			}

			m_unknownFileList.Add(fileName);
		}

		//------------------------------------------------------
		// gui
		//------------------------------------------------------

		void InitGUI()
		{
			var skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
			m_messageStyle = skin.FindStyle("TL Selection H1");
			m_labelStyle = skin.FindStyle("PreToolbar");
		}

		void DrawDeliveryInfo()
		{
			if (m_assetPathList.Count > 0)
			{
				EditorGUILayout.LabelField("配達完了", m_labelStyle);
				m_assetPathList.ForEach(DrawDeliveredAssetPath);
			}

			if (m_unknownFileList.Count > 0)
			{
				EditorGUILayout.HelpBox("配達できなかったアセットがあります", MessageType.Warning);
				GUI.color = Color.yellow;
				m_unknownFileList.ForEach(DrawUnknownFileName);
				GUI.color = Color.white;
			}
		}

		void DrawDeliveredAssetPath(string assetPath)
		{
			EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath), typeof(UnityEngine.Object), false);
		}

		void DrawUnknownFileName(string fileName)
		{
			EditorGUILayout.LabelField(fileName);
		}
	}
}