// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.MicroThreading;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Core.Storage;

namespace SiliconStudio.Core.Serialization.Contents
{
    /// <summary>
    /// Loads and saves assets.
    /// </summary>
    public sealed partial class ContentManager : IContentManager
    {
        private static readonly Logger Log = GlobalLogger.GetLogger("ContentManager");

        public static DatabaseFileProvider FileProvider => GetFileProvider?.Invoke();

        public static Func<DatabaseFileProvider> GetFileProvider { get; set; }

        public ContentSerializer Serializer { get; private set; }

        /// <summary>
        /// A dictionary mapping, for each loaded object, its url to the corresponding instance of <see cref="Reference"/>.
        /// </summary>
        internal readonly Dictionary<string, Reference> LoadedAssetUrls = new Dictionary<string, Reference>();

        /// <summary>
        /// A dictionary mapping, for each loaded object, the corresponding instance of <see cref="Reference"/>.
        /// </summary>
        internal readonly Dictionary<object, Reference> LoadedAssetReferences = new Dictionary<object, Reference>();

        public ContentManager() : this(null)
        {
        }

        public ContentManager(IServiceRegistry services)
        {
            Serializer = new ContentSerializer();
            if (services != null)
            {
                Serializer.SerializerContextTags.Set(ServiceRegistry.ServiceRegistryKey, services);
            }
        }

        /// <summary>
        /// Saves an asset at a specific URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="asset">The asset.</param>
        /// <exception cref="System.ArgumentNullException">
        /// url
        /// or
        /// asset
        /// </exception>
        public void Save(string url, object asset)
        {
            if (url == null) throw new ArgumentNullException("url");
            if (asset == null) throw new ArgumentNullException("asset");

            lock (LoadedAssetUrls)
            {
                using (var profile = Profiler.Begin(ContentProfilingKeys.ContentSave))
                {
                    SerializeObject(url, asset, true);
                }
            }
        }

        /// <summary>
        /// Check if the specified asset exists.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>
        ///   <c>true</c> if the specified asset url exists, <c>false</c> otherwise.
        /// </returns>
        public bool Exists(string url)
        {
            ObjectId objectId;
            return FileProvider.ContentIndexMap.TryGetValue(url, out objectId);
        }

        public Stream OpenAsStream(string url, StreamFlags streamFlags)
        {
            return FileProvider.OpenStream(url, VirtualFileMode.Open, VirtualFileAccess.Read, streamFlags:streamFlags);
        }

        /// <summary>
        /// Loads an asset from the specified URL.
        /// </summary>
        /// <typeparam name="T">The content type.</typeparam>
        /// <param name="url">The URL to load from.</param>
        /// <param name="settings">The settings. If null, fallback to <see cref="ContentManagerLoaderSettings.Default" />.</param>
        /// <remarks>If the asset is already loaded, it just increases the reference count of the asset and return the same instance.</remarks>
        /// <returns></returns>
        public T Load<T>(string url, ContentManagerLoaderSettings settings = null) where T : class
        {
            return (T)Load(typeof(T), url, settings);
        }

        /// <summary>
        /// Loads an asset from the specified URL.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="url">The URL.</param>
        /// <param name="settings">The settings.</param>
        /// <returns></returns>
        /// <remarks>If the asset is already loaded, it just increases the reference count of the asset and return the same instance.</remarks>
        /// <exception cref="System.ArgumentNullException">url</exception>
        public object Load(Type type, string url, ContentManagerLoaderSettings settings = null)
        {
            if (settings == null)
                settings = ContentManagerLoaderSettings.Default;

            if (url == null) throw new ArgumentNullException("url");

            lock (LoadedAssetUrls)
            {
                using (var profile = Profiler.Begin(ContentProfilingKeys.ContentLoad, url))
                {
                    return DeserializeObject(url, type, null, settings);
                }
            }
        }

        /// <summary>
        /// Reloads an asset. If possible, same recursively referenced objects are reused.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>True if it could be reloaded, false otherwise.</returns>
        /// <exception cref="System.InvalidOperationException">Content not loaded through this ContentManager.</exception>
        public bool Reload(object obj, ContentManagerLoaderSettings settings = null)
        {
            if (settings == null)
                settings = ContentManagerLoaderSettings.Default;

            lock (LoadedAssetUrls)
            {
                Reference reference;
                if (!LoadedAssetReferences.TryGetValue(obj, out reference))
                    return false;

                var url = reference.Url;

                using (var profile = Profiler.Begin(ContentProfilingKeys.ContentReload, url))
                {
                    DeserializeObject(url, obj.GetType(), obj, settings);
                }

                return true;
            }
        }

