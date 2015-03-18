using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using ServiceStack.CacheAccess;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;
using StructureMap.Configuration.DSL;
using StructureMap.Graph;
using StructureMap.Web;

namespace MvcKickstart.Infrastructure
{
	public class IocRegistry : Registry
	{
		public IocRegistry(params Assembly[] additionalAssembliesToScan)
		{
			Scan(scan =>
					{
						scan.TheCallingAssembly();
						if (additionalAssembliesToScan != null)
						{
							foreach (var assembly in additionalAssembliesToScan)
							{
								scan.Assembly(assembly);
							}
						}
						// Make sure all plugins/modules are wired up with default conventions
						scan.AssembliesFromApplicationBaseDirectory(x => x.FullName.Contains("MvcKickstart"));
						scan.AssemblyContainingType<IocRegistry>();
						scan.WithDefaultConventions();
					});

            var connStr = "Default";
            if (ConfigurationManager.ConnectionStrings[Environment.MachineName] != null)
                connStr = Environment.MachineName;
            For<SqlConnection>()
                .Use(
                    () =>
                        new SqlConnection(ConfigurationManager.ConnectionStrings[connStr].ConnectionString));

			For<IDbConnection>()
				.HybridHttpOrThreadLocalScoped()
                .Use(ctx => OpenConnection(ctx.GetInstance<SqlConnection>()))
				.Named("Database Connection");

			For<IMetricTracker>()
				.Singleton()
				.Use(() => StartMetricTracker())
				.Named("Metric Tracker");

			For<ICacheClient>()
				.Singleton()
				.Use(x => new MemoryCacheClient());
		}

        private static ProfiledDbConnection OpenConnection(DbConnection connection)
        {
            connection.Open();
            return new ProfiledDbConnection(connection, MiniProfiler.Current);
        }

        private static MetricTracker StartMetricTracker()
        {
            int port;
            int.TryParse(ConfigurationManager.AppSettings["Metrics:Port"], out port);
            return new MetricTracker(ConfigurationManager.AppSettings["Metrics:Host"], port, ConfigurationManager.AppSettings["Metrics:Prefix"]);
        }
	}
}
