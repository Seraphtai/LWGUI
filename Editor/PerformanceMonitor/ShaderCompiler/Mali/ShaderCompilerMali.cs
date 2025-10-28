// Copyright (c) Jason Ma

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEditor;
using LWGUI.PerformanceMonitor;
using LWGUI.PerformanceMonitor.ShaderCompiler.Mali;

namespace LWGUI.PerformanceMonitor.ShaderCompiler
{
    public class ShaderCompilerMali : IShaderCompiler
    {
        private static int _isSupportCurrentPlatform = -1;
        public static bool isSupportCurrentPlatform
        {
            get
            {
                // if (_isSupportCurrentPlatform == -1)
                {
                    if (!IOHelper.RunCMD("malioc --list", out var output) || string.IsNullOrWhiteSpace(output) || !output.Contains("Compiler"))
                        _isSupportCurrentPlatform = 0;
                    else
                        _isSupportCurrentPlatform = 1;
                }

                return _isSupportCurrentPlatform == 1;
            }
        }

        public string compilerName => "Mali Offline Compiler";

        public ShaderCompilerPlatform Api    { get; private set; }
        public BuildTarget            Target { get; private set; }
        public GraphicsTier           Tier   { get; private set; }


        public ShaderCompilerMali(ShaderCompilerPlatform api, BuildTarget target, GraphicsTier tier)
        {
            Api = api;
            Target = target;
            Tier = tier;
        }

        public string GetCompiledShaderPath(ShaderPerfData shaderPerfData, string compiledShaderDirectory, string shaderTypeName)
        {
            string ext;
            switch (shaderPerfData.shaderType)
            {
                // https://developer.arm.com/documentation/101863/8-8/Using-Mali-Offline-Compiler/Compiling-OpenGL-ES-shaders
                case ShaderType.Vertex:   ext = ".vert"; break;
                case ShaderType.Fragment: ext = ".frag"; break;
                case ShaderType.Geometry: ext = ".geom"; break;
                default:                  return null;
            }

            return Path.Combine(compiledShaderDirectory, $"Mali_{Api}_{Target}_{shaderTypeName}{ext}");
        }

        public string GetMaliJsonOutputPath(ShaderPerfData shaderPerfData)
            => Path.Combine(shaderPerfData.compiledShaderDirectory, $"Mali_{Api}_{Target}_{shaderPerfData.shaderTypeName}.json");


        public bool CompilePass(ShaderPerfData shaderPerfData, ShaderData.Pass pass, ShaderType shaderType, string[] keywords,
                                out string     compiledShader)
        {
            compiledShader = string.Empty;

            if (shaderPerfData == null || pass == null || keywords == null)
                return false;

            var compileInfo = pass.CompileVariant(shaderType, keywords, Api, Target, Tier, true);
            if (!compileInfo.Success)
                return false;

            compiledShader = Encoding.UTF8.GetString(compileInfo.ShaderData);

            // Fix Mali Compiler Errors
            compiledShader = compiledShader.Replace("#version 300 es", "#version 320 es");
            IOHelper.WriteTextFile(shaderPerfData.compiledShaderPath, compiledShader);

            return !string.IsNullOrWhiteSpace(compiledShader);
        }


        public object AnalyzeShaderPerformance(ShaderPerfData shaderPerfData, string compiledShader)
        {
            var jsonPath = GetMaliJsonOutputPath(shaderPerfData);
            string jsonString = string.Empty;

            if (!File.Exists(jsonPath))
            {
                // https://developer.arm.com/documentation/101863/8-8/Using-Mali-Offline-Compiler/Compiling-OpenGL-ES-shaders
                IOHelper.RunCMD($"malioc --core Mali-G76 --format json \"{shaderPerfData.compiledShaderPath}\"", out jsonString);
                IOHelper.WriteTextFile(jsonPath, jsonString);
            }
            else
            {
                jsonString = IOHelper.ReadTextFile(jsonPath);
            }

            if (IOHelper.ExistAndNotEmpty(jsonPath))
            {
                return MaliocOutputParser.Parse(jsonString);
            }

            return null;
        }

        public void DrawShaderPerformanceStatsHeader(LWGUIMetaDatas metaDatas)
        {
            EditorGUILayout.LabelField(" ", "Cycles, A = Arithmetic, LS = Load/Store, V = Varying, T = Texture");
        }

        // https://developer.arm.com/documentation/101863/8-8/Using-Mali-Offline-Compiler/Performance-analysis
        public void DrawShaderPerformanceStatsLine(LWGUIMetaDatas metaDatas, ShaderPerfData shaderPerfData)
        {
            EditorGUILayout.BeginHorizontal();

            var statsObj = shaderPerfData.stats;
            RuntimeMaliocShader stats = statsObj is RuntimeMaliocShader ? statsObj as RuntimeMaliocShader : null;
            if (stats is { Variants                      : { Count: > 0 } }
                && stats.Variants[0].Pipelines is { Count: > 0 })
            {
                var variant = stats.Variants[0];
                var cycles = Enumerable.Repeat(0.0f, variant.Pipelines.Count).ToList();

                for (int i = 0; i < variant.Pipelines.Count; i++)
                {
                    cycles[i] = Mathf.Max(Mathf.Max(variant.ShortestPathCycles.PipelineCycles[i],
                            variant.LongestPathCycles.PipelineCycles[i]),
                        variant.TotalCycles.PipelineCycles[i]);
                }

                // https://developer.arm.com/documentation/101863/8-8/Using-Mali-Offline-Compiler/Performance-analysis/Performance-table
                float arithmeticCycle = 0;
                float loadStoreCycle = 0;
                float varyingCycle = 0;
                float textureCycle = 0;
                for (int i = 0; i < variant.Pipelines.Count; i++)
                {
                    switch (variant.Pipelines[i])
                    {
                        case RuntimeMaliocShader.ShaderVariantPipelineType.Arithmetic: arithmeticCycle = cycles[i]; break;
                        case RuntimeMaliocShader.ShaderVariantPipelineType.LoadStore:  loadStoreCycle = cycles[i]; break;
                        case RuntimeMaliocShader.ShaderVariantPipelineType.Varying:    varyingCycle = cycles[i]; break;
                        case RuntimeMaliocShader.ShaderVariantPipelineType.Texture:    textureCycle = cycles[i]; break;
                    }
                }

                var statsStr = $"A: {arithmeticCycle,-5:0.0}\tLS: {loadStoreCycle,-6:0.0}\tV: {varyingCycle,-5:0.0}\tT: {textureCycle,-5:0.0}";
                EditorGUILayout.LabelField($"{shaderPerfData.passName} | {shaderPerfData.shaderTypeName}", statsStr);

                ToolbarHelper.DrawShaderPerformanceStatsLineButtons(shaderPerfData);
            }
            else
            {
                var status = shaderPerfData.isCompiledSuccessful ? "ANALYSIS FAILED" : "COMPILATION FAILED";
                EditorGUILayout.LabelField($"{shaderPerfData.passName} | {shaderPerfData.shaderTypeName}", status);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}