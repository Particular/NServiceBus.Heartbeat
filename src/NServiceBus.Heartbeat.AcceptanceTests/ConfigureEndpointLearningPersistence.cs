﻿using System;
using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NUnit.Framework;

public class ConfigureEndpointLearningPersistence
{
    public Task Configure(EndpointConfiguration configuration)
    {
        var testRunId = TestContext.CurrentContext.Test.ID;

        string tempDir;

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            //can't use bin dir since that will be too long on the build agents
            tempDir = @"c:\temp";
        }
        else
        {
            tempDir = Path.GetTempPath();
        }

        storageDir = Path.Combine(tempDir, testRunId);

        configuration.UsePersistence<AcceptanceTestingPersistence, StorageType.Subscriptions>();
        configuration.UsePersistence<AcceptanceTestingPersistence, StorageType.Timeouts>();

        configuration.UsePersistence<LearningPersistence, StorageType.Sagas>()
            .SagaStorageDirectory(storageDir);

        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        if (Directory.Exists(storageDir))
        {
            Directory.Delete(storageDir, true);
        }
        return Task.FromResult(0);
    }

    string storageDir;
}