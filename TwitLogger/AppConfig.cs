using System.Configuration;

namespace TwitLogger
{
	class AppConfig
	{
		static AppSettingsReader AppSettingsReader = new AppSettingsReader();
		public static string Get(string key) => (string)AppSettingsReader.GetValue(key, typeof(string));
	}
}
