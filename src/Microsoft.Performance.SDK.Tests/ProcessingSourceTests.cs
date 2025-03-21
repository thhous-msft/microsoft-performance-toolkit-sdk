// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Testing;
using Microsoft.Performance.Testing.SDK;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Performance.SDK.Tests
{
    [TestClass]
    public class ProcessingSourceTests
    {
        private ITableConfigurationsSerializer serializer = new FakeSerializer();
        private IApplicationEnvironment applicationEnvironment = new StubApplicationEnvironment() { Serializer = new FakeSerializer() };

        [TestCleanup]
        public void Cleanup()
        {
            StubDataSource.Assembly = typeof(StubDataSource).Assembly;
        }

        [TestMethod]
        [UnitTest]
        public void AllTablesIsUnionOfDataAndMetadata()
        {
            var assembly = new FakeAssembly
            {
                TypesToReturn = new[]
                {
                    typeof(DateTime),
                    typeof(StubDataTableOne),
                    typeof(StubDataTableTwo),
                    typeof(StubDataTableThree),
                    typeof(StubMetadataTableOne),
                    typeof(StubMetadataTableTwo),
                    typeof(Exception),
                    typeof(StubMetadataTableNoBuildMethod),
                    typeof(StubDataTableOneNoBuildMethod),
                    typeof(StubDataTableTwoNoBuildMethod),
                }
            };

            var expectedDescriptors = TableDescriptorUtils.CreateTableDescriptors(
                serializer,
                typeof(StubMetadataTableNoBuildMethod),
                typeof(StubDataTableOneNoBuildMethod),
                typeof(StubDataTableTwoNoBuildMethod));

            StubDataSource.Assembly = assembly;

            var sut = new StubDataSource();
            sut.SetApplicationEnvironment(applicationEnvironment);

            Assert.AreEqual(expectedDescriptors.Count, sut.AllTablesExposed.Count);
            foreach (var td in expectedDescriptors)
            {
                Assert.IsTrue(sut.AllTablesExposed.Contains(td));
            }
        }

        [TestMethod]
        [UnitTest]
        public void CreateProcessorWithOneDataSourceCallsSubClass()
        {
            var dataSource = Any.DataSource();
            var env = Any.ProcessorEnvironment();
            var options = ProcessorOptions.Default;

            var sut = new StubDataSource();
            sut.SetApplicationEnvironment(applicationEnvironment);
            var result = sut.CreateProcessor(dataSource, env, options);

            Assert.AreEqual(1, sut.CreateProcessorCoreCalls.Count);
            Assert.AreEqual(1, sut.CreateProcessorCoreCalls[0].Item1.Count());
            Assert.AreEqual(dataSource, sut.CreateProcessorCoreCalls[0].Item1.ElementAt(0));
            Assert.AreEqual(env, sut.CreateProcessorCoreCalls[0].Item2);
            Assert.AreEqual(options, sut.CreateProcessorCoreCalls[0].Item3);
        }

        [TestMethod]
        [UnitTest]
        public void WhenSubClassReturnsNullProcessorThenThrows()
        {
            var dataSource = Any.DataSource();

            var sut = new StubDataSource
            {
                ProcessorToReturn = null,
            };
            sut.SetApplicationEnvironment(applicationEnvironment);

            Assert.ThrowsException<InvalidOperationException>(
                () => sut.CreateProcessor(dataSource, Any.ProcessorEnvironment(), ProcessorOptions.Default));
        }

        [TestMethod]
        [UnitTest]
        public void CreateProcessorWithManyDataSourcesCallsSubClass()
        {
            var dataSource = Any.DataSource();
            var dataSources = new[] { dataSource, };
            var env = Any.ProcessorEnvironment();
            var options = ProcessorOptions.Default;

            var sut = new StubDataSource();
            sut.SetApplicationEnvironment(applicationEnvironment);
            var result = sut.CreateProcessor(dataSources, env, options);

            Assert.AreEqual(1, sut.CreateProcessorCoreCalls.Count);
            Assert.AreEqual(dataSources, sut.CreateProcessorCoreCalls[0].Item1);
            Assert.AreEqual(env, sut.CreateProcessorCoreCalls[0].Item2);
            Assert.AreEqual(options, sut.CreateProcessorCoreCalls[0].Item3);
        }

        [TestMethod]
        [UnitTest]
        public void ManyDataSourcesWhenSubClassReturnsNullProcessorThenThrows()
        {
            var dataSource = Any.DataSource();
            var dataSources = new[] { dataSource, };

            var sut = new StubDataSource
            {
                ProcessorToReturn = null,
            };

            sut.SetApplicationEnvironment(applicationEnvironment);

            Assert.ThrowsException<InvalidOperationException>(
                () => sut.CreateProcessor(dataSources, Any.ProcessorEnvironment(), ProcessorOptions.Default));
        }

        [TestMethod]
        [UnitTest]
        public void WhenTableDiscoveryProvidedUsesDiscovery()
        {
            TableDescriptorUtils.CreateTableDescriptors(
                serializer,
                out var expectedDescriptors,
                out var _,
                typeof(StubDataTableOne),
                typeof(StubDataTableTwo),
                typeof(StubDataTableThree),
                typeof(StubMetadataTableOne),
                typeof(StubMetadataTableTwo));

            var discovery = new FakeTableProvider();
            discovery.DiscoverReturnValue = new HashSet<TableDescriptor>
            {
                expectedDescriptors[0],
                expectedDescriptors[1],
                expectedDescriptors[2],
                expectedDescriptors[3],
                expectedDescriptors[4],
            };

            var sut = new StubDataSource(discovery);
            sut.SetApplicationEnvironment(applicationEnvironment);

            Assert.AreEqual(5, sut.AllTablesExposed.Count);

            foreach (var td in expectedDescriptors)
            {
                Assert.IsTrue(sut.AllTablesExposed.Contains(td));
            }
        }

        [TestMethod]
        [UnitTest]
        public void WhenDiscoveryProvidesDuplicateTables_DiscoveryThrows()
        {
            TableDescriptorUtils.CreateTableDescriptors(
                serializer,
                out var allDescriptors,
                out var buildTableActions,
                typeof(StubDataTableOne),

                typeof(StubDataTableTwo),

                typeof(StubMetadataTableOne),
                typeof(StubMetadataTableTwo),

                typeof(StubMetadataTableNoBuildMethod),
                typeof(StubDataTableOneNoBuildMethod),
                typeof(StubDataTableOneNoBuildMethod),
                typeof(StubDataTableTwoNoBuildMethod));

            var expectedDescriptors = new List<TableDescriptor>();
            for (int x = 0; x < allDescriptors.Count; x++)
            {
                if (buildTableActions[x] is null && !allDescriptors[x].RequiresDataExtensions())
                {
                    expectedDescriptors.Add(allDescriptors[x]);
                }
            }

            var discovery = new FakeTableProvider();
            discovery.DiscoverReturnValue = expectedDescriptors;

            var sut = new StubDataSource(discovery);

            Assert.ThrowsException<InvalidOperationException>(() => sut.SetApplicationEnvironment(applicationEnvironment));
        }

        [ProcessingSource("{CABDB99F-F182-457B-B0B4-AD3DD62272D8}", "One", "One")]
        [FileDataSource(".csv")]
        private sealed class StubDataSource
            : ProcessingSource
        {
            static StubDataSource()
            {
                Assembly = typeof(StubDataSource).Assembly;
            }

            public StubDataSource()
                : this(TableDiscovery.CreateForAssembly(Assembly))
            {
            }

            public StubDataSource(IProcessingSourceTableProvider discovery)
               : base(discovery)
            {
                this.CommandLineOptionsToReturn = new List<Option>();
                this.SetApplicationEnvironmentCalls = new List<IApplicationEnvironment>();
                this.ProcessorToReturn = new MockCustomDataProcessor();
                this.CreateProcessorCoreCalls =
                    new List<Tuple<IEnumerable<IDataSource>, IProcessorEnvironment, ProcessorOptions>>();
            }

            public static Assembly Assembly { get; set; }

            public HashSet<TableDescriptor> AllTablesExposed => new HashSet<TableDescriptor>(this.AllTables);

            public List<Option> CommandLineOptionsToReturn { get; set; }
            public override IEnumerable<Option> CommandLineOptions => this.CommandLineOptionsToReturn ?? Enumerable.Empty<Option>();

            public List<IApplicationEnvironment> SetApplicationEnvironmentCalls { get; }
            protected override void SetApplicationEnvironmentCore(IApplicationEnvironment applicationEnvironment)
            {
                this.SetApplicationEnvironmentCalls.Add(applicationEnvironment);
            }

            public ICustomDataProcessor ProcessorToReturn { get; set; }
            public List<Tuple<IEnumerable<IDataSource>, IProcessorEnvironment, ProcessorOptions>> CreateProcessorCoreCalls { get; }
            protected override ICustomDataProcessor CreateProcessorCore(
                IEnumerable<IDataSource> dataSources,
                IProcessorEnvironment processorEnvironment,
                ProcessorOptions options)
            {
                this.CreateProcessorCoreCalls.Add(
                    Tuple.Create(
                        dataSources,
                        processorEnvironment,
                        options));

                return this.ProcessorToReturn;
            }

            protected override bool IsDataSourceSupportedCore(IDataSource dataSource)
            {
                return true;
            }
        }
    }
}
