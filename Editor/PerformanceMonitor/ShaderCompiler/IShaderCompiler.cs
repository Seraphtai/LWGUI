// Copyright (c) Jason Ma

using System;
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using UnityEditor;

namespace LWGUI.PerformanceMonitor.ShaderCompiler
{
    public interface IShaderCompiler
    {
        ShaderCompilerPlatform Api                      { get; }
        BuildTarget            Target                   { get; }
        GraphicsTier           Tier                     { get; }
        string                 CompilerName             { get; }
        bool                   isSupportCurrentPlatform { get; }

        /// <summary>
        /// The path to the Shader compilation result stored in text.
        /// </summary>
        string GetCompiledShaderPath(ShaderPerfData shaderPerfData, string compiledShaderDirectory, string shaderTypeName);

        /// <summary>
        /// Try to compile a pass variant. Returns true and outputs string on success.
        /// </summary>
        bool CompilePass(ShaderPerfData shaderPerfData, ShaderData.Pass pass, ShaderType shaderType, string[] keywords,
                         out string     compiledShader);

        /// <summary>
        /// Analyze a compiled shader (or readable asm) and return a compiler-specific stats object.
        /// The returned object is opaque to the caller and will be stored in ShaderPerfData.stats.
        /// Return null on failure.
        /// </summary>
        object AnalyzeShaderPerformance(ShaderPerfData shaderPerfData, string compiledShader);

        /// <summary>
        /// Draw a single line (pass) of shader performance UI inside the toolbar area.
        /// Compiler-specific UI (Find/Open buttons, label contents) should be implemented here.
        /// </summary>
        void DrawShaderPerformanceStatsLine(ShaderPerfData shaderPerfData);
    }
}