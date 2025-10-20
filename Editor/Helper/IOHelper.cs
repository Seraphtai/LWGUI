// Copyright (c) Jason Ma

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using LWGUI.PerformanceMonitor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LWGUI
{
    public static class IOHelper
    {
        #region Paths

        private static string _cachedProjectPath;
        public static string ProjectPath
        {
            get
            {
                if (string.IsNullOrEmpty(_cachedProjectPath))
                    _cachedProjectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                return _cachedProjectPath;
            }
        }

        private static string _cachedCompiledShaderCachePath;
        public static string CompiledShaderCacheRootPath
        {
            get
            {
                if (string.IsNullOrEmpty(_cachedCompiledShaderCachePath))
                    _cachedCompiledShaderCachePath = Path.Combine(ProjectPath, "Library", "LWGUI", "ShaderPerfCache");
                return _cachedCompiledShaderCachePath;
            }
        }

        #endregion

        #region File

        public static bool ExistAndNotEmpty(string filePath) => File.Exists(filePath) && new FileInfo(filePath).Length > 1;

        public static void WriteBinaryFile(string filePath, byte[] bytes)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllBytes(filePath, bytes);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        public static void WriteTextFile(string filePath, string text)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, text, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        #endregion

        #region Process

        public static bool RunProcess(string file, string args, 
            out string output)
        {
            output = string.Empty;
            
            var psi = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var p = Process.Start(psi);
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            p.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) stdout.AppendLine(e.Data);
            };
            p.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) stderr.AppendLine(e.Data);
            };

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();

            output = stdout.ToString();
                
            if (p.ExitCode != 0)
            {
                Debug.LogError($"LWGUI: Process Exit Code { p.ExitCode }: { stderr }" +
                               $"File: { file }\n" +
                               $"Args: { args }");
                output = stderr.ToString();
                return false;
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                output = stderr.ToString();
                return false;
            }

            return true;
        }

        #endregion

        #region Performance Monitor

        public static string GetCompiledShaderVariantCacheDirectory(Shader shader, ShaderPerfData shaderPerfData)
        {
            var shaderPath = AssetDatabase.Contains(shader) ? AssetDatabase.GetAssetPath(shader) : null;
            var shaderCachePath = shaderPath != null
                // Assets/Shaders/Lit.shader => Shaders/Lit
                ? Path.Combine(Path.GetDirectoryName(shaderPath[7..]) ?? string.Empty, Path.GetFileNameWithoutExtension(shaderPath))
                : shader.name.Replace('/', '_').Replace('\\', '_');
             
            return Path.Combine(CompiledShaderCacheRootPath, 
                shaderCachePath,
                shaderPerfData.subshaderIndex.ToString(),
                shaderPerfData.passName,
                shaderPerfData.hash);
        }

        public static void ClearShaderCache(Shader shader)
        {
            try
            {
                var shaderDir = Path.Combine(CompiledShaderCacheRootPath, shader.name);
                if (Directory.Exists(shaderDir)) 
                    Directory.Delete(shaderDir, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"LWGUI: Cleaning the Shader({ shader.name }) cache failed: { e.Message }");
            }
        }


        #endregion

    }
}