        /// <summary>
        /// Reloads an asset asynchronously. If possible, same recursively referenced objects are reused.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>True if it could be reloaded, false otherwise.</returns>
        /// <exception cref="System.InvalidOperationException">Content not loaded through this ContentManager.</exception>
        public Task<bool> ReloadAsync(object obj, ContentManagerLoaderSettings settings = null)
        {
            return ScheduleAsync(() => Reload(obj, settings));
        }

        /// <summary>
        /// Loads an asset from the specified URL asynchronously.
        /// </summary>
        /// <typeparam name="T">The content type.</typeparam>
        /// <param name="url">The URL to load from.</param>
        /// <param name="settings">The settings. If null, fallback to <see cref="ContentManagerLoaderSettings.Default" />.</param>
        /// <remarks>If the asset is already loaded, it just increases the reference count of the asset and return the same instance.</remarks>
        /// <returns></returns>
        public Task<T> LoadAsync<T>(string url, ContentManagerLoaderSettings settings = null) where T : class
        {
            return ScheduleAsync(() => Load<T>(url, settings));
        }

        /// <summary>
        /// Loads an asset from the specified URL asynchronously.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="url">The URL.</param>
        /// <param name="settings">The settings.</param>
        /// <remarks>If the asset is already loaded, it just increases the reference count of the asset and return the same instance.</remarks>
        /// <returns></returns>
        public Task<object> LoadAsync(Type type, string url, ContentManagerLoaderSettings settings = null)
        {
            return ScheduleAsync(() => Load(type, url, settings));
        }

        private static Task<T> ScheduleAsync<T>(Func<T> action)
        {
            var microThread = Scheduler.CurrentMicroThread;
            return Task.Factory.StartNew(() =>
            {
                var initialContext = SynchronizationContext.Current;
                // This synchronization context gives access to any MicroThreadLocal values. The database to use might actually be micro thread local.
                SynchronizationContext.SetSynchronizationContext(new MicrothreadProxySynchronizationContext(microThread));
                var result = action();
                SynchronizationContext.SetSynchronizationContext(initialContext);
                return result;
            });
        }

        /// <summary>
        /// Gets a previously loaded asset from its URL.
        /// </summary>
        /// <typeparam name="T">The type of asset to retrieve.</typeparam>
        /// <param name="url">The URL of the asset to retrieve.</param>
        /// <returns>The loaded asset, or <c>null</c> if the asset has not been loaded.</returns>
        /// <remarks>This function does not increase the reference count on the asset.</remarks>
        public T Get<T>(string url)
        {
            return (T)Get(typeof(T), url);
        }

        /// <summary>
        /// Gets a previously loaded asset from its URL.
        /// </summary>
        /// <param name="type">The type of asset to retrieve.</param>
        /// <param name="url">The URL of the asset to retrieve.</param>
        /// <returns>The loaded asset, or <c>null</c> if the asset has not been loaded.</returns>
        /// <remarks>This function does not increase the reference count on the asset.</remarks>
        public object Get(Type type, string url)
        {
            var reference = FindDeserializedObject(url, type);
            return reference != null ? reference.Object : null;
        }

        /// <summary>
        /// Gets or sets whether an asset with the given URL is currently loaded.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <param name="loadedManuallyOnly">If <c>true</c>, this method will return true only if an asset with the given URL has been manually loaded via <see cref="Load"/>, and not if the asset has been only loaded indirectly from another asset.</param>
        /// <returns><c>True</c> if an asset with the given URL is currently loaded, <c>false</c> otherwise.</returns>
        public bool IsLoaded(string url, bool loadedManuallyOnly = false)
        {
            Reference reference;
            return LoadedAssetUrls.TryGetValue(url, out reference) && (!loadedManuallyOnly || reference.PublicReferenceCount > 0);
        }

        public bool TryGetAssetUrl(object obj, out string url)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            lock (LoadedAssetUrls)
            {
                Reference reference;
                if (!LoadedAssetReferences.TryGetValue(obj, out reference))
                {
                    url = null;
                    return false;
                }

                url = reference.Url;
                return true;
            }
        }

