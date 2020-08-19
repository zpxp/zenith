using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Zenith.Core;

namespace Zenith.Service
{

	/// <summary>
	/// 
	/// </summary>
	public static class SqlService
	{
		/// <summary>
		/// Change to override profile container behavoir
		/// </summary>
		/// <returns></returns>
		public static IProfileContainer ProfileContainer = new DefaultProfileContainer();

		/// <summary>
		/// Call to add Zenith sql to the specified IServiceCollection 
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configure"></param>
		/// <returns></returns>
		public static IServiceCollection AddZenithSql(this IServiceCollection services, Action<SqlConfiguration> configure)
		{
			var opts = new SqlConfiguration();
			configure(opts);
			if (string.IsNullOrWhiteSpace(opts.ConnectionString))
			{
				throw new ArgumentException("ConnectionString is required");
			}
			if (opts.Provider == null)
			{
				throw new ArgumentException("Provider is required");
			}

			if (ProfileContainer.HasProfile(opts.Profile))
			{
				throw new ArgumentException($"Sql profile '{opts.Profile}' already registered. Cannot add duplicate profile.");
			}

			// record the profile
			ProfileContainer.AddProfile(opts.Profile, opts);

			// create a service that resolves configs based on profiles
			services.TryAddTransient((context) =>
			{
				Func<string, SqlConfiguration> factory = profile =>
				{
					if (ProfileContainer.HasProfile(profile))
					{
						return ProfileContainer.GetProfile(profile);
					}
					else
					{
						throw new ArgumentException($"Cannot find sql profile '{profile}'.");
					}
				};
				return factory;
			});

			// 'default' profile unit of work
			services.TryAddLazyScoped<IUnitOfWork, UnitOfWork>();

			// resolve the default profile config
			services.TryAddTransient<SqlConfiguration>((context) =>
			{
				return ProfileContainer.GetProfile("default");
			});

			// resolve all other UOW profiles via Func<string, IUnitOfWork> injections
			services.TryAddScoped<Func<string, IUnitOfWork>>(context =>
			{
				// this function will be invoked once PerLifetimeScope
				IUnitOfWork instance = null;
				return (profile) =>
				{
					// this function will be called on every Func<string, IUnitOfWork> invoke
					if (instance == null)
					{
						var config = ProfileContainer.GetProfile(profile);
						instance = ActivatorUtilities.CreateInstance<UnitOfWork>(context, context, config);
					}
					return instance;
				};
			});

			return services;
		}



		private static void TryAddLazyScoped<TInterface, TImplement>(this IServiceCollection services) where TInterface : class where TImplement : class, TInterface
		{
			services.TryAddScoped<TInterface, TImplement>();
			// add lazy factory for all bl types e.g. -> Func<LogicType> 
			services.TryAddScoped<Func<TInterface>>(context => () => context.GetService<TInterface>());
		}
	}
}