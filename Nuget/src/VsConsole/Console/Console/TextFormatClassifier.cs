using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace NuGetConsole.Implementation.Console
{
    public interface ITextFormatClassifier
    {
        IClassificationType GetClassificationType(Color? foreground, Color? background);
    }

    public interface ITextFormatClassifierProvider
    {
        ITextFormatClassifier GetTextFormatClassifier(ITextView textView);
    }

    [Export(typeof(ITextFormatClassifierProvider))]
    class TextFormatClassifierProvider : ITextFormatClassifierProvider
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [Import]
        internal IStandardClassificationService StandardClassificationService { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistryService { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [Import]
        internal IClassificationFormatMapService ClassificationFormatMapService { get; set; }

        public ITextFormatClassifier GetTextFormatClassifier(ITextView textView)
        {
            UtilityMethods.ThrowIfArgumentNull(textView);
            return textView.Properties.GetOrCreateSingletonProperty<TextFormatClassifier>(
                () => new TextFormatClassifier(this, textView));
        }
    }

    class TextFormatClassifier : ObjectWithFactory<TextFormatClassifierProvider>, ITextFormatClassifier
    {
        ITextView _textView;

        Dictionary<Tuple<Color?, Color?>, IClassificationType> _classificationMap =
            new Dictionary<Tuple<Color?, Color?>, IClassificationType>();

        public TextFormatClassifier(TextFormatClassifierProvider factory, ITextView textView)
            : base(factory)
        {
            UtilityMethods.ThrowIfArgumentNull(textView);
            _textView = textView;
        }

        static string GetClassificationName(Color? foreground, Color? background)
        {
            StringBuilder sb = new StringBuilder(32);

            if (foreground != null)
            {
                sb.Append(foreground.Value);
            }
            sb.Append('-'); // Need this to distinguish foreground only with background only
            if (background != null)
            {
                sb.Append(background.Value);
            }
            return sb.ToString();
        }

        static TextFormattingRunProperties GetFormat(Color? foreground, Color? background)
        {
            TextFormattingRunProperties fmt = TextFormattingRunProperties.CreateTextFormattingRunProperties();

            if (foreground != null)
            {
                fmt = fmt.SetForeground(foreground.Value);
            }
            if (background != null)
            {
                fmt = fmt.SetBackground(background.Value);
            }
            return fmt;
        }

        public IClassificationType GetClassificationType(Color? foreground, Color? background)
        {
            var key = Tuple.Create(foreground, background);
            IClassificationType classificationType;
            if (!_classificationMap.TryGetValue(key, out classificationType))
            {
                string classificationName = GetClassificationName(foreground, background);
                classificationType = Factory.ClassificationTypeRegistryService.GetClassificationType(classificationName);
                if (classificationType == null)
                {
                    classificationType = Factory.ClassificationTypeRegistryService.CreateClassificationType(
                        classificationName, new IClassificationType[] { Factory.StandardClassificationService.NaturalLanguage });
                }
                _classificationMap.Add(key, classificationType);

                IClassificationFormatMap formatMap = Factory.ClassificationFormatMapService.GetClassificationFormatMap(_textView);
                formatMap.SetTextProperties(classificationType, GetFormat(foreground, background));
            }
            return classificationType;
        }
    }
}