        /// <summary>
        /// Unloads the specified asset.
        /// </summary>
        /// <param name="obj">The object to unload.</param>
        /// <exception cref="System.InvalidOperationException">Content not loaded through this ContentManager.</exception>
        public void Unload(object obj)
        {
            lock (LoadedAssetUrls)
            {
                Reference reference;
                if (!LoadedAssetReferences.TryGetValue(obj, out reference))
                    throw new InvalidOperationException("Content not loaded through this ContentManager.");

                // Release reference
                DecrementReference(reference, true);
            }
        }

        /// <summary>
        /// Unloads the asset at the specified URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <exception cref="System.InvalidOperationException">Content not loaded through this ContentManager.</exception>
        public void Unload(string url)
        {
            lock (LoadedAssetUrls)
            {
                // Try to find already loaded object
                Reference reference;
                if (!LoadedAssetUrls.TryGetValue(url, out reference))
                    throw new InvalidOperationException("Content not loaded through this ContentManager.");
                
                // Release reference
                DecrementReference(reference, true);
            }
        }

        /// <summary>
        /// Computes statistics about the assets that are currently loaded. This method is intended to be used for debug purpose only.
        /// </summary>
        /// <returns></returns>
        public ContentManagerStats GetStats()
        {
            return new ContentManagerStats(LoadedAssetUrls.Values);
        }

        private void PrepareSerializerContext(ContentSerializerContext contentSerializerContext, SerializerContext context)
        {
            context.Set(ContentSerializerContext.ContentSerializerContextProperty, contentSerializerContext);

            // Duplicate context from SerializerContextTags
            foreach (var property in Serializer.SerializerContextTags)
            {
                context.Tags.SetObject(property.Key, property.Value);
            }
        }

        struct DeserializeOperation
        {
            public readonly Reference ParentReference;
            public readonly string Url;
            public readonly Type ObjectType;
            public readonly Object Object;

            public DeserializeOperation(Reference parentReference, string url, Type objectType, object obj)
            {
                ParentReference = parentReference;
                Url = url;
                ObjectType = objectType;
                Object = obj;
            }
        }

        private object DeserializeObject(string url, Type type, object obj, ContentManagerLoaderSettings settings)
        {
            var serializeOperations = new Queue<DeserializeOperation>();
            serializeOperations.Enqueue(new DeserializeOperation(null, url, type, obj));

            Reference reference = null;
            if (obj != null)
            {
                reference = FindDeserializedObject(url, type);
                if (reference.Object != obj)
                {
                    throw new InvalidOperationException("Object doesn't match, can't reload");
                }
            }

            // Let's put aside old references, so that we unload them only afterwise (avoid a referenced object to be unloaded for no reason)
            HashSet<Reference> references = null;
            if (reference != null)
            {
                // Let's collect dependent reference, and reset current list
                references = reference.References;
                reference.References = new HashSet<Reference>();

                // Mark object as not deserialized yet
                reference.Deserialized = false;
            }

            bool isFirstOperation = true;
            object result = null;

            while (serializeOperations.Count > 0)
            {
                var serializeOperation = serializeOperations.Dequeue();
                var deserializedObject = DeserializeObject(serializeOperations, serializeOperation.ParentReference, serializeOperation.Url, serializeOperation.ObjectType, serializeOperation.Object, settings);
                if (isFirstOperation)
                {
                    result = deserializedObject;
                    isFirstOperation = false;
                }
            }

            if (reference != null)
            {
                foreach (var dependentReference in references)
                {
                    DecrementReference(dependentReference, false);
                }
            }

            return result;
        }

        internal Reference FindDeserializedObject(string url, Type objType)
        {
            // Try to find already loaded object
            Reference reference;
            if (LoadedAssetUrls.TryGetValue(url, out reference))
            {
                while (reference != null && !objType.GetTypeInfo().IsAssignableFrom(reference.Object.GetType().GetTypeInfo()))
                {
                    reference = reference.Next;
                }

                if (reference != null)
                {
                    // TODO: Currently ReferenceSerializer creates a reference, so we will go through DeserializeObject later to add the reference
                    // This should be unified at some point
                    return reference;
                }
            }

            return null;
        }

        internal void RegisterDeserializedObject<T>(string url, T obj)
        {
            var assetReference = new Reference(url, false);
            SetAssetObject(assetReference, obj);
        }

        internal ChunkHeader ReadChunkHeader(string url)
        {
            if (!FileProvider.FileExists(url))
            {
                HandleAssetNotFound(url);
                return null;
            }

            using (var stream = FileProvider.OpenStream(url, VirtualFileMode.Open, VirtualFileAccess.Read))
            {
                // File does not exist
                // TODO/Benlitz: Add a log entry for that, it's not expected to happen
                if (stream == null)
                    return null;

                // Read header
                var streamReader = new BinarySerializationReader(stream);
                return ChunkHeader.Read(streamReader);
            }
        }

