// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System.ComponentModel;

using SiliconStudio.Core;

namespace SiliconStudio.Assets.Templates
{
    /// <summary>
    /// A template for using an existing package as a template, expecting a <see cref="Package"/> to be accessible 
    /// from <see cref="TemplateDescription.FullPath"/> with the same name as this template.
    /// </summary>
    [DataContract("TemplateSample")]
    public class TemplateSampleDescription : TemplateDescription
    {
        /// <summary>
        /// Gets or sets the name of the pattern used to substitute files and content. If null, use the 
        /// <see cref="TemplateDescription.DefaultOutputName"/>.
        /// </summary>
        /// <value>The name of the pattern.</value>
        [DataMember(70)]
        [DefaultValue(null)]
        public string PatternName { get; set; }
    }
}
