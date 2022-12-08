using System;
using Microsoft.Extensions.DependencyInjection;

namespace API.DataSyncSyncer.Client;

public class DataSyncClientFactory : IDataSyncClientFactory
{
    private readonly IServiceProvider serviceProvider;

    public DataSyncClientFactory(IServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

    public IDataSyncClient CreateClient() => serviceProvider.GetRequiredService<IDataSyncClient>();
}
