// Copyright (c) Jason Ma

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LWGUI
{
	/// <summary>
	/// Control the show or hide of a single or a group of properties based on multiple conditions.
	/// 
	/// logicalOperator: And | Or (Default: And).
	/// propName: Target Property Name used for comparison.
	/// compareFunction: Less (L) | Equal (E) | LessEqual (LEqual / LE) | Greater (G) | NotEqual (NEqual / NE) | GreaterEqual (GEqual / GE).
	/// value: Target Property Value used for comparison.
	/// </summary>
	public class ShowIfDecorator : SubDrawer
	{
		public enum LogicalOperator
		{
			And,
			Or
		}

		public class ShowIfData
		{
			public LogicalOperator logicalOperator    = LogicalOperator.And;
			public string          targetPropertyName = string.Empty;
			public CompareFunction compareFunction    = CompareFunction.Equal;
			public float           value              = 0;
		}

		public ShowIfData showIfData = new();
		
		private readonly Dictionary<string, string> _compareFunctionLUT = new()
		{
			{ "Less", "Less" },
			{ "L", "Less" },
			{ "Equal", "Equal" },
			{ "E", "Equal" },
			{ "LessEqual", "LessEqual" },
			{ "LEqual", "LessEqual" },
			{ "LE", "LessEqual" },
			{ "Greater", "Greater" },
			{ "G", "Greater" },
			{ "NotEqual", "NotEqual" },
			{ "NEqual", "NotEqual" },
			{ "NE", "NotEqual" },
			{ "GreaterEqual", "GreaterEqual" },
			{ "GEqual", "GreaterEqual" },
			{ "GE", "GreaterEqual" },
		};

		public ShowIfDecorator(string propName, string comparisonMethod, float value) : this("And", propName, comparisonMethod, value) { }

		public ShowIfDecorator(string logicalOperator, string propName, string compareFunction, float value)
		{
			showIfData.logicalOperator = logicalOperator.ToLower() == "or" ? LogicalOperator.Or : LogicalOperator.And;
			showIfData.targetPropertyName = propName;
			if (!_compareFunctionLUT.ContainsKey(compareFunction) || !Enum.IsDefined(typeof(CompareFunction), _compareFunctionLUT[compareFunction]))
				Debug.LogError("LWGUI: Invalid compareFunction: '"
							 + compareFunction
							 + "', Must be one of the following: Less (L) | Equal (E) | LessEqual (LEqual / LE) | Greater (G) | NotEqual (NEqual / NE) | GreaterEqual (GEqual / GE).");
			else
				showIfData.compareFunction = (CompareFunction)Enum.Parse(typeof(CompareFunction), _compareFunctionLUT[compareFunction]);
			showIfData.value = value;
		}

		private static void Compare(ShowIfData showIfData, float targetValue, ref bool result)
		{
			bool compareResult;

			switch (showIfData.compareFunction)
			{
				case CompareFunction.Less:
					compareResult = targetValue < showIfData.value;
					break;
				case CompareFunction.LessEqual:
					compareResult = targetValue <= showIfData.value;
					break;
				case CompareFunction.Greater:
					compareResult = targetValue > showIfData.value;
					break;
				case CompareFunction.NotEqual:
					compareResult = targetValue != showIfData.value;
					break;
				case CompareFunction.GreaterEqual:
					compareResult = targetValue >= showIfData.value;
					break;
				default:
					compareResult = targetValue == showIfData.value;
					break;
			}

			switch (showIfData.logicalOperator)
			{
				case LogicalOperator.And:
					result &= compareResult;
					break;
				case LogicalOperator.Or:
					result |= compareResult;
					break;
			}
		}

		public static bool GetShowIfResultToFilterDrawerApplying(MaterialProperty prop)
		{
			var material = prop.targets[0] as Material;
			var showIfDatas = new List<ShowIfData>();
			{
				var drawer = ReflectionHelper.GetPropertyDrawer(material.shader, prop, out var decoratorDrawers);
				if (decoratorDrawers != null && decoratorDrawers.Count > 0)
				{
					foreach (ShowIfDecorator showIfDecorator in decoratorDrawers.Where(drawer => drawer is ShowIfDecorator))
					{
						showIfDatas.Add(showIfDecorator.showIfData);
					}
				}
				else
				{
					return true;
				}
			}

			return GetShowIfResultFromMaterial(showIfDatas, material);
		}
		
		public static bool GetShowIfResultFromMaterial(List<ShowIfData> showIfDatas, Material material)
		{
			bool result = true;
			foreach (var showIfData in showIfDatas)
			{
				var targetValue = material.GetFloat(showIfData.targetPropertyName);
				Compare(showIfData, targetValue, ref result);
			}

			return result;
		}
		
		public static void GetShowIfResult(PropertyStaticData propStaticData, PropertyDynamicData propDynamicData, PerMaterialData perMaterialData)
		{
			foreach (var showIfData in propStaticData.showIfDatas)
			{
				var targetValue = perMaterialData.propDynamicDatas[showIfData.targetPropertyName].property.floatValue;
				Compare(showIfData, targetValue, ref propDynamicData.isShowing);
			}
		}

		protected override float GetVisibleHeight(MaterialProperty prop) { return 0; }

		public override void BuildStaticMetaData(Shader inShader, MaterialProperty inProp, MaterialProperty[] inProps, PropertyStaticData inoutPropertyStaticData)
		{
			inoutPropertyStaticData.showIfDatas.Add(showIfData);
		}

		public override void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor) { }
	}
}
