using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LinqToTwitter;
using Newtonsoft.Json;


namespace TwitLogger
{
	class Program
	{
		static void Main(string[] args)
		{
			var scraper = new Scraper();

			try
			{
				scraper.Authorize().Wait();
			}
			catch
			{
				Console.WriteLine("Authrization Error");
				return;
			}

			var t1 = Task.Factory.StartNew(() => StoreListMembers(scraper));
			var t2 = Task.Factory.StartNew(() => StoreListTimeLine(scraper));
			Console.WriteLine("Scraping...");

			Task.WaitAny(t1, t2);
			if (t1.IsFaulted || t2.IsFaulted)
				Console.WriteLine("Some error occurred. Restart application to continue.");
		}


		static Task StoreListTimeLine(Scraper scraper)
		{
			while (true)
			{
				Thread.Sleep(1 * 60 * 1000);
				Dumper.Log(scraper.FetchListTimeLine());
			}
		}

		static Task StoreListMembers(Scraper scraper)
		{
			while (true)
			{
				var listMembers = scraper.FetchListMembers();
				Dumper.Log(listMembers);
				foreach (var user in listMembers)
				{
					Thread.Sleep(3 * 60 * 1000);
					string screenName = user.ScreenName;
					Dumper.Log(screenName, scraper.FetchFollowers(screenName));
				}
			}
		}
	}

	class Dumper
	{
		static readonly JsonSerializerSettings Settings = new JsonSerializerSettings()
		{
			Formatting = Formatting.Indented,
			NullValueHandling = NullValueHandling.Ignore,
			DefaultValueHandling = DefaultValueHandling.Ignore,
		};
		static readonly string StatusLogDirectory = Path.Combine(
			Directory.GetCurrentDirectory(),
			AppConfig.Get("StatusLogDirectoryName"));
		static readonly string UserLogDirectory = Path.Combine(
			Directory.GetCurrentDirectory(),
			AppConfig.Get("UserLogDirectoryName"));

		static string GetDateString() {
			var t = DateTime.Now;
			return $"{t.Month:00}-{t.Day:00}-{t.Hour:00}-{t.Minute:00}";
		}

		public static string JsonDump(SimpleStatus status) => JsonConvert.SerializeObject(status, typeof(SimpleStatus), Settings);
		public static string JsonDump(SimpleUser user) => JsonConvert.SerializeObject(user, typeof(SimpleUser), Settings);

		public static void Log(IEnumerable<SimpleStatus> timeline)
		{
			Directory.CreateDirectory(StatusLogDirectory);
			string filePath = Path.Combine(StatusLogDirectory, $"s{GetDateString()}.json");
			using (var f = new StreamWriter(filePath))
			{
				foreach (var status in timeline) f.WriteLine(JsonDump(status));
			}
		}

		public static void Log(IEnumerable<SimpleUser> users)
		{
			Directory.CreateDirectory(UserLogDirectory);
			string filePath = Path.Combine(UserLogDirectory, $"m{GetDateString()}.json");
			using (var f = new StreamWriter(filePath))
			{
				foreach (var user in users) f.WriteLine(JsonDump(user));
			}
		}

		public static void Log(string screenName, IEnumerable<SimpleUser> users)
		{
			var dirName = Path.Combine(UserLogDirectory, screenName);
			Directory.CreateDirectory(dirName);
			string filePath = Path.Combine(dirName, $"u{GetDateString()}.json");
			using (var f = new StreamWriter(filePath))
			{
				foreach (var user in users) f.WriteLine(JsonDump(user));
			}
		}
	}

	class Scraper
	{
		TwitterContext Context;

		public async Task Authorize()
		{
			var auth = new PinAuthorizer()
			{
				CredentialStore = new InMemoryCredentialStore
				{
					ConsumerKey = AppConfig.Get("ConsumerKey"),
					ConsumerSecret = AppConfig.Get("ConsumerSecret")
				},
				GoToTwitterAuthorization = pageLink => Process.Start(pageLink),
				GetPin = () =>
				{
					Console.Write("Enter the PIN number here: ");
					return Console.ReadLine();
				}
			};
			await auth.AuthorizeAsync();
			Context = new TwitterContext(auth);
		}

		public IEnumerable<SimpleStatus> FetchListTimeLine()
		{
			return (
				from
					list in Context.List
				where
					list.Type == ListType.Statuses &&
					list.OwnerScreenName == AppConfig.Get("ListOwnerScreenName") &&
					list.Slug == AppConfig.Get("ListSlug")
				select list.Statuses
			)
			.Single()
			.Select(s => new SimpleStatus(s));
		}

		public IEnumerable<SimpleUser> FetchListMembers()
		{
			return (
				from
					list in Context.List
				where
					list.Type == ListType.Members &&
					list.OwnerScreenName == AppConfig.Get("ListOwnerScreenName") &&
					list.Slug == AppConfig.Get("ListSlug") &&
					list.SkipStatus == true &&
					list.Count == 1000
				select list.Users
			)
			.Single()
			.Select(s => new SimpleUser(s));
		}

		public IEnumerable<SimpleUser> FetchFollowers(string screenName)
		{
			Func<long, Friendship> fetchFollowersPage = (c) => (
			   from
				   friend in Context.Friendship
			   where
				   friend.Type == FriendshipType.FollowersList &&
				   friend.ScreenName == screenName &&
				   friend.Cursor == c
			   select friend
		   ).Single();

			var followers = new List<SimpleUser>();

			{
				long cursor = -1;
				do
				{
					var friendship = fetchFollowersPage(cursor);
					followers.AddRange(friendship.Users.Select(u => new SimpleUser(u)));
					cursor = friendship.CursorMovement.Next;
				} while (cursor != 0);
			}

			return followers;
		}
	}

}
