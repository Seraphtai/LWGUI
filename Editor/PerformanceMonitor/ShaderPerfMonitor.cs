// Copyright (c) Jason Ma

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LWGUI.PerformanceMonitor.ShaderCompiler;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace LWGUI.PerformanceMonitor
{
    public static class ShaderPerfMonitor
    {
        #region Global Settings

        public static GraphicsTier           graphicsTier           = (GraphicsTier)(-1);
        public static ShaderCompilerPlatform shaderCompilerPlatform = ShaderCompilerPlatform.D3D;
        public static BuildTarget            buildTarget            = BuildTarget.StandaloneWindows64;

        #endregion

        public static List<string> GetMaterialAndGlobalActiveKeywords(Material material)
        {
            var output = new List<string>();

            foreach (var localKeyword in material.enabledKeywords)
            {
                output.Add(localKeyword.name);
            }

            foreach (var keyword in material.shader.keywordSpace.keywords)
            {
                // Is a global keyword?
                if (keyword.isOverridable)
                {
                    if (Shader.IsKeywordEnabled(keyword.name) && !output.Contains(keyword.name))
                        output.Add(keyword.name);
                }
            }

            return output;
        }

        public static string ComputeShaderVariantHash(List<string> keywords, BuildTarget target, ShaderCompilerPlatform platform, GraphicsTier tier)
        {
            var sorted = keywords?.OrderBy(k => k) ?? Enumerable.Empty<string>();
            var key = string.Join(";", sorted) + $"|{target}|{platform}|{tier}";
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(key);
            var hash = md5.ComputeHash(bytes);
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }

        public static List<ShaderPerfData> GetShaderVariantPerfDatas(Shader shader, List<string> keywords)
        {
            var output = new List<ShaderPerfData>();
            var shaderData = ShaderUtil.GetShaderData(shader);
            var subshader = shaderData.ActiveSubshader;

            IShaderCompiler compiler = GetActiveCompiler();

            for (int i = 0; i < subshader.PassCount; i++)
            {
                var pass = subshader.GetPass(i);
                for (int j = 0; j < (int)ShaderType.Count; j++)
                {
                    var shaderType = (ShaderType)j;
                    if (!pass.HasShaderStage(shaderType))
                        continue;

                    // Collect input data
                    var hash = ComputeShaderVariantHash(keywords, buildTarget, shaderCompilerPlatform, graphicsTier);
                    var shaderPerfData = new ShaderPerfData
                    {
                        subshaderIndex = shaderData.ActiveSubshaderIndex,
                        passIndex = i,
                        passName = IOHelper.GetValidFileName(pass.Name),
                        shaderType = shaderType,
                        hash = hash,
                    };

                    shaderPerfData.shaderTypeName = shaderPerfData.shaderType.ToString();
                    shaderPerfData.compiledShaderDirectory = IOHelper.GetCompiledShaderVariantCacheDirectory(shader, shaderPerfData);

                    if (compiler != null)
                    {
                        shaderPerfData.compiledShaderPath = compiler.GetCompiledShaderPath(shaderPerfData, shaderPerfData.compiledShaderDirectory, shaderPerfData.shaderTypeName);

                        if (!string.IsNullOrEmpty(shaderPerfData.compiledShaderPath))
                        {
                            // Compile and create cache
                            shaderPerfData.isCompiledSuccessful = true;
                            string compiledShader;
                            if (!File.Exists(shaderPerfData.compiledShaderPath))
                            {
                                shaderPerfData.isCompiledSuccessful = compiler.CompilePass(shaderPerfData, pass, shaderType, keywords.ToArray(),
                                    out compiledShader);
                                IOHelper.WriteTextFile(shaderPerfData.compiledShaderPath, compiledShader);
                            }
                            else
                            {
                                compiledShader = IOHelper.ReadTextFile(shaderPerfData.compiledShaderPath);
                            }

                            shaderPerfData.isCompiledSuccessful &= IOHelper.ExistAndNotEmpty(shaderPerfData.compiledShaderPath);

                            // Analyze performance
                            if (shaderPerfData.isCompiledSuccessful)
                            {
                                shaderPerfData.stats = compiler.AnalyzeShaderPerformance(shaderPerfData, compiledShader);
                                
                                if (shaderPerfData.stats == null)
                                    Debug.LogError($"LWGUI: Failed to Analyze Shader: {shader.name} | Subshader: {shaderPerfData.subshaderIndex} | Pass: {shaderPerfData.passName} | Stage: {shaderType}\n" +
                                                   $"Keywords: \n{string.Join('\n', keywords)}");
                            }
                            else
                            {
                                Debug.LogError($"LWGUI: Failed to Compile Shader: {shader.name} | Subshader: {shaderPerfData.subshaderIndex} | Pass: {shaderPerfData.passName} | Stage: {shaderType}\n" +
                                               $"Keywords: \n{string.Join('\n', keywords)}");
                            }
                        }
                        else
                        {
                            Debug.LogError("LWGUI: Unable to get the compiled Shader path!");
                            break;
                        }
                    }

                    output.Add(shaderPerfData);
                }
            }

            return output;
        }

        public static void ClearShaderPerfCache(Shader shader)
        {
            IOHelper.ClearShaderPerfCache(shader);
        }

        public static IShaderCompiler GetActiveCompiler()
        {
            if (ShaderCompilerMali.isSupportCurrentPlatform)
                return ShaderCompilerMali.instance;
            
            return ShaderCompilerDefaultFxc.instance;
        }
    }
}