        private object DeserializeObject(Queue<DeserializeOperation> serializeOperations, Reference parentReference, string url, Type objType, object obj, ContentManagerLoaderSettings settings)
        {
            // Try to find already loaded object
            Reference reference = FindDeserializedObject(url, objType);
            if (reference != null && reference.Deserialized)
            {
                // Add reference
                bool isRoot = parentReference == null;
                if (isRoot || parentReference.References.Add(reference))
                {
                    IncrementReference(reference, isRoot);
                }

                return reference.Object;
            }

            if (!FileProvider.FileExists(url))
            {
                HandleAssetNotFound(url);
                return null;
            }

            ContentSerializerContext contentSerializerContext;
            object result;

            // Open asset binary stream
            try
            {
                using (var stream = FileProvider.OpenStream(url, VirtualFileMode.Open, VirtualFileAccess.Read))
                {
                    // File does not exist
                    // TODO/Benlitz: Add a log entry for that, it's not expected to happen
                    if (stream == null)
                        return null;

                    Type headerObjType = null;

                    // Read header
                    var streamReader = new BinarySerializationReader(stream);
                    var chunkHeader = ChunkHeader.Read(streamReader);
                    if (chunkHeader != null)
                    {
                        headerObjType = AssemblyRegistry.GetType(chunkHeader.Type);
                    }

                    // Find serializer
                    var serializer = Serializer.GetSerializer(headerObjType, objType);
                    if (serializer == null)
                        throw new InvalidOperationException(string.Format("Content serializer for {0}/{1} could not be found.", headerObjType, objType));
                    contentSerializerContext = new ContentSerializerContext(url, ArchiveMode.Deserialize, this) { LoadContentReferences = settings.LoadContentReferences };

                    // Read chunk references
                    if (chunkHeader != null && chunkHeader.OffsetToReferences != -1)
                    {
                        // Seek to where references are stored and deserialize them
                        streamReader.NativeStream.Seek(chunkHeader.OffsetToReferences, SeekOrigin.Begin);
                        contentSerializerContext.SerializeReferences(streamReader);
                        streamReader.NativeStream.Seek(chunkHeader.OffsetToObject, SeekOrigin.Begin);
                    }

                    if (reference == null)
                    {
                        // Create Reference
                        reference = new Reference(url, parentReference == null);
                        result = obj ?? serializer.Construct(contentSerializerContext);
                        SetAssetObject(reference, result);
                    }
                    else
                    {
                        result = reference.Object;
                    }

                    reference.Deserialized = true;

                    PrepareSerializerContext(contentSerializerContext, streamReader.Context);

                    contentSerializerContext.SerializeContent(streamReader, serializer, result);

                    // Add reference
                    if (parentReference != null)
                    {
                        parentReference.References.Add(reference);
                    }
                }
            }
            catch (Exception exception)
            {
                throw new ContentManagerException(string.Format("Unexpected exception while loading asset [{0}]. Reason: {1}. Check inner-exception for details.", url, exception.Message), exception);
            }

            if (settings.LoadContentReferences)
            {
                // Process content references
                // TODO: Should we work at ChunkReference level?
                foreach (var contentReference in contentSerializerContext.ContentReferences)
                {
                    bool shouldBeLoaded = true;

                    //Reference childReference;

                    settings.ContentFilter?.Invoke(contentReference, ref shouldBeLoaded);

                    if (shouldBeLoaded)
                    {
                        serializeOperations.Enqueue(new DeserializeOperation(reference, contentReference.Location, contentReference.Type, contentReference.ObjectValue));
                    }
                }
            }

            return result;
        }

        struct SerializeOperation
        {
            public readonly string Url;
            public readonly object Object;
            public readonly bool PublicReference;

            public SerializeOperation(string url, object obj, bool publicReference)
            {
                Url = url;
                Object = obj;
                PublicReference = publicReference;
            }
        }

        private void SerializeObject(string url, object obj, bool publicReference)
        {
            var serializeOperations = new Queue<SerializeOperation>();
            serializeOperations.Enqueue(new SerializeOperation(url, obj, publicReference));

            while (serializeOperations.Count > 0)
            {
                var serializeOperation = serializeOperations.Dequeue();
                SerializeObject(serializeOperations, serializeOperation.Url, serializeOperation.Object, serializeOperation.PublicReference);
            }
        }

