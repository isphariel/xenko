// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.ComponentModel;
using SiliconStudio.Assets;
using SiliconStudio.Core;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization.Contents;
using SiliconStudio.Paradox.Assets.Materials.Processor.Visitors;
using SiliconStudio.Paradox.Assets.Textures;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Shaders;

namespace SiliconStudio.Paradox.Assets.Materials.Nodes
{
    [ContentSerializer(typeof(DataContentSerializer<MaterialTextureNode>))]
    [DataContract("MaterialTextureNode")]
    [Display("Texture")]
    public class MaterialTextureNode : MaterialNodeBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public MaterialTextureNode()
            : this(null, TextureCoordinate.Texcoord0, Vector2.One, Vector2.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialTextureNode"/> class.
        /// </summary>
        /// <param name="texturePath">Name of the texture.</param>
        /// <param name="texcoordIndex">Index of the texcoord.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="offset">The offset.</param>
        public MaterialTextureNode(string texturePath, TextureCoordinate texcoordIndex, Vector2 scale, Vector2 offset)
        {
            if (!string.IsNullOrEmpty(texturePath))
            {
                TextureReference = new AssetReference<TextureAsset>(Guid.Empty, new UFile(texturePath));
            }
            Channel = TextureChannel.RGBA;
            TexcoordIndex = texcoordIndex;
            Sampler = new NodeParameterSampler();
            Scale = scale;
            Offset = offset;
            Key = null;
            UsedParameterKey = null;
        }

        /// <summary>
        /// The texture Reference.
        /// </summary>
        /// <userdoc>
        /// The texture.
        /// </userdoc>
        [DataMember(10)] 
        [DefaultValue(null)]
        [Display("Texture")]
        public AssetReference<TextureAsset> TextureReference { get; set; }

        /// <summary>
        /// Gets or sets the channel.
        /// </summary>
        /// <value>The channel.</value>
        /// <userdoc>Selects the RGBA channel to sample from the texture.</userdoc>
        [DataMember(20)]
        [DefaultValue(TextureChannel.RGBA)]
        public TextureChannel Channel { get; set; }

        /// <summary>
        /// The texture coordinate used to sample the texture.
        /// </summary>
        /// <userdoc>
        /// The set of uv used to sample the texture.
        /// </userdoc>
        [DataMember(30)]
        [DefaultValue(TextureCoordinate.Texcoord0)]
        public TextureCoordinate TexcoordIndex { get; set; }

        /// <summary>
        /// The sampler of the texture.
        /// </summary>
        /// <userdoc>
        /// The sampler of the texture.
        /// </userdoc>
        [DataMemberIgnore]
        internal NodeParameterSampler Sampler { get; private set; }

        /// <summary>
        /// The texture filtering mode.
        /// </summary>
        /// <userdoc>
        /// The filtering of the texture.
        /// </userdoc>
        [DataMember(41)]
        [DefaultValue(TextureFilter.Linear)]
        public TextureFilter Filtering
        {
            get
            {
                return Sampler.Filtering;
            }
            set
            {
                Sampler.Filtering = value;
            }
        }

        /// <summary>
        /// The texture address mode.
        /// </summary>
        /// <userdoc>
        /// The wrapping of the texture along U.
        /// </userdoc>
        [DataMember(42)]
        [DefaultValue(TextureAddressMode.Wrap)]
        public TextureAddressMode AddressModeU
        {
            get
            {
                return Sampler.AddressModeU;
            }
            set
            {
                Sampler.AddressModeU = value;
            }
        }

        /// <summary>
        /// The texture address mode.
        /// </summary>
        /// <userdoc>
        /// The wrapping of the texture along V.
        /// </userdoc>
        [DataMember(43)]
        [DefaultValue(TextureAddressMode.Wrap)]
        public TextureAddressMode AddressModeV
        {
            get
            {
                return Sampler.AddressModeV;
            }
            set
            {
                Sampler.AddressModeV = value;
            }
        }

        /// <summary>
        /// The scale of the texture coordinates.
        /// </summary>
        /// <userdoc>
        /// The scale on texture coordinates. Lower than 1 means that the texture will be zoomed in.
        /// </userdoc>
        [DataMember(50)]
        public Vector2 Scale { get; set; }

        /// <summary>
        /// The offset in the texture coordinates.
        /// </summary>
        /// <userdoc>
        /// The offset on texture coordinates.
        /// </userdoc>
        [DataMember(60)]
        public Vector2 Offset { get; set; }

        /// <summary>
        /// The desired parameter key.
        /// </summary>
        /// <userdoc>
        /// The key to access the texture at runtime. 
        /// </userdoc>
        [DataMember(80)]
        [DefaultValue(null)]
        public ParameterKey<Graphics.Texture> Key { get; set; }

        /// <summary>
        /// The parameter key used in the shader.
        /// </summary>
        [DataMemberIgnore]
        internal ParameterKey<Graphics.Texture> UsedParameterKey { get; set; }

        /// <summary>
        /// The name of the texture;
        /// </summary>
        public string TextureName
        {
            get
            {
                return TextureReference != null && TextureReference.Location != null ? TextureReference.Location : null;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Texture";
        }

        public override ShaderSource GenerateShaderSource(MaterialShaderGeneratorContext shaderGeneratorContext)
        {
            // TODO: Use a generated UsedTexcoordIndex when backing textures
            var usedTexcoord = "TEXCOORD" + MaterialUtility.GetTextureIndex(TexcoordIndex);

            // "TTEXTURE", "TStream"
            ShaderClassSource shaderSource;
            if (Offset != Vector2.Zero)
                shaderSource = new ShaderClassSource("ComputeColorTextureScaledOffsetSampler", UsedParameterKey, usedTexcoord, MaterialUtility.GetAsShaderString(Scale), MaterialUtility.GetAsShaderString(Offset), Sampler.SamplerParameterKey, MaterialUtility.GetAsShaderString(Channel));
            else if (Scale != Vector2.One)
                shaderSource = new ShaderClassSource("ComputeColorTextureScaledSampler", UsedParameterKey, usedTexcoord, MaterialUtility.GetAsShaderString(Scale), Sampler.SamplerParameterKey, MaterialUtility.GetAsShaderString(Channel));
            else
                shaderSource = new ShaderClassSource("ComputeColorTextureSampler", UsedParameterKey, usedTexcoord, Sampler.SamplerParameterKey, MaterialUtility.GetAsShaderString(Channel));

            return shaderSource;            
        }
    }
}
