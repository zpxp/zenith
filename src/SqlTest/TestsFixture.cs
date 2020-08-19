using System;
using Microsoft.Extensions.DependencyInjection;
using Zenith;
using System.IO;
using System.Data.SQLite;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Zenith.Service;
using System.Threading;
using System.Collections.Concurrent;
using Xunit;

//tests only work syncro
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ZenithTest
{
	public class TestsFixture : IDisposable
	{
		public ServiceCollection services;
		string DBFILE = Guid.NewGuid().ToString();
		string connectionString;
		public TestsFixture()
		{
			// set to thread independant test container so each test class has own profiles
			SqlService.ProfileContainer = new TestProfileContainer();

			connectionString = $"Data Source={DBFILE};Mode=Memory;Cache=Shared;Version=3;";

			InitDb();

			// Do "global" initialization here; Only called once.
			services = new ServiceCollection();
			services.AddTransient<ILogger, Logger>();

			// register sql module
			services.AddSql(options =>
			{
				options.Profile = "default";
				options.ConnectionString = connectionString;
				options.Provider = new Zenith.Providers.SQLite.SQLiteProvider();
				options.AddMiddleware(async (context, next) =>
				{
					if ((SqlTypeEnum.Update | SqlTypeEnum.Insert).HasFlag(context.Type))
					{
						foreach (var item in context.ObjectParameters)
						{
							if (item.Data is IUpdatable u)
							{
								u.UpdatedOn = DateTime.UtcNow;
							}
						}
					}

					return await next();
				});
			});

			services.AddSql(options =>
			{
				options.Profile = "profile 2";
				options.ConnectionString = connectionString;
				options.Provider = new Zenith.Providers.SQLite.SQLiteProvider();
			});
		}

		private void InitDb()
		{
			var assembly = typeof(TestsFixture).GetTypeInfo().Assembly;
			var g = assembly.ManifestModule;
			Stream resource = assembly.GetManifestResourceStream("SqlTest.test.sql");

			using StreamReader reader = new StreamReader(resource);
			string sql = reader.ReadToEnd();

			using SQLiteConnection dbConn = new SQLiteConnection(connectionString);
			dbConn.Open();

			using SQLiteCommand command = new SQLiteCommand(sql, dbConn);
			command.ExecuteNonQuery();
			Console.WriteLine("Create DB Successful " + DBFILE);
		}

		public void Dispose()
		{
			// destroy current container so next test run in sync can create a fresh container
			SqlService.ProfileContainer = null;
		}

		class TestProfileContainer : IProfileContainer
		{
			// each test should have its own container
			Dictionary<string, SqlConfiguration> profiles = new Dictionary<string, SqlConfiguration>();


			public void AddProfile(string name, SqlConfiguration config)
			{
				profiles.Add(name, config);
			}

			public SqlConfiguration GetProfile(string name)
			{
				return profiles[name];
			}

			public bool HasProfile(string name)
			{
				return profiles.ContainsKey(name);
			}
		}
	}


	class Logger : ILogger
	{
		public IDisposable BeginScope<TState>(TState state)
		{
			return null;
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return true;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			Console.WriteLine($"Test: {logLevel} {formatter(state, exception)}");
		}
	}

}