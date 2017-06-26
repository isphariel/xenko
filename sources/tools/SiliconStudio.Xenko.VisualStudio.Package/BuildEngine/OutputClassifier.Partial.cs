// Copyright (c) 2011-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;

using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

using SiliconStudio.Xenko.VisualStudio.BuildEngine;

namespace SiliconStudio.Xenko.VisualStudio
{
    public partial class OutputClassifier
    {
        private Dictionary<string, string> classificationTypes = new Dictionary<string, string>();

        private void InitializeClassifiers()
        {
            classificationTypes.Add("Debug", AssetCompilerDebug);
            classificationTypes.Add("Verbose", AssetCompilerVerbose);
            classificationTypes.Add("Info", AssetCompilerInfo);
            classificationTypes.Add("Warning", AssetCompilerWarning);
            classificationTypes.Add("Error", AssetCompilerError);
            classificationTypes.Add("Fatal", AssetCompilerFatal);
        }

        public const string AssetCompilerDebug = "Xenko.AssetCompiler.Debug";
        public const string AssetCompilerVerbose = "Xenko.AssetCompiler.Verbose";
        public const string AssetCompilerInfo = "Xenko.AssetCompiler.Info";
        public const string AssetCompilerWarning = "Xenko.AssetCompiler.Warning";
        public const string AssetCompilerError = "Xenko.AssetCompiler.Error";
        public const string AssetCompilerFatal = "Xenko.AssetCompiler.Fatal";

        [Export]
        [Name(AssetCompilerDebug)]
        internal static ClassificationTypeDefinition assetCompilerDebug = null;

        [Export]
        [Name(AssetCompilerVerbose)]
        internal static ClassificationTypeDefinition assetCompilerVerbose = null;

        [Export]
        [Name(AssetCompilerInfo)]
        internal static ClassificationTypeDefinition assetCompilerInfo = null;

        [Export]
        [Name(AssetCompilerWarning)]
        internal static ClassificationTypeDefinition assetCompilerWarning = null;

        [Export]
        [Name(AssetCompilerError)]
        internal static ClassificationTypeDefinition assetCompilerError = null;

        [Export]
        [Name(AssetCompilerFatal)]
        internal static ClassificationTypeDefinition assetCompilerFatal = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = AssetCompilerDebug)]
        [Name(AssetCompilerDebug)]
        [UserVisible(true)] //this should be visible to the end user
        [Order(Before = Priority.Default)] //set the priority to be after the default classifiers
        internal sealed class AssetCompilerDebugFormat : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public AssetCompilerDebugFormat(OutputClassificationColorManager colorManager)
            {
                DisplayName = "Xenko AssetCompiler Debug";
                this.IsBold = false;
                var classificationColor = colorManager.GetClassificationColor(AssetCompilerDebug);
                ForegroundColor = classificationColor.ForegroundColor;
                BackgroundColor = classificationColor.BackgroundColor;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = AssetCompilerVerbose)]
        [Name(AssetCompilerVerbose)]
        [UserVisible(true)] //this should be visible to the end user
        [Order(Before = Priority.Default)] //set the priority to be after the default classifiers
        internal sealed class AssetCompilerVerboseFormat : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public AssetCompilerVerboseFormat(OutputClassificationColorManager colorManager)
            {
                DisplayName = "Xenko AssetCompiler Verbose";
                this.IsBold = false;
                var classificationColor = colorManager.GetClassificationColor(AssetCompilerVerbose);
                ForegroundColor = classificationColor.ForegroundColor;
                BackgroundColor = classificationColor.BackgroundColor;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = AssetCompilerInfo)]
        [Name(AssetCompilerInfo)]
        [UserVisible(true)] //this should be visible to the end user
        [Order(Before = Priority.Default)] //set the priority to be after the default classifiers
        internal sealed class AssetCompilerInfoFormat : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public AssetCompilerInfoFormat(OutputClassificationColorManager colorManager)
            {
                DisplayName = "Xenko AssetCompiler Info";
                this.IsBold = false;
                var classificationColor = colorManager.GetClassificationColor(AssetCompilerInfo);
                ForegroundColor = classificationColor.ForegroundColor;
                BackgroundColor = classificationColor.BackgroundColor;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = AssetCompilerWarning)]
        [Name(AssetCompilerWarning)]
        [UserVisible(true)] //this should be visible to the end user
        [Order(Before = Priority.Default)] //set the priority to be after the default classifiers
        internal sealed class AssetCompilerWarningFormat : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public AssetCompilerWarningFormat(OutputClassificationColorManager colorManager)
            {
                DisplayName = "Xenko AssetCompiler Warning";
                this.IsBold = false;
                var classificationColor = colorManager.GetClassificationColor(AssetCompilerWarning);
                ForegroundColor = classificationColor.ForegroundColor;
                BackgroundColor = classificationColor.BackgroundColor;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = AssetCompilerError)]
        [Name(AssetCompilerError)]
        [UserVisible(true)] //this should be visible to the end user
        [Order(Before = Priority.Default)] //set the priority to be after the default classifiers
        internal sealed class AssetCompilerErrorFormat : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public AssetCompilerErrorFormat(OutputClassificationColorManager colorManager)
            {
                DisplayName = "Xenko AssetCompiler Error";
                this.IsBold = true;
                var classificationColor = colorManager.GetClassificationColor(AssetCompilerError);
                ForegroundColor = classificationColor.ForegroundColor;
                BackgroundColor = classificationColor.BackgroundColor;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = AssetCompilerFatal)]
        [Name(AssetCompilerFatal)]
        [UserVisible(true)] //this should be visible to the end user
        [Order(Before = Priority.Default)] //set the priority to be after the default classifiers
        internal sealed class AssetCompilerFatalFormat : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            public AssetCompilerFatalFormat(OutputClassificationColorManager colorManager)
            {
                DisplayName = "Xenko AssetCompiler Fatal";
                this.IsBold = true;
                var classificationColor = colorManager.GetClassificationColor(AssetCompilerFatal);
                ForegroundColor = classificationColor.ForegroundColor;
                BackgroundColor = classificationColor.BackgroundColor;
            }
        }

    }
}
