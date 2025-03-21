// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Performance.SDK.Processing;

namespace Microsoft.Performance.SDK.Runtime
{
    /// <summary>
    ///     Represents a session data source. A session data source
    ///     is an <see cref="IProcessingSource"/> and the associated
    ///     <see cref="IDataSource"/>s that can be processed by the
    ///     <see cref="IProcessingSource"/>.
    /// </summary>
    public sealed class DataSessionSource
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DataSessionSource"/>
        ///     class.
        /// </summary>
        public DataSessionSource(
            ProcessingSourceReference processingSource,
            IEnumerable<IDataSource> dataSources,
            ProcessorOptions options)
        {
            Guard.NotNull(processingSource, nameof(processingSource));
            Guard.NotNull(dataSources, nameof(dataSources));
            Guard.NotNull(options, nameof(options));

            this.ProcessingSource = processingSource;
            this.DataSources = dataSources.ToList().AsReadOnly();
            this.Options = options;

            this.ProgressTracker = new DataProcessorProgress();
        }

        /// <summary>
        ///     Gets the <see cref="IProcessingSource"/> associated
        ///     with the given data items.
        /// </summary>
        public ProcessingSourceReference ProcessingSource { get; }

        /// <summary>
        ///     Gets the <see cref="IDataSource"/>s that can be
        ///     processed by the <see cref="ProcessingSource"/>.
        /// </summary>
        public IEnumerable<IDataSource> DataSources { get; }

        /// <summary>
        ///     Gets the options to pass to the processors.
        /// </summary>
        public ProcessorOptions Options { get; private set; }

        /// <summary>
        ///     Gets the progress tracker for this instance.
        /// </summary>
        public DataProcessorProgress ProgressTracker { get; }

        /// <summary>
        ///     Adds an option to this instance.
        /// </summary>
        /// <param name="newOption">
        ///     The new option.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="newOption"/> is <c>null</c>.
        /// </exception>
        public void AddOption(OptionInstance newOption)
        {
            if (this.Options is null)
            {
                this.Options = new ProcessorOptions(newOption.AsEnumerableSingleton());
            }
            else
            {
                this.Options = new ProcessorOptions(
                    this.Options.Options.Concat(newOption),
                    this.Options.Arguments);
            }
        }

        /// <summary>
        ///     Gets the <see cref="string"/> representation of this
        ///     instance.
        /// </summary>
        /// <returns>
        ///     The <see cref="string"/> representation of this instance.
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            var dataSourceName = this.ProcessingSource.Name;
            sb.Append(dataSourceName);

            sb.Append(" (");
            var isFirst = true;
            foreach (var source in this.DataSources)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    sb.Append(", ");
                }

                sb.Append(source.Uri);
            }

            sb.Append(")");

            return sb.ToString();
        }
    }
}
