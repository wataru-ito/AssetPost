using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
		enum Mode
		{
			Delivery,
			Addressbook,
			RegisterAddress,
		}

		AssetPostAddressBook m_adressbook;
		AssetPostman[] m_postmans;
		List<string> m_assetPathList = new List<string>();
		List<string> m_unknownFileList = new List<string>();

		Mode m_mode;

		AssetPostAddress m_adress;
		string m_sampleString = string.Empty;

		GUIStyle m_messageStyle;
		GUIStyle m_labelStyle;
		GUIStyle m_deleteBtnStyle;
		GUIStyle m_plusStyle;
		GUIStyle m_registerStyle;


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
			titleContent = new GUIContent("Asset Post");
			minSize = new Vector2(330f, 100f);

			m_adressbook = AssetPostAddressBook.Load();
			UpdatePostman();

			InitGUI();
		}

		private void OnGUI()
		{
			switch (m_mode)
			{
				case Mode.Delivery:
					DeliveryMode();
					break;
				case Mode.Addressbook:
					DrawAdressbook();
					break;

				case Mode.RegisterAddress:
					RegisterAdresseeMode();
					break;		
			}
		}


		//------------------------------------------------------
		// gui
		//------------------------------------------------------

		void InitGUI()
		{
			var skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
			m_messageStyle = skin.FindStyle("TL Selection H1");
			m_labelStyle = skin.FindStyle("PreToolbar");

			m_deleteBtnStyle = skin.FindStyle("OL Minus");
			m_plusStyle = skin.FindStyle("OL Plus");
			m_registerStyle = skin.FindStyle("AC Button");
		}

		void DrawToolbar(string label, string button, Mode mode)
		{			
			var r = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.toolbar, GUILayout.ExpandWidth((true)));
			EditorGUI.LabelField(r, label, EditorStyles.toolbar);

			r.x = r.xMax - 64 - 8;
			r.width = 64;
			if (GUI.Button(r, button, EditorStyles.toolbarButton))
			{
				m_mode = mode;
			}
		}


		//------------------------------------------------------
		// delivery
		//------------------------------------------------------

		void UpdatePostman()
		{
			m_postmans = m_adressbook.adresses.Select(i => new AssetPostman(i)).ToArray();
		}

		void DeliveryMode()
		{
			DrawToolbar("Asset Post", "配達先一覧", Mode.Addressbook);
			GUILayout.FlexibleSpace();

			EditorGUILayout.LabelField("ドロップされたアセットを\n適切な場所へ配置します。", m_messageStyle, GUILayout.Height(m_messageStyle.fontSize * 3f));

			GUILayout.FlexibleSpace();

			DrawDeliveryInfo();

			DeliveryDropFiles();
		}

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
			foreach (var postman in m_postmans)
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


		//------------------------------------------------------
		// adresseebook
		//------------------------------------------------------

		void DrawAdressbook()
		{
			DrawToolbar("配達先一覧", "戻る", Mode.Delivery);

			for (int i = 0; i < m_adressbook.adresses.Count; ++i)
			{
				if (DrawAdress(m_adressbook.adresses[i]))
				{
					m_adressbook.adresses.RemoveAt(i--);
				}
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("新規登録", m_registerStyle))
			{
				m_adress = new AssetPostAddress();
				m_mode = Mode.RegisterAddress;
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}

		bool DrawAdress(AssetPostAddress adress)
		{
			bool deleteFlag = false;
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("編集", GUILayout.Width(32)))
			{
				m_adress = adress;
				m_mode = Mode.RegisterAddress;
			}

			EditorGUILayout.LabelField(adress.name);

			if (GUILayout.Button(GUIContent.none, m_deleteBtnStyle, GUILayout.Width(16)))
			{
				deleteFlag = true;
			}

			EditorGUILayout.EndHorizontal();
			return deleteFlag;
		}

		void RegisterAddress(AssetPostAddress address)
		{
			if (!m_adressbook.adresses.Contains(m_adress))
			{
				m_adressbook.adresses.Add(m_adress);
				m_adressbook.adresses.Sort((x, y) => x.name.CompareTo(y.name));
			}
			m_adressbook.Save();
			UpdatePostman();
		}


		//------------------------------------------------------
		// 配達先登録
		//------------------------------------------------------

		readonly GUIContent kPatternContent = new GUIContent("ファイル命名規約", "正規表現");
		readonly GUIContent kAssetPathContent = new GUIContent("Assets/", "string.Format()形式で指定");
		readonly GUIContent kIndexContent = new GUIContent("Index", "マイナスを指定すると最後から");

		void RegisterAdresseeMode()
		{
			DrawToolbar("配達先登録", "戻る", Mode.Addressbook);

			if (m_adress == null)
				return;

			bool addable = true;

			m_adress.name = EditorGUILayout.TextField("登録名", m_adress.name ?? string.Empty);
			m_adress.fileNamePattern = EditorGUILayout.TextField(kPatternContent, m_adress.fileNamePattern);

			EditorGUILayout.Space();

			EditorGUILayout.LabelField("お届け先", m_labelStyle);
			var labelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 45;
			m_adress.assetPathFormat = EditorGUILayout.TextField(kAssetPathContent, m_adress.assetPathFormat);
			EditorGUIUtility.labelWidth = labelWidth;

			int needArgmentCount = GetFormatArgumentCount(m_adress.assetPathFormat);

			++EditorGUI.indentLevel;
			EditorGUILayout.LabelField("format引数設定 - ファイル名をSplitして使う");

			m_adress.separators = DrawSeparators(m_adress.separators, m_adress.fileNamePattern);

			for (int i = 0; i < m_adress.argumentList.Count; ++i)
			{
				if (DrawElementInfo(i, m_adress.argumentList[i]))
				{
					m_adress.argumentList.RemoveAt(i--);
				}
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Args追加", m_plusStyle))
			{
				var info = new AssetPostAddress.ArgumentInfo();
				m_adress.argumentList.Add(info);
			}
			EditorGUILayout.EndHorizontal();

			if (m_adress.argumentList.Count < needArgmentCount)
			{
				EditorGUILayout.HelpBox("引数の数がたりません", MessageType.Warning);
				addable = false;
			}

			--EditorGUI.indentLevel;


			EditorGUILayout.Space();

			addable &= DrawSample(needArgmentCount);

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			GUI.enabled = addable && !string.IsNullOrEmpty(m_adress.name);
			if (GUILayout.Button("登録",m_registerStyle))
			{
				RegisterAddress(m_adress);
				m_adress = new AssetPostAddress();
				m_mode = Mode.Addressbook;
			}
			GUI.enabled = true;

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}

		static char[] DrawSeparators(char[] separators, string sample)
		{
			var str = EditorGUILayout.TextField("separator", new string(separators));
			separators = new char[str.Length];
			for (int i = 0; i < str.Length; ++i)
			{
				separators[i] = str[i];
			}

			var elements = sample.Split(separators);
			var sb = new System.Text.StringBuilder();
			for (int i = 0; i < elements.Length; ++i)
			{
				if (sb.Length > 0)
					sb.Append(" | ");
				sb.Append(elements[i]);
			}
			sb.Insert(0, "Split = ");

			EditorGUILayout.HelpBox(sb.ToString(), MessageType.None);

			return separators;
		}

		bool DrawElementInfo(int index, AssetPostAddress.ArgumentInfo info)
		{
			bool deleteFlag = false;

			var numW = GUILayout.Width(32);

			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			EditorGUILayout.LabelField("{"+index+"}", GUILayout.Width(20));

			var labelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 35;
			info.elementIndex = EditorGUILayout.IntField(kIndexContent, info.elementIndex, GUILayout.Width(60));
			EditorGUIUtility.labelWidth = labelWidth;

			GUILayout.Space(8);
			info.startIndex = EditorGUILayout.IntField(info.startIndex, numW);
			EditorGUILayout.LabelField("文字目 -", GUILayout.Width(45));
			info.endIndex = EditorGUILayout.IntField(info.endIndex, numW);
			EditorGUILayout.LabelField("文字目", GUILayout.Width(45));

			if (GUILayout.Button(GUIContent.none, m_deleteBtnStyle, GUILayout.Width(16)))
			{
				deleteFlag = true;
			}

			EditorGUILayout.EndHorizontal();
			EditorGUI.indentLevel = indent;

			return deleteFlag;
		}

		static int GetFormatArgumentCount(string format)
		{
			int count = 0;
			var regex = new Regex(@"{[0-9]+}");
			var m = regex.Match(format);
			while (m.Success)
			{
				var str = m.Value;
				int n;
				if (int.TryParse(str.Substring(1, str.Length - 2), out n))
				{
					count = Math.Max(n + 1, count);	
				}
				m = m.NextMatch();
			}
			return count;
		}

		bool DrawSample(int needArgmentCount)
		{
			bool success = true;

			EditorGUILayout.LabelField("テスト", m_labelStyle);
			m_sampleString = EditorGUILayout.TextField("ファイル名", m_sampleString);
			if (m_adress.argumentList.Count >= needArgmentCount)
			{
				try
				{
					if (Regex.IsMatch(m_sampleString, m_adress.fileNamePattern))
					{
						EditorGUILayout.LabelField("> " + m_adress.GetAssetPath(m_sampleString));
					}
					else
					{
						EditorGUILayout.HelpBox("ファイル名が規約に合っていません", MessageType.Info);
					}
				}
				catch (Exception)
				{
					EditorGUILayout.HelpBox("命名規約の正規表現が異常", MessageType.Warning);
					success = false;
				}
			}

			return success;
		}
	}
}