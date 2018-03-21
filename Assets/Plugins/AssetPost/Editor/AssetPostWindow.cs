using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using State = System.Action;

namespace AssetPost
{
	/// <summary>
	/// このウィンドウにアセットをドロップすると、プロジェクトの適切な所に配置される。
	/// どこに配置されるかは AssetPostman が決める。
	/// </summary>
	public class AssetPostWindow : EditorWindow
	{
		AssetPostAddressBook m_addressbook;
		AssetPostman[] m_postmans;
		List<string> m_assetPathList = new List<string>();
		List<string> m_unknownFileList = new List<string>();

		State m_state;

		AssetPostAddress m_registerAddress;
		bool m_patternEnabled;
		int m_needArgmentCount;
		string m_sampleString = string.Empty;

		GUIStyle m_messageStyle;
		GUIStyle m_labelStyle;
		GUIStyle m_deleteBtnStyle;
		GUIStyle m_plusStyle;
		GUIStyle m_registerStyle;

		Vector2 m_scrollPosition;


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

			m_addressbook = AssetPostAddressBook.Load();
			UpdatePostman();

			InitGUI();

			SetState(StateDelivery);
		}

		private void OnGUI()
		{
			m_state();
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

		void Toolbar(string label, string button, State state)
		{			
			var r = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.toolbar, GUILayout.ExpandWidth((true)));
			EditorGUI.LabelField(r, label, EditorStyles.toolbar);

			r.x = r.xMax - 64 - 8;
			r.width = 64;
			if (GUI.Button(r, button, EditorStyles.toolbarButton))
			{
				SetState(state);
			}
		}

