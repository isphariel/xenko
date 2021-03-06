﻿// <auto-generated>
// Do not edit this file yourself!
//
// This code was generated by Xenko Shader Mixin Code Generator.
// To generate it yourself, please install SiliconStudio.Xenko.VisualStudio.Package .vsix
// and re-save the associated .xkfx.
// </auto-generated>

using System;
using SiliconStudio.Core;
using SiliconStudio.Xenko.Rendering;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Shaders;
using SiliconStudio.Core.Mathematics;
using Buffer = SiliconStudio.Xenko.Graphics.Buffer;

namespace SiliconStudio.Xenko.Rendering
{
    public static partial class CameraKeys
    {
        public static readonly ValueParameterKey<float> NearClipPlane = ParameterKeys.NewValue<float>(1.0f);
        public static readonly ValueParameterKey<float> FarClipPlane = ParameterKeys.NewValue<float>(100.0f);
        public static readonly ValueParameterKey<Vector2> ZProjection = ParameterKeys.NewValue<Vector2>();
        public static readonly ValueParameterKey<Vector2> ViewSize = ParameterKeys.NewValue<Vector2>();
        public static readonly ValueParameterKey<float> AspectRatio = ParameterKeys.NewValue<float>();
    }
}
