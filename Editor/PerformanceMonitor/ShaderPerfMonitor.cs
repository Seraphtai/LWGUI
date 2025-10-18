// Copyright (c) Jason Ma
using System;
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
    public class ShaderPerfMonitor
    {
        #region Global Settings
        
        public static GraphicsTier graphicsTier = (GraphicsTier)(-1);
        public static ShaderCompilerPlatform shaderCompilerPlatform = ShaderCompilerPlatform.D3D;
        public static BuildTarget buildTarget = BuildTarget.StandaloneWindows64;
        
        #endregion
        
        public static bool IsCalculating { get; private set; }
        
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

            for (int i = 0; i < subshader.PassCount; i++)
            {
                var pass = subshader.GetPass(i);
                var passNameOrIndex = string.IsNullOrEmpty(pass.Name) ? i.ToString() : pass.Name;
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
                        passName = passNameOrIndex,
                        shaderType = shaderType,
                        hash = hash,
                    };

                    shaderPerfData.shaderCacheNamePrefix = IOHelper.GetShaderCacheNamePrefix(shaderPerfData);
                    shaderPerfData.compiledShaderDirectory = IOHelper.GetCompiledShaderVariantCacheDirectory(shader, shaderPerfData);
                    shaderPerfData.compiledBinaryDxbcShaderPath = Path.Combine(shaderPerfData.compiledShaderDirectory, shaderPerfData.shaderCacheNamePrefix + ".dxbc");
                    shaderPerfData.compiledReadableShaderPath = Path.Combine(shaderPerfData.compiledShaderDirectory, shaderPerfData.shaderCacheNamePrefix + ".txt");
                    
                    // Compile and create cache
                    if (!File.Exists(shaderPerfData.compiledBinaryDxbcShaderPath))
                    {
                        var compileInfo = pass.CompileVariant(shaderType, keywords.ToArray(), shaderCompilerPlatform, buildTarget, graphicsTier, true);
                        var compiledShader = compileInfo.Success ? compileInfo.ShaderData : Array.Empty<byte>();
                        IOHelper.WriteBinaryFile(shaderPerfData.compiledBinaryDxbcShaderPath, compiledShader);
                    }
                    
                    shaderPerfData.isCompiledSuccessful = File.Exists(shaderPerfData.compiledBinaryDxbcShaderPath) 
                                                            && new FileInfo(shaderPerfData.compiledBinaryDxbcShaderPath).Length > 1;
                    
                    if (shaderPerfData.isCompiledSuccessful)
                    {
                        // Analyze Shader Performance
                        shaderPerfData.stats = ShaderCompilerDefaultFxc.AnalyzeShaderPerformance(shaderPerfData);
                        
                        if (!shaderPerfData.stats.isValid)
                            Debug.LogError($"LWGUI: Failed to Analyze Shader: { shader.name } | Subshader: { shaderPerfData.subshaderIndex } | Pass: { passNameOrIndex } | Stage: { shaderType }\n" +
                                           $"Keywords: \n{ string.Join('\n', keywords) }");
                    }
                    
                    output.Add(shaderPerfData);
                }
            }
            
            return output;
        }

        public static void ClearShaderPerfCaches(Shader shader)
        {
            
        }
    }
}