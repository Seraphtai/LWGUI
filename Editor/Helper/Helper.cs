// Copyright (c) Jason Ma

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LWGUI.Timeline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace LWGUI
{
	/// <summary>
	/// Misc Function
	/// </summary>
	public static class Helper
	{
		
		#region Misc
		
		public static bool IsPropertyHideInInspector(MaterialProperty prop)
		{
			return (prop.GetPropertyFlags() & ShaderPropertyFlags.HideInInspector) != 0;
		}

		public static bool StringToBool(string str) => str?.ToLower() is "on" or "true";
		
		public static string GetKeywordName(string keyword, string propName)
		{
			string k;
			if (string.IsNullOrEmpty(keyword) || keyword == "__")
			{
				k = propName.ToUpperInvariant() + "_ON";
			}
			else
			{
				k = keyword.ToUpperInvariant();
			}
			return k;
		}

		public static void SetShaderKeywordEnabled(Object[] materials, string keywordName, bool isEnable)
		{
			if (string.IsNullOrEmpty(keywordName) || string.IsNullOrEmpty(keywordName)) return;

			foreach (Material m in materials)
			{
				// delete "_" keywords
				if (keywordName == "_")
				{
					if (m.IsKeywordEnabled(keywordName))
					{
						m.DisableKeyword(keywordName);
					}
					continue;
				}

				if (m.IsKeywordEnabled(keywordName))
				{
					if (!isEnable) m.DisableKeyword(keywordName);
				}
				else
				{
					if (isEnable) m.EnableKeyword(keywordName);
				}
			}
		}

		public static void SelectShaderKeyword(Object[] materials, string[] keywordNames, int index)
		{
			Debug.Assert(keywordNames.Length >= 1 && index < keywordNames.Length && index >= 0,
						 "KeyWords Length: " + keywordNames.Length + " or Index: " + index + " Error! ");
			for (int i = 0; i < keywordNames.Length; i++)
			{
				SetShaderKeywordEnabled(materials, keywordNames[i], index == i);
			}
		}

		public static void SetShaderPassEnabled(Object[] materials, string[] lightModeNames, bool enabled)
		{
			if (lightModeNames.Length == 0) return;

			foreach (Material material in materials)
			{
				for (int i = 0; i < lightModeNames.Length; i++)
				{
					material.SetShaderPassEnabled(lightModeNames[i], enabled);
				}
			}
		}

		public static LWGUI GetLWGUI(MaterialEditor editor)
		{
			var customShaderGUI = editor.customShaderGUI;
			if (customShaderGUI != null && customShaderGUI is LWGUI)
			{
				LWGUI gui = customShaderGUI as LWGUI;
				return gui;
			}
			else
			{
				Debug.LogWarning("LWGUI: Please add \"CustomEditor \"LWGUI.LWGUI\"\" to the end of your shader!");
				return null;
			}
		}

		public static LWGUIMetaDatas GetLWGUIMetadatas(MaterialEditor editor) => GetLWGUI(editor).metaDatas;

		public static void AdaptiveFieldWidth(GUIStyle style, GUIContent content)
		{
			var extraTextWidth = Mathf.Max(0, style.CalcSize(content).x - (EditorGUIUtility.fieldWidth - RevertableHelper.revertButtonWidth));
			EditorGUIUtility.labelWidth -= extraTextWidth;
			EditorGUIUtility.fieldWidth += extraTextWidth;
		}

		public static void BeginProperty(Rect rect, MaterialProperty property, LWGUIMetaDatas metaDatas)
		{
#if UNITY_2022_1_OR_NEWER
			MaterialEditor.BeginProperty(rect, property);
			foreach (var extraPropName in metaDatas.GetPropStaticData(property.name).extraPropNames)
				MaterialEditor.BeginProperty(rect, metaDatas.GetPropDynamicData(extraPropName).property);
#endif
		}

		public static void EndProperty(LWGUIMetaDatas metaDatas, MaterialProperty property)
		{
#if UNITY_2022_1_OR_NEWER
			MaterialEditor.EndProperty();
			foreach (var extraPropName in metaDatas.GetPropStaticData(property.name).extraPropNames)
				MaterialEditor.EndProperty();
#endif
		}

		public static bool EndChangeCheck(LWGUIMetaDatas metaDatas, MaterialProperty property)
		{
			return metaDatas.perMaterialData.EndChangeCheck(property.name);
		}

		public static float GetCurrentPropertyLayoutWidth()
		{
			return ReflectionHelper.EditorGUILayout_kLabelFloatMinW - ReflectionHelper.EditorGUI_Indent - RevertableHelper.revertButtonWidth - 2;
		}

		#endregion


		#region Math

		public const double Float_Epsilon = 1e-10;

		public static bool Approximately(float a, float b) => Mathf.Abs(a - b) < Float_Epsilon;
		
		public static bool PropertyValueEquals(MaterialProperty prop1, MaterialProperty prop2)
		{
			if (prop1.textureValue == prop2.textureValue
			    && prop1.vectorValue == prop2.vectorValue
			    && prop1.colorValue == prop2.colorValue
			    && Approximately(prop1.floatValue, prop2.floatValue)
			    && prop1.intValue == prop2.intValue
			   )
				return true;
			else
				return false;
		}

		public static float PowPreserveSign(float f, float p)
		{
			float num = Mathf.Pow(Mathf.Abs(f), p);
			if ((double)f < 0.0)
				return -num;
			return num;
		}

		#endregion


		#region GUI Styles

		// Tips: Use properties to fix null reference errors

		private static GUIStyle _guiStyles_IconButton;
		public static GUIStyle guiStyles_IconButton => _guiStyles_IconButton ?? new GUIStyle(EditorStyles.iconButton) { fixedHeight = 0, fixedWidth = 0 };

		private static GUIStyle _guiStyle_Foldout;
		public static GUIStyle guiStyle_Foldout => _guiStyle_Foldout ?? new GUIStyle(EditorStyles.miniButton)
		{
			contentOffset = new Vector2(22, 0),
			fixedHeight = 27,
			alignment = TextAnchor.MiddleLeft,
			font = EditorStyles.boldLabel.font,
			fontSize = EditorStyles.boldLabel.fontSize + 1
		};

		private static GUIStyle _guiStyle_Helpbox;
		public static GUIStyle guiStyle_Helpbox => _guiStyle_Helpbox ?? new GUIStyle(EditorStyles.helpBox) { fontSize = 12 };
		
		private static GUIStyle _guiStyle_RampSelectButton;
		public static GUIStyle guiStyle_RampSelectButton => _guiStyle_RampSelectButton ?? new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft };

		private static GUIStyle _guiStyles_ToolbarSearchTextFieldPopup;
		public static GUIStyle guiStyles_ToolbarSearchTextFieldPopup
		{
			get
			{
				if (_guiStyles_ToolbarSearchTextFieldPopup == null)
				{
					string toolbarSeachTextFieldPopupStr = "ToolbarSeachTextFieldPopup";
					{
						// ToolbarSeachTextFieldPopup has renamed at Unity 2021.3.28+
#if !UNITY_2022_3_OR_NEWER
						string[] versionParts = Application.unityVersion.Split('.');
						int majorVersion = int.Parse(versionParts[0]);
						int minorVersion = int.Parse(versionParts[1]);
						Match patchVersionMatch = Regex.Match(versionParts[2], @"\d+");
						int patchVersion = int.Parse(patchVersionMatch.Value);
						if (majorVersion >= 2021 && minorVersion >= 3 && patchVersion >= 28)
#endif
						{
							toolbarSeachTextFieldPopupStr = "ToolbarSearchTextFieldPopup";
						}
					}
					_guiStyles_ToolbarSearchTextFieldPopup = new GUIStyle(toolbarSeachTextFieldPopupStr);
				}
				return _guiStyles_ToolbarSearchTextFieldPopup;
			}
		}

		#endregion


		#region Draw GUI for Drawers

		// TODO: use Reflection
		// copy and edit of https://github.com/GucioDevs/SimpleMinMaxSlider/blob/master/Assets/SimpleMinMaxSlider/Scripts/Editor/MinMaxSliderDrawer.cs
		public static Rect[] SplitRect(Rect rectToSplit, int n)
		{
			Rect[] rects = new Rect[n];

			for (int i = 0; i < n; i++)
			{
				rects[i] = new Rect(rectToSplit.position.x + (i * rectToSplit.width / n), rectToSplit.position.y,
									rectToSplit.width / n, rectToSplit.height);
			}

			int padding = (int)rects[0].width - 50; // use 50, enough to show 0.xx (2 digits)
			int space = 5;

			rects[0].width -= padding + space;
			rects[2].width -= padding + space;

			rects[1].x -= padding;
			rects[1].width += padding * 2;

			rects[2].x += padding + space;

			return rects;
		}

		public static bool DrawFoldout(Rect rect, ref bool isFolding, bool toggleValue, bool hasToggle, GUIContent label)
		{
			var toggleRect = new Rect(rect.x + 8f, rect.y + 7f, 13f, 13f);

			// Toggle Event
			if (hasToggle)
			{
				if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && toggleRect.Contains(Event.current.mousePosition))
				{
					toggleValue = !toggleValue;
					Event.current.Use();
					GUI.changed = true;
				}
			}

			// Button
			{
				// Cancel Right Click
				if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && rect.Contains(Event.current.mousePosition))
					Event.current.Use();

				var enabled = GUI.enabled;
				GUI.enabled = true;
				var guiColor = GUI.backgroundColor;
				GUI.backgroundColor = isFolding ? Color.white : new Color(0.85f, 0.85f, 0.85f);
				if (GUI.Button(rect, label, guiStyle_Foldout))
				{
					isFolding = !isFolding;
					GUI.changed = false;
				}
				GUI.backgroundColor = guiColor;
				GUI.enabled = enabled;
			}

			// Toggle Icon
			if (hasToggle)
			{
				EditorGUI.Toggle(toggleRect, string.Empty, toggleValue);
			}

			return toggleValue;
		}

		public static bool ToggleButton(Rect position, GUIContent label, bool on, GUIStyle style = null, float padding = 0)
		{
			var paddedRect = new Rect(position.x + padding, position.y, position.width - padding * 2, position.height);
			style ??= EditorStyles.miniButton;
			
			bool flag = GUI.Button(paddedRect, label, style);
			if (Event.current.type == EventType.Repaint)
			{
				bool isHover = paddedRect.Contains(Event.current.mousePosition);
				style.Draw(position, label, isHover, false, on, false);
			}

			return flag;
		}

		public static void DrawShaderPropertyWithErrorLabel(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor, string message)
		{
			var c = GUI.color;
			GUI.color = Color.red;
			var newLabel = $"{ label.text } ({ message })";
			editor.DefaultShaderProperty(position, prop, newLabel);
			GUI.color = c;
		}

		#endregion


		#region Draw GUI for Materials

		public static void DrawSplitLine()
		{
			var rect = EditorGUILayout.GetControlRect(true, 1);
			rect.x = 0;
			rect.width = EditorGUIUtility.currentViewWidth;
			EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.45f));
		}

		private static readonly Texture2D _helpboxIcon = EditorGUIUtility.IconContent("console.infoicon").image as Texture2D;

		public static void DrawHelpbox(PropertyStaticData propertyStaticData, PropertyDynamicData propertyDynamicData)
		{
			var helpboxStr = propertyStaticData.helpboxMessages;
			if (!string.IsNullOrEmpty(helpboxStr))
			{
				var content = new GUIContent(helpboxStr, _helpboxIcon);
				var textWidth = GetCurrentPropertyLayoutWidth();
				var textHeight = guiStyle_Helpbox.CalcHeight(content, textWidth);
				var helpboxRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(true, textHeight));
				helpboxRect.xMax -= RevertableHelper.revertButtonWidth;
				GUI.Label(helpboxRect, content, guiStyle_Helpbox);
				// EditorGUI.HelpBox(helpboxRect, helpboxStr, MessageType.Info);
			}
		}

		private static Texture _logoCache;
		private static GUIContent _logoGuiContentCache;
		private static Texture _logo => _logoCache = _logoCache ?? AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath("26b9d845eb7b1a747bf04dc84e5bcc2c"));
		private static GUIContent _logoGuiContent => _logoGuiContentCache = _logoGuiContentCache ?? new GUIContent(string.Empty, _logo,
																   "LWGUI (Light Weight Shader GUI)\n\n"
																 + "A Lightweight, Flexible, Powerful Unity Shader GUI system.\n\n"
																 + "Copyright (c) Jason Ma");

		public static void DrawLogo()
		{
			var logoRect = EditorGUILayout.GetControlRect(false, _logo.height);
			var w = logoRect.width;
			logoRect.xMin += w * 0.5f - _logo.width * 0.5f;
			logoRect.xMax -= w * 0.5f - _logo.width * 0.5f;

			if (EditorGUIUtility.currentViewWidth >= logoRect.width && GUI.Button(logoRect, _logoGuiContent, guiStyles_IconButton))
			{
				Application.OpenURL("https://github.com/JasonMa0012/LWGUI");
			}
		}

		#endregion
		

		#region Importer

		// https://docs.unity3d.com/ScriptReference/TextureImporter.GetPlatformTextureSettings.html
		public static string[] platformNamesForTextureSettings =>
			new[] { "DefaultTexturePlatform", "Standalone", "Web", "iPhone", "Android", "WebGL", "Windows Store Apps", "PS4", "XboxOne", "Nintendo Switch", "tvOS" };

		#endregion
	}
}