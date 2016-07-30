﻿using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using Orleans.TestingHost;
using System.Collections.Generic;

namespace Tester
{
    public class DefaultClusterFixture : BaseTestClusterFixture
    {
        protected override TestCluster CreateTestCluster()
        {
            var options = new TestClusterOptions();
            options.ClusterConfiguration.AddMemoryStorageProvider("Default");
            options.ClusterConfiguration.AddMemoryStorageProvider("MemoryStore");
            options.ClusterConfiguration.Defaults.Assemblies = new List<string> { 
                    @".\TestGrainInterfaces.dll",
                    @".\TestInternalGrainInterfaces.dll",
                    @".\TestFSharpInterfaces.dll",
                    @".\TestFSharp.dll"};
            options.ClientConfiguration.Assemblies = new List<string> { 
                    @".\TestGrainInterfaces.dll",
                    @".\TestFSharpInterfaces.dll",
                    @".\TestInternalGrainInterfaces.dll"};
            return new TestCluster(options);
        }
    }
}
