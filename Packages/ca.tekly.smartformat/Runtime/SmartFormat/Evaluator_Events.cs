//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.
//

using System;
using SmartFormat.Core.Extensions;
using SmartFormat.Core.Parsing;

namespace SmartFormat
{
    internal partial class Evaluator
    {
        /// <summary>
        /// Event is raising, when an error occurs during evaluation of values or formats.
        /// </summary>
        public event EventHandler<FormattingErrorEventArgs> OnFormattingFailure;

        /// <summary>
        /// Event raised when a <see cref="Format"/> is encountered.
        /// </summary>
        internal EventHandler<FormatEventArgs> OnFormat;

        /// <summary>
        /// Event raised when a <see cref="LiteralText"/>> is encountered.
        /// </summary>
        internal EventHandler<LiteralEventArgs> OnLiteral;

        /// <summary>
        /// Event raised when a <see cref="Placeholder"/> is encountered.
        /// </summary>
        internal EventHandler<PlaceholderEventArgs> OnPlaceholder;

        /// <summary>
        /// Event raised when a <see cref="Selector"/> is evaluated.
        /// </summary>
        internal EventHandler<SelectorValueEventArgs> OnSelectorValue;

        /// <summary>
        /// Event raised when a <see cref="Selector"/> fails to evaluate.
        /// </summary>
        internal EventHandler<SelectorValueEventArgs> OnSelectorFailure;

        /// <summary>
        /// Event raised when formatting starts.
        /// </summary>
        internal EventHandler<FormattingEventArgs> OnFormattingStart;

        /// <summary>
        /// Event raised when output was written by a <see cref="IFormattingInfo"/> instance.
        /// </summary>
        internal EventHandler<OutputWrittenEventArgs> OnOutputWritten;

        /// <summary>
        /// Event raised when formatting ends.
        /// </summary>
        internal EventHandler<FormattingEventArgs> OnFormattingEnd;

        /// <summary>
        /// Arguments for the <see cref="OnFormat"/> event.
        /// </summary>
        internal readonly struct FormatEventArgs
        {
            /// <summary>
            /// Arguments for the <see cref="OnFormat"/> event.
            /// </summary>
            public FormatEventArgs(Format Format)
            {
                this.Format = Format;
            }
            public Format Format { get; }
            public void Deconstruct(out Format Format)
            {
                Format = this.Format;
            }
        }

        internal readonly struct OutputWrittenEventArgs
        {
            public OutputWrittenEventArgs(string WrittenValue)
            {
                this.WrittenValue = WrittenValue;
            }
            public string WrittenValue { get; }
            public void Deconstruct(out string WrittenValue)
            {
                WrittenValue = this.WrittenValue;
            }
        }

        /// <summary>
        /// Arguments for the <see cref="OnLiteral"/> event.
        /// </summary>
        internal readonly struct LiteralEventArgs
        {
            /// <summary>
            /// Arguments for the <see cref="OnLiteral"/> event.
            /// </summary>
            public LiteralEventArgs(string Text)
            {
                this.Text = Text;
            }
            public string Text { get; }
            public void Deconstruct(out string Text)
            {
                Text = this.Text;
            }
        }

        /// <summary>
        /// Arguments for the <see cref="OnPlaceholder"/> event.
        /// </summary>
        internal readonly struct PlaceholderEventArgs
        {
            /// <summary>
            /// Arguments for the <see cref="OnPlaceholder"/> event.
            /// </summary>
            public PlaceholderEventArgs(Placeholder Placeholder)
            {
                this.Placeholder = Placeholder;
            }
            public Placeholder Placeholder { get; }
            public void Deconstruct(out Placeholder Placeholder)
            {
                Placeholder = this.Placeholder;
            }
        }

        /// <summary>
        /// Arguments for the <see cref="OnSelectorValue"/> event.
        /// </summary>
        internal readonly struct SelectorValueEventArgs
        {
            /// <summary>
            /// Arguments for the <see cref="OnSelectorValue"/> event.
            /// </summary>
            public SelectorValueEventArgs(Selector Selector, bool Success, Type SourceType, object Value)
            {
                this.Selector = Selector;
                this.Success = Success;
                this.SourceType = SourceType;
                this.Value = Value;
            }
            public Selector Selector { get; }
            public bool Success { get; }
            public Type SourceType { get; }
            public object Value { get; }
            public void Deconstruct(out Selector Selector, out bool Success, out Type SourceType, out object Value)
            {
                Selector = this.Selector;
                Success = this.Success;
                SourceType = this.SourceType;
                Value = this.Value;
            }
        }

        /// <summary>
        /// Arguments for the <see cref="OnFormattingStart"/> and <see cref="OnFormattingEnd"/> events.
        /// </summary>
        internal readonly struct FormattingEventArgs
        {
            /// <summary>
            /// Arguments for the <see cref="OnFormattingStart"/> and <see cref="OnFormattingEnd"/> events.
            /// </summary>
            public FormattingEventArgs(Selector Selector, object Value, bool Success, Type FormatterType)
            {
                this.Selector = Selector;
                this.Value = Value;
                this.Success = Success;
                this.FormatterType = FormatterType;
            }
            public Selector Selector { get; }
            public object Value { get; }
            public bool Success { get; }
            public Type FormatterType { get; }
            public void Deconstruct(out Selector Selector, out object Value, out bool Success, out Type FormatterType)
            {
                Selector = this.Selector;
                Value = this.Value;
                Success = this.Success;
                FormatterType = this.FormatterType;
            }
        }
    }
}
