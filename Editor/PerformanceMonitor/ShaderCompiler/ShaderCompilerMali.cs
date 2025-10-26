// Copyright (c) Jason Ma

using System;
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using UnityEngine;
using LWGUI.PerformanceMonitor;
using UnityEditor;

namespace LWGUI.PerformanceMonitor.ShaderCompiler
{
    public class ShaderCompilerMali : IShaderCompiler
    {
        public ShaderCompilerPlatform Api    { get; private set; }
        public BuildTarget            Target { get; private set; }
        public GraphicsTier           Tier   { get; private set; }

        public string CompilerName => "Mali (placeholder)";

        public bool isSupportCurrentPlatform => true;

        public string GetCompiledShaderPath(ShaderPerfData shaderPerfData, string compiledShaderDirectory, string shaderTypeName)
        {
            throw new NotImplementedException();
        }

        public bool CompilePass(ShaderPerfData shaderPerfData, ShaderData.Pass pass, ShaderType shaderType, string[] keywords, out string compiledShader)
        {
            throw new NotImplementedException();
        }

        public ShaderCompilerMali(ShaderCompilerPlatform api, BuildTarget target, GraphicsTier tier)
        {
            Api = api;
            Target = target;
            Tier = tier;
        }


        public object AnalyzeShaderPerformance(ShaderPerfData shaderPerfData, string compiledShader)
        {
            return null;
        }

        public void DrawShaderPerformanceStatsLine(ShaderPerfData shaderPerfData)
        {
        }
    }
}