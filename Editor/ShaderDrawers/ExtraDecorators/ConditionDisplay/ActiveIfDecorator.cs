// Copyright (c) Jason Ma

using UnityEditor;
using UnityEngine;

namespace LWGUI
{
	/// <summary>
	/// Control whether the property can be edited based on multiple conditions.
	///
	/// logicalOperator: And | Or (Default: And).
	/// propName: Target Property Name used for comparison.
	/// compareFunction: Less (L) | Equal (E) | LessEqual (LEqual / LE) | Greater (G) | NotEqual (NEqual / NE) | GreaterEqual (GEqual / GE).
	/// value: Target Property Value used for comparison.
	/// </summary>
	public class ActiveIfDecorator : SubDrawer
	{
		public ShowIfDecorator.ShowIfData activeIfData = new();

		public ActiveIfDecorator(string propName, string comparisonMethod, float value) : this("And", propName, comparisonMethod, value) { }

		public ActiveIfDecorator(string logicalOperator, string propName, string compareFunction, float value)
		{
			activeIfData.logicalOperator = logicalOperator.ToLower() == "or" ? ShowIfDecorator.LogicalOperator.Or : ShowIfDecorator.LogicalOperator.And;
			activeIfData.targetPropertyName = propName;
			activeIfData.compareFunction = ShowIfDecorator.ParseCompareFunction(compareFunction);
			activeIfData.value = value;
		}

		protected override float GetVisibleHeight(MaterialProperty prop) { return 0; }

		public override void BuildStaticMetaData(Shader inShader, MaterialProperty inProp, MaterialProperty[] inProps, PropertyStaticData inoutPropertyStaticData)
		{
			inoutPropertyStaticData.activeIfDatas.Add(activeIfData);
		}

		public override void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor) { }
	}
}
