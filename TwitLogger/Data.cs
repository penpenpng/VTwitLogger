using System;
using LinqToTwitter;

namespace TwitLogger
{
	class SimpleStatus
	{
		public DateTime CreatedAt { get; set; }
		public ulong StatusID { get; set; }
		public SimpleUser User { get; set; }
		public string Text { get; set; }
		public int FavoriteCount { get; set; }
		public int RetweetedCount { get; set; }
		public bool IsRetweet { get; set; }
		public string OriginalText { get; set; }
		public SimpleUser OriginalUser { get; set; }

		public SimpleStatus(Status status)
		{
			CreatedAt = status.CreatedAt;
			StatusID = status.StatusID;
			User = new SimpleUser(status.User);
			Text = status.Text;
			FavoriteCount = status.FavoriteCount ?? -1;
			RetweetedCount = status.RetweetCount;
			IsRetweet = status.Retweeted;
			if (IsRetweet)
			{
				OriginalText = status.RetweetedStatus?.Text;
				OriginalUser = new SimpleUser(status.RetweetedStatus.User);
			}
		}
	}

	class SimpleUser
	{
		public ulong UserID { get; set; }
		public string ScreenName { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public int FolloweesCount { get; set; }
		public int FollowersCount { get; set; }
		public int StatusesCount { get; set; }
		public int FavoritesCount { get; set; }
		public DateTime CreatedAt { get; set; }

		public SimpleUser(User user)
		{
			UserID = user.UserID;
			ScreenName = user.ScreenNameResponse;
			Name = user.Name;
			Description = user.Description;
			FolloweesCount = user.FriendsCount;
			FollowersCount = user.FollowersCount;
			StatusesCount = user.StatusesCount;
			FavoritesCount = user.FavoritesCount;
		}
	}
}