		bool CenterButton(string label)
		{
			bool press = false;
			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();
				press = GUILayout.Button(label, m_registerStyle);
				GUILayout.FlexibleSpace();
			}
			EditorGUILayout.EndHorizontal();
			return press;
		}

		void ProcessDragAndDrop(Action<string[]> dropprocessor)
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
					dropprocessor(DragAndDrop.paths);
					e.Use();
					break;
			}

		}

		//------------------------------------------------------
		// state
		//------------------------------------------------------

		void SetState(State state)
		{
			m_state = state;
			GUI.FocusControl(string.Empty);
		}


		//------------------------------------------------------
		// state delivery
		//------------------------------------------------------

		void UpdatePostman()
		{
			m_postmans = m_addressbook.addressList.Select(i => new AssetPostman(i)).ToArray();
		}

		void StateDelivery()
		{
			Toolbar("Asset Post", "配達先一覧", StateAddressbook);
			GUILayout.FlexibleSpace();

			EditorGUILayout.LabelField("ドロップされたアセットを\n適切な場所へ配置します。", m_messageStyle, GUILayout.Height(m_messageStyle.fontSize * 3f));

			GUILayout.FlexibleSpace();

			DrawDeliveryInfo();

			ProcessDragAndDrop(DeliveryFiles);
		}

		void DeliveryFiles(string[] paths)
		{
			m_assetPathList.Clear();
			m_unknownFileList.Clear();
			AssetDatabase.StartAssetEditing();
			foreach (var file in GetFiles(paths))
			{
				DeliveryFile(file);
			}
			AssetDatabase.StopAssetEditing();
			AssetDatabase.Refresh();
		}

		static IEnumerable<string> GetFiles(string[] paths)
		{
			foreach (var path in paths)
			{
				if (Directory.Exists(path))
				{
					foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
					{
						yield return file;
					}					
				}
				else
				{
					yield return path;
				}
			}
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
			m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

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

			EditorGUILayout.EndScrollView();
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
		// state addresseebook
		//------------------------------------------------------

		void StateAddressbook()
		{
			Toolbar("配達先一覧", "戻る", StateDelivery);

			for (int i = 0; i < m_addressbook.addressList.Count; ++i)
			{
				if (AddressField(m_addressbook.addressList[i]))
				{
					m_addressbook.addressList.RemoveAt(i--);
				}
			}

			if (CenterButton("新規登録"))
			{
				SetRegisterAddress(null);
			}
		}

		bool AddressField(AssetPostAddress address)
		{
			bool deleteFlag = false;
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("編集", GUILayout.Width(32)))
			{
				SetRegisterAddress(address);
			}

			EditorGUILayout.LabelField(address.name);

			if (GUILayout.Button(GUIContent.none, m_deleteBtnStyle, GUILayout.Width(16)))
			{
				if (EditorUtility.DisplayDialog("配達先削除", string.Format("配達先[{0}]を本当に削除しますか？", address.name), "削除"))
				{
					deleteFlag = true;
				}
			}

			EditorGUILayout.EndHorizontal();
			return deleteFlag;
		}


		//------------------------------------------------------
		// state register
		//------------------------------------------------------

		void SetRegisterAddress(AssetPostAddress address)
		{
			m_registerAddress = address ?? new AssetPostAddress();
			m_patternEnabled = false;
			m_needArgmentCount = GetFormatArgumentCount(m_registerAddress.assetPathFormat);
			SetState(StateRegisterAddress);
		}

		bool CanRegisterAddress()
		{
			return m_registerAddress != null && 
				!string.IsNullOrEmpty(m_registerAddress.name) &&
				!string.IsNullOrEmpty(m_registerAddress.fileNamePattern) &&
				m_patternEnabled &&
				!string.IsNullOrEmpty(m_registerAddress.assetPathFormat) &&
				m_registerAddress.argumentList.Count >= m_needArgmentCount;
		}

		void RegisterAddress()
		{
			Assert.IsTrue(CanRegisterAddress());
			
			if (!m_addressbook.addressList.Contains(m_registerAddress))
			{
				m_addressbook.addressList.Add(m_registerAddress);
				m_addressbook.addressList.Sort((x, y) => x.name.CompareTo(y.name));
			}
			m_addressbook.Save();
			UpdatePostman();

			m_registerAddress = null;
			SetState(StateAddressbook);
		}

		void StateRegisterAddress()
		{
			Toolbar("配達先登録", "戻る", StateAddressbook);

			var labelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 100;
			{
				EditAddress(m_registerAddress);

				EditorGUILayout.Space();

				DrawPatternCheck();
			}
			EditorGUIUtility.labelWidth = labelWidth;

			EditorGUILayout.Space();

			GUI.enabled = CanRegisterAddress();
			if (CenterButton("登録"))
			{
				RegisterAddress();
			}
			GUI.enabled = true;

			ProcessDragAndDrop(DropFilePatternCheck);
		}

		readonly GUIContent kAdressNameContent = new GUIContent("登録名");
		readonly GUIContent kPatternContent = new GUIContent("ファイル命名規約", "正規表現");
		readonly GUIContent kAdressPathContent = new GUIContent("お届け先");
		readonly GUIContent kAssetPathContent = new GUIContent("Assets/", "string.Format()形式で指定");
		readonly GUIContent kFormatHelpContent = new GUIContent("format引数設定 - ファイル名をSplitして使う");
		readonly GUIContent kAddArgumentContent = new GUIContent("引数追加");

		void EditAddress(AssetPostAddress address)
		{
			address.name = EditorGUILayout.TextField(kAdressNameContent, address.name ?? string.Empty);
			address.fileNamePattern = EditorGUILayout.TextField(kPatternContent, address.fileNamePattern);

			EditorGUILayout.Space();

			EditorGUILayout.LabelField(kAdressPathContent, m_labelStyle);
			EditorGUI.BeginChangeCheck();
			var labelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 45;
			address.assetPathFormat = EditorGUILayout.TextField(kAssetPathContent, address.assetPathFormat);
			EditorGUIUtility.labelWidth = labelWidth;
			if (EditorGUI.EndChangeCheck())
			{
				m_needArgmentCount = GetFormatArgumentCount(address.assetPathFormat);
			}
						
			++EditorGUI.indentLevel;
			EditorGUILayout.LabelField(kFormatHelpContent);

			address.separators = SeparatorsField(address.separators, address.fileNamePattern);

			for (int i = 0; i < address.argumentList.Count; ++i)
			{
				if (ArgumentInfoField(i, address.argumentList[i]))
				{
					address.argumentList.RemoveAt(i--);
				}
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button(kAddArgumentContent, m_plusStyle))
			{
				var info = new AssetPostAddress.ArgumentInfo();
				address.argumentList.Add(info);
			}
			EditorGUILayout.EndHorizontal();

			if (address.argumentList.Count < m_needArgmentCount)
			{
				EditorGUILayout.HelpBox("引数の数がたりません", MessageType.Warning);
			}

			--EditorGUI.indentLevel;
		}

		readonly GUIContent kSeparatorContent = new GUIContent("separator", "ファイル名がこのchar[]でSplitされます");

		char[] SeparatorsField(char[] separators, string sample)
		{
			var str = EditorGUILayout.TextField(kSeparatorContent, new string(separators));
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

		readonly GUIContent kIndexContent = new GUIContent("Index", "マイナスを指定すると最後から");

		bool ArgumentInfoField(int index, AssetPostAddress.ArgumentInfo info)
		{
			bool deleteFlag = false;

			var numW = GUILayout.Width(32);

			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				EditorGUILayout.LabelField("{" + index + "}", GUILayout.Width(24));

				var labelWidth = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 40;
				info.elementIndex = EditorGUILayout.IntField(kIndexContent, info.elementIndex, GUILayout.Width(65));
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
			}
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

		readonly GUIContent kTestContent = new GUIContent("テスト", "ファイルドロップでも可能");
		readonly GUIContent kFileNameContent = new GUIContent("ファイル名", "ファイルドロップでも可能");
		readonly GUIContent kDeliveryAddressContent = new GUIContent("お届け先");

		void DrawPatternCheck()
		{
			m_patternEnabled = false;

			EditorGUILayout.LabelField(kTestContent, m_labelStyle);
			m_sampleString = EditorGUILayout.TextField(kFileNameContent, m_sampleString);
			EditorGUILayout.LabelField(kDeliveryAddressContent);
			if (m_registerAddress.argumentList.Count >= m_needArgmentCount)
			{
				try
				{
					if (Regex.IsMatch(m_sampleString, m_registerAddress.fileNamePattern))
					{
						EditorGUILayout.LabelField(m_registerAddress.GetAssetPath(m_sampleString));
					}
					else
					{
						EditorGUILayout.HelpBox("ファイル名が規約に合っていません", MessageType.Info);
					}

					m_patternEnabled = true;
				}
				catch (Exception)
				{
					EditorGUILayout.HelpBox("命名規約の正規表現が異常", MessageType.Warning);		
				}
			}
		}

		void DropFilePatternCheck(string[] paths)
		{
			if (paths.Length > 0)
			{
				m_sampleString = Path.GetFileName(paths[0]);
			}
		}
	}
}