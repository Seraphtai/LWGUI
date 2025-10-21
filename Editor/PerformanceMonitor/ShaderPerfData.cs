// Copyright (c) Jason Ma

using System;
using System.Collections.Generic;
using UnityEditor.Rendering;

namespace LWGUI.PerformanceMonitor
{
    public struct ShaderPerfStats
    {
        public float estimatedCost; // Estimated relative performance cost based on experience, not precise results.
        public int sampleCount;
        public int samplerCount;
        public int registerCount;
        public int interpolatorChannelCount;

        public bool isValid;
    }

    public class ShaderPerfData
    {
        public int subshaderIndex                   = -1;
        public int passIndex                        = -1;
        public string passName                      = string.Empty;
        public ShaderType shaderType;
        public string hash                          = string.Empty;
        public bool isCompiledSuccessful            = false;
        
        // Path
        public string shaderTypeName               = string.Empty;
        public string compiledShaderDirectory      = string.Empty;
        public string compiledBinaryDxbcShaderPath = string.Empty;
        public string compiledReadableShaderPath   = string.Empty;

        public ShaderPerfStats stats;
    }
}