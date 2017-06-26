// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System;
using System.Linq;

namespace SiliconStudio.Core.Serialization.Contents
{
    /// <summary>
    /// Specifies settings for <see cref="ContentManager.Load{T}"/> operations.
    /// </summary>
    public sealed class ContentManagerLoaderSettings
    {
        public delegate void ContentFilterDelegate(ILoadableReference reference, ref bool shouldBeLoaded);

        /// <summary>
        /// Gets the default loader settings.
        /// </summary>
        /// <value>
        /// The default loader settings.
        /// </value>
        public static ContentManagerLoaderSettings Default { get; } = new ContentManagerLoaderSettings();

        /// <summary>
        /// Gets the loader settings which doesn't load content references.
        /// </summary>
        /// <value>
        /// The loader settings which doesn't load content references.
        /// </value>
        public static ContentManagerLoaderSettings IgnoreReferences { get; } = new ContentManagerLoaderSettings { LoadContentReferences = false };

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="IReference"/> should be loaded.
        /// </summary>
        /// <value>
        ///   <c>true</c> if <see cref="IReference"/> should be loaded; otherwise, <c>false</c>.
        /// </value>
        public bool LoadContentReferences { get; set; } = true;

        /// <summary>
        /// Gets or sets a filter that can indicate whether <see cref="IReference"/> should be loaded.
        /// </summary>
        /// <value>
        /// The content reference filter.
        /// </value>
        public ContentFilterDelegate ContentFilter { get; set; }

        /// <summary>
        /// Creates a new content filter that won't load chunk if not one of the given types.
        /// </summary>
        /// <param name="types">The accepted types.</param>
        /// <returns></returns>
        public static ContentFilterDelegate NewContentFilterByType(params Type[] types)
        {
            // We could convert to HashSet, but usually not worth it for small sets
            return (ILoadableReference contentReference, ref bool shouldBeLoaded) =>
            {
                if (!types.Contains(contentReference.Type))
                    shouldBeLoaded = false;
            };
        }
    }
}
