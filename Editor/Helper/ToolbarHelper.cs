// Copyright (c) Jason Ma

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LWGUI
{
    public class ToolbarHelper
    {
		#region Toolbar Buttons
		
		internal enum CopyMaterialValueMask
		{
			Float       = 1 << 0,
			Vector      = 1 << 1,
			Texture     = 1 << 2,
			Keyword     = 1 << 3,
			RenderQueue = 1 << 4,
			Number      = Float | Vector,
			All         = (1 << 5) - 1,
		}

		private static GUIContent[] _pasteMaterialMenus = new[]
		{
			new GUIContent("Paste Number Values"),
			new GUIContent("Paste Texture Values"),
			new GUIContent("Paste Keywords"),
			new GUIContent("Paste RenderQueue"),
		};

		private static uint[] _pasteMaterialMenuValueMasks = new[]
		{
			(uint)CopyMaterialValueMask.Number,
			(uint)CopyMaterialValueMask.Texture,
			(uint)CopyMaterialValueMask.Keyword,
			(uint)CopyMaterialValueMask.RenderQueue,
		};
		

		private const string _iconCopyGUID       = "9cdef444d18d2ce4abb6bbc4fed4d109";
		private const string _iconPasteGUID      = "8e7a78d02e4c3574998524a0842a8ccb";
		private const string _iconSelectGUID     = "6f44e40b24300974eb607293e4224ecc";
		private const string _iconCheckoutGUID   = "72488141525eaa8499e65e52755cb6d0";
		private const string _iconExpandGUID     = "2382450e7f4ddb94c9180d6634c41378";
		private const string _iconCollapseGUID   = "929b6e5dfacc42b429d715a3e1ca2b57";
		private const string _iconVisibilityGUID = "9576e23a695b35d49a9fc55c9a948b4f";

		private const string _iconCopyTooltip       = "Copy Material Properties";
		private const string _iconPasteTooltip      = "Paste Material Properties\n\nRight-click to paste values by type.";
		private const string _iconSelectTooltip     = "Select the Material Asset\n\nUsed to jump from a Runtime Material Instance to a Material Asset.";
		private const string _iconCheckoutTooltip   = "Checkout selected Material Assets";
		private const string _iconExpandTooltip     = "Expand All Groups";
		private const string _iconCollapseTooltip   = "Collapse All Groups";
		private const string _iconVisibilityTooltip = "Display Mode";

		private static GUIContent _guiContentCopyCache;
		private static GUIContent _guiContentPasteCache;
		private static GUIContent _guiContentSelectCache;
		private static GUIContent _guiContentChechoutCache;
		private static GUIContent _guiContentExpandCache;
		private static GUIContent _guiContentCollapseCache;
		private static GUIContent _guiContentVisibilityCache;
		
		private static GUIContent _guiContentCopy       => _guiContentCopyCache = _guiContentCopyCache ?? new GUIContent("", AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(_iconCopyGUID)), _iconCopyTooltip);
		private static GUIContent _guiContentPaste      => _guiContentPasteCache = _guiContentPasteCache ?? new GUIContent("", AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(_iconPasteGUID)), _iconPasteTooltip);
		private static GUIContent _guiContentSelect     => _guiContentSelectCache = _guiContentSelectCache ?? new GUIContent("", AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(_iconSelectGUID)), _iconSelectTooltip);
		private static GUIContent _guiContentChechout   => _guiContentChechoutCache = _guiContentChechoutCache ?? new GUIContent("", AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(_iconCheckoutGUID)), _iconCheckoutTooltip);
		private static GUIContent _guiContentExpand     => _guiContentExpandCache = _guiContentExpandCache ?? new GUIContent("", AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(_iconExpandGUID)), _iconExpandTooltip);
		private static GUIContent _guiContentCollapse   => _guiContentCollapseCache = _guiContentCollapseCache ?? new GUIContent("", AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(_iconCollapseGUID)), _iconCollapseTooltip);
		private static GUIContent _guiContentVisibility => _guiContentVisibilityCache = _guiContentVisibilityCache ?? new GUIContent("", AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(_iconVisibilityGUID)), _iconVisibilityTooltip);
		

		public static void DrawToolbarButtons(ref Rect toolBarRect, LWGUIMetaDatas metaDatas)
		{
			var (perShaderData, perMaterialData, perInspectorData) = metaDatas.GetDatas();

			// Copy
			var buttonRectOffset = toolBarRect.height + 2;
			var buttonRect = new Rect(toolBarRect.x, toolBarRect.y, toolBarRect.height, toolBarRect.height);
			toolBarRect.xMin += buttonRectOffset;
			if (GUI.Button(buttonRect, _guiContentCopy, Helper.guiStyles_IconButton))
			{
				ContextMenuHelper.CopyMaterial(metaDatas.GetMaterial());
			}

			// Paste
			buttonRect.x += buttonRectOffset;
			toolBarRect.xMin += buttonRectOffset;
			// Right Click
			if (Event.current.type == EventType.MouseDown
			 && Event.current.button == 1
			 && buttonRect.Contains(Event.current.mousePosition))
			{
				EditorUtility.DisplayCustomMenu(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0), _pasteMaterialMenus, -1,
												(data, options, selected) =>
												{
													ContextMenuHelper.DoPasteMaterialProperties(metaDatas, _pasteMaterialMenuValueMasks[selected]);
												}, null);
				Event.current.Use();
			}
			// Left Click
			if (GUI.Button(buttonRect, _guiContentPaste, Helper.guiStyles_IconButton))
			{
				ContextMenuHelper.DoPasteMaterialProperties(metaDatas, (uint)CopyMaterialValueMask.All);
			}

			// Select Material Asset, jump from a Runtime Material Instance to a Material Asset
			buttonRect.x += buttonRectOffset;
			toolBarRect.xMin += buttonRectOffset;
			if (GUI.Button(buttonRect, _guiContentSelect, Helper.guiStyles_IconButton))
			{
				var material = metaDatas.GetMaterial();

				if (AssetDatabase.Contains(material))
				{
					Selection.activeObject = material;
				}
				else
				{
					if (FindMaterialAssetByMaterialInstance(material, metaDatas, out var materialAsset))
					{
						Selection.activeObject = materialAsset;
					}
				}
			}

			// Checkout
			buttonRect.x += buttonRectOffset;
			toolBarRect.xMin += buttonRectOffset;
			if (GUI.Button(buttonRect, _guiContentChechout, Helper.guiStyles_IconButton))
			{
				VersionControlHelper.Checkout(metaDatas.GetMaterialEditor().targets);
			}

			// Expand
			buttonRect.x += buttonRectOffset;
			toolBarRect.xMin += buttonRectOffset;
			if (GUI.Button(buttonRect, _guiContentExpand, Helper.guiStyles_IconButton))
			{
				foreach (var propStaticDataKVPair in perShaderData.propStaticDatas)
				{
					if (propStaticDataKVPair.Value.isMain || propStaticDataKVPair.Value.isAdvancedHeader)
						propStaticDataKVPair.Value.isExpanding = true;
				}
			}

			// Collapse
			buttonRect.x += buttonRectOffset;
			toolBarRect.xMin += buttonRectOffset;
			if (GUI.Button(buttonRect, _guiContentCollapse, Helper.guiStyles_IconButton))
			{
				foreach (var propStaticDataKVPair in perShaderData.propStaticDatas)
				{
					if (propStaticDataKVPair.Value.isMain || propStaticDataKVPair.Value.isAdvancedHeader)
						propStaticDataKVPair.Value.isExpanding = false;
				}
			}

			// Display Mode
			buttonRect.x += buttonRectOffset;
			toolBarRect.xMin += buttonRectOffset;
			var color = GUI.color;
			var displayModeData = perShaderData.displayModeData;
			if (!displayModeData.IsDefaultDisplayMode())
				GUI.color = Color.yellow;
			if (GUI.Button(buttonRect, _guiContentVisibility, Helper.guiStyles_IconButton))
			{
				// Build Display Mode Menu Items
				var displayModeMenus = new[]
				{
					$"Show All Advanced Properties				({ displayModeData.advancedCount } - { perShaderData.propStaticDatas.Count })",
					$"Show All Hidden Properties				({ displayModeData.hiddenCount } - { perShaderData.propStaticDatas.Count })",
					$"Show Only Modified Properties				({ perMaterialData.modifiedCount } - { perShaderData.propStaticDatas.Count })",
					$"Show Only Modified Properties by Group	({ perMaterialData.modifiedCount } - { perShaderData.propStaticDatas.Count })",
				};
				var enabled = new[] { true, true, true, true };
				var separator = new bool[4];
				var selected = new[]
				{
					displayModeData.showAllAdvancedProperties ? 0 : -1,
					displayModeData.showAllHiddenProperties ? 1 : -1,
					displayModeData.showOnlyModifiedProperties ? 2 : -1,
					displayModeData.showOnlyModifiedGroups ? 3 : -1,
				};


				// Click Event
				void OnSwitchDisplayMode(object data, string[] options, int selectedIndex)
				{
					switch (selectedIndex)
					{
						case 0: // Show All Advanced Properties
							displayModeData.showAllAdvancedProperties = !displayModeData.showAllAdvancedProperties;
							perShaderData.ToggleShowAllAdvancedProperties();
							break;
						case 1: // Show All Hidden Properties
							displayModeData.showAllHiddenProperties = !displayModeData.showAllHiddenProperties;
							break;
						case 2: // Show Only Modified Properties
							displayModeData.showOnlyModifiedProperties = !displayModeData.showOnlyModifiedProperties;
							if (displayModeData.showOnlyModifiedProperties) displayModeData.showOnlyModifiedGroups = false;
							MetaDataHelper.ForceUpdateAllMaterialsMetadataCache(metaDatas.GetShader());
							break;
						case 3: // Show Only Modified Groups
							displayModeData.showOnlyModifiedGroups = !displayModeData.showOnlyModifiedGroups;
							if (displayModeData.showOnlyModifiedGroups) displayModeData.showOnlyModifiedProperties = false;
							MetaDataHelper.ForceUpdateAllMaterialsMetadataCache(metaDatas.GetShader());
							break;
					}
				}

				ReflectionHelper.DisplayCustomMenuWithSeparators(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0),
																 displayModeMenus, enabled, separator, selected, OnSwitchDisplayMode);
			}
			GUI.color = color;

			toolBarRect.xMin += 2;
		}

		public static Func<Renderer, Material, Material> onFindMaterialAssetInRendererByMaterialInstance;
		
		private static bool FindMaterialAssetByMaterialInstance(Material material, LWGUIMetaDatas metaDatas, out Material materialAsset)
		{
			materialAsset = null;
			
			var renderers = metaDatas.perInspectorData.materialEditor.GetMeshRenderersByMaterialEditor();
			foreach (var renderer in renderers)
			{
				if (onFindMaterialAssetInRendererByMaterialInstance != null)
				{
					materialAsset = onFindMaterialAssetInRendererByMaterialInstance(renderer, material);
				}
				
				if (materialAsset == null)
				{
					int index = renderer.materials.ToList().FindIndex(materialInstance => materialInstance == material);
					if (index >= 0 && index < renderer.sharedMaterials.Length)
					{
						materialAsset = renderer.sharedMaterials[index];
					}
				}
				
				if (materialAsset != null && AssetDatabase.Contains(materialAsset))
					return true;
			}
			
			Debug.LogError("LWGUI: Can not find the Material Assets of: " + material.name);

			return false;
		}

		#endregion


		#region Search Field

		private static readonly int s_TextFieldHash = "EditorTextField".GetHashCode();
		private static readonly GUIContent[] _searchModeMenus = Enumerable.Range(0, (int)SearchMode.Num - 1).Select(i => 
				new GUIContent(((SearchMode)i).ToString())).ToArray();

		/// <returns>is has changed?</returns>
		public static bool DrawSearchField(Rect rect, LWGUIMetaDatas metaDatas)
		{
			var (perShaderData, perMaterialData, perInspectorData) = metaDatas.GetDatas();

			bool hasChanged = false;
			EditorGUI.BeginChangeCheck();

			var revertButtonRect = RevertableHelper.SplitRevertButtonRect(ref rect);

			// Get internal TextField ControlID
			int controlId = GUIUtility.GetControlID(s_TextFieldHash, FocusType.Keyboard, rect) + 1;

			// searching mode
			Rect modeRect = new Rect(rect);
			modeRect.width = 20f;
			if (Event.current.type == UnityEngine.EventType.MouseDown && modeRect.Contains(Event.current.mousePosition))
			{
				EditorUtility.DisplayCustomMenu(rect, _searchModeMenus, (int)perShaderData.searchMode,
												(data, options, selected) =>
												{
													perShaderData.searchMode = (SearchMode)selected;
													hasChanged = true;
												}, null);
				Event.current.Use();
			}

			perShaderData.searchString = EditorGUI.TextField(rect, String.Empty, perShaderData.searchString, Helper.guiStyles_ToolbarSearchTextFieldPopup);

			if (EditorGUI.EndChangeCheck())
				hasChanged = true;

			// revert button
			if (!string.IsNullOrEmpty(perShaderData.searchString)
			 && RevertableHelper.DrawRevertButton(revertButtonRect))
			{
				perShaderData.searchString = string.Empty;
				hasChanged = true;
				GUIUtility.keyboardControl = 0;
			}

			// display search mode
			if (GUIUtility.keyboardControl != controlId
			 && string.IsNullOrEmpty(perShaderData.searchString)
			 && Event.current.type == UnityEngine.EventType.Repaint)
			{
				using (new EditorGUI.DisabledScope(true))
				{
					var disableTextRect = new Rect(rect.x, rect.y, rect.width,
												   Helper.guiStyles_ToolbarSearchTextFieldPopup.fixedHeight > 0.0
													   ? Helper.guiStyles_ToolbarSearchTextFieldPopup.fixedHeight
													   : rect.height);
					disableTextRect = Helper.guiStyles_ToolbarSearchTextFieldPopup.padding.Remove(disableTextRect);
					int fontSize = EditorStyles.label.fontSize;
					EditorStyles.label.fontSize = Helper.guiStyles_ToolbarSearchTextFieldPopup.fontSize;
					EditorStyles.label.Draw(disableTextRect, new GUIContent(perShaderData.searchMode.ToString()), false, false, false, false);
					EditorStyles.label.fontSize = fontSize;
				}
			}

			if (hasChanged) perShaderData.UpdateSearchFilter();

			return hasChanged;
		}

		#endregion

    }
}