        private void SerializeObject(Queue<SerializeOperation> serializeOperations, string url, object obj, bool publicReference)
        {
            // Don't create context in case we don't want to serialize referenced objects
            //if (!SerializeReferencedObjects && obj != RootObject)
            //    return null;

            // Already saved?
            // TODO: Ref counting? Should we change it on save? Probably depends if we cache or not.
            if (LoadedAssetReferences.ContainsKey(obj))
                return;

            var serializer = Serializer.GetSerializer(null, obj.GetType());
            if (serializer == null)
                throw new InvalidOperationException(string.Format("Content serializer for {0} could not be found.", obj.GetType()));

            var contentSerializerContext = new ContentSerializerContext(url, ArchiveMode.Serialize, this);

            using (var stream = FileProvider.OpenStream(url, VirtualFileMode.Create, VirtualFileAccess.Write))
            {
                var streamWriter = new BinarySerializationWriter(stream);
                PrepareSerializerContext(contentSerializerContext, streamWriter.Context);

                ChunkHeader header = null;

                // Allocate space in the stream, and also include header version in the hash computation, which is better
                // If serialization type is null, it means there should be no header.
                var serializationType = serializer.SerializationType;
                if (serializationType != null)
                {
                    header = new ChunkHeader();
                    header.Type = serializer.SerializationType.AssemblyQualifiedName;
                    header.Write(streamWriter);
                    header.OffsetToObject = (int)streamWriter.NativeStream.Position;
                }

                contentSerializerContext.SerializeContent(streamWriter, serializer, obj);

                // Write references and updated header
                if (header != null)
                {
                    header.OffsetToReferences = (int)streamWriter.NativeStream.Position;
                    contentSerializerContext.SerializeReferences(streamWriter);

                    // Move back to the pre-allocated header position in the steam
                    stream.Seek(0, SeekOrigin.Begin);

                    // Write actual header.
                    header.Write(new BinarySerializationWriter(stream));
                }
            }

            var assetReference = new Reference(url, publicReference);
            SetAssetObject(assetReference, obj);

            // Process content references
            // TODO: Should we work at ChunkReference level?
            foreach (var contentReference in contentSerializerContext.ContentReferences)
            {
                if (contentReference.ObjectValue != null)
                {
                    var attachedReference = AttachedReferenceManager.GetAttachedReference(contentReference.ObjectValue);
                    if (attachedReference == null || attachedReference.IsProxy)
                        continue;

                    serializeOperations.Enqueue(new SerializeOperation(contentReference.Location, contentReference.ObjectValue, false));
                }
            }
        }

        /// <summary>
        /// Sets Reference.Object, and updates loadedAssetByUrl collection.
        /// </summary>
        internal void SetAssetObject(Reference reference, object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");

            if (reference.Object != null)
            {
                if (reference.Object != obj)
                    throw new InvalidOperationException("SetAssetObject has already been called with a different object");

                return;
            }

            var url = reference.Url;
            reference.Object = obj;

            lock (LoadedAssetUrls)
            {
                Reference previousReference;

                if (LoadedAssetUrls.TryGetValue(url, out previousReference))
                {
                    reference.Next = previousReference.Next;
                    reference.Prev = previousReference;

                    if (previousReference.Next != null)
                        previousReference.Next.Prev = reference;
                    previousReference.Next = reference;
                }
                else
                {
                    LoadedAssetUrls[url] = reference;
                }

                LoadedAssetReferences[obj] = reference;

                // TODO: Currently here so that reference.ObjectValue later keeps its Url.
                // Need some reorganization?
                AttachedReferenceManager.SetUrl(obj, reference.Url);
            }
        }

        /// <summary>
        /// Notify debugger and logging when an asset could not be found.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <exception cref="ContentManagerException"></exception>
        // TODO: Replug this when an asset is not found?
        private static void HandleAssetNotFound(string url)
        {
            var errorMessage = $"The asset '{url}' could not be found. Asset path should be 'MyFolder/MyAssetName'. Check that the path is correct and that the asset has been included into the build.";

            // If a debugger is attached, throw an exception (we do that instead of Debugger.Break so that user can easily ignore this specific type of exception)
            if (Debugger.IsAttached)
            {
                try
                {
                    throw new ContentManagerException(errorMessage);
                }
                catch (Exception)
                {
                }
            }

            // Log error
            Log.Error(errorMessage);
        }
    }
}
