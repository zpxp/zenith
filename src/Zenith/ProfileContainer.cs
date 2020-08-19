using System.Collections.Generic;

namespace Zenith
{

	/// <summary>
	/// Container used to hold all profiles for the sql configuration
	/// </summary>
	public interface IProfileContainer
	{
		void AddProfile(string name, SqlConfiguration config);
		SqlConfiguration GetProfile(string name);
		bool HasProfile(string name);
	}

	/// <summary>
	/// Singleton instance that holds sql profiles
	/// </summary>
	internal class DefaultProfileContainer : IProfileContainer
	{
		private Dictionary<string, SqlConfiguration> profiles = new Dictionary<string, SqlConfiguration>();

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