﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using CacheStack;
using CacheStack.DonutCaching;
using Google.GData.Analytics;
using Google.GData.Client;
using MvcKickstart.Analytics.Infrastructure.Extensions;
using MvcKickstart.Analytics.Models;
using MvcKickstart.Analytics.Models.GoogleAnalytics;
using MvcKickstart.Analytics.ViewModels.Widgets;
using MvcKickstart.Infrastructure;
using MvcKickstart.Analytics.Services;
using MvcKickstart.RestSharp;
using RestSharp;
using ServiceStack.CacheAccess;
using Spruce;

namespace MvcKickstart.Analytics.Controllers
{
//	[Restricted(RequireAdmin = true)]
	[RouteArea("admin")]
	public class WidgetsController : MvcKickstart.Infrastructure.ControllerBase
	{
		private readonly ISiteSettingsService _siteSettingsService;
		public WidgetsController(IDbConnection db, ICacheClient cache, IMetricTracker metrics, ISiteSettingsService siteSettingsService) : base(db, cache, metrics)
		{
			_siteSettingsService = siteSettingsService;
		}

		[HttpGet, Route("widgets/analytics", Name = "MvcKickstart_Analytics_Widgets_Index")]
		public ActionResult Index()
		{			
			return Redirect(Url.AdminHome());
		}

		[HttpGet, Route("widgets/analyticsWidget", Name = "MvcKickstart_Analytics_Widgets_AnalyticsWidget")]
		public ActionResult AnalyticsWidget()
		{			
			return View();
		}

		[HttpGet, Route("widgets/analytics/authResponse", Name = "MvcKickstart_Analytics_Widgets_AuthResponse")]
		public ActionResult AuthResponse(string token)
		{
			var sessionToken = AuthSubUtil.exchangeForSessionToken(token, null);

			var settings = _siteSettingsService.GetSettings();
			settings.AnalyticsToken = sessionToken;
			Db.Save(settings);
			Cache.Trigger(TriggerFor.Id<SiteSettings>(settings.Id));

			return Redirect(Url.AdminHome());
		}

		[HttpPost, Route("widgets/analytics", Name = "MvcKickstart_Analytics_Widgets_Analytics")]
		[DonutOutputCache]
		public ActionResult Analytics(int? duration)
		{
			CacheContext.InvalidateOn(TriggerFrom.Any<SiteSettings>());

			var settings = _siteSettingsService.GetSettings();

			if (string.IsNullOrEmpty(settings.AnalyticsToken))
			{
				const string scope = "https://www.google.com/analytics/feeds/";
				var next = Url.Absolute(Url.AnalyticsWidget().AuthResponse());
				var auth = new Authorize
					{
						Url = AuthSubUtil.getRequestUrl(next, scope, false, true)
					};
				return View("AnalyticsAuthorize", auth);
			}

			if (string.IsNullOrEmpty(settings.AnalyticsProfileId))
			{
				var config = new Config
					{
						Accounts = GetAccounts(settings),
						Profiles = GetProfiles(settings)
					};

				return View("AnalyticsConfig", config);
			}

			duration = duration ?? 30;
			var model = new ViewModels.Widgets.Analytics
			            	{
								Duration = duration.Value,
								Start = DateTime.Today.AddDays(-1 * duration.Value),
								End = DateTime.Now,
						 		Visits = new Dictionary<DateTime, int>(),
								PageViews = new Dictionary<string, int>(),
								PageTitles = new Dictionary<string, string>(),
								TopReferrers = new Dictionary<string, int>(),
								TopSearches = new Dictionary<string, int>()
			            	};
			if (model.Start > model.End)
			{
				var tempDate = model.Start;
				model.Start = model.End;
				model.End = tempDate;
			}

			var profiles = GetProfiles(settings);			
			var profile = profiles.SingleOrDefault(x => x.Id == settings.AnalyticsProfileId);
			if (profile == null)
				throw new Exception("Unable to find the specified analytics profile: " + settings.AnalyticsProfileId);
			model.Profile = profile;

			var authFactory = new GAuthSubRequestFactory("analytics", "MvcKickstart")
								{
									Token = settings.AnalyticsToken
								};
								
			var analytics = new AnalyticsService(authFactory.ApplicationName) { RequestFactory = authFactory };

			var profileId = "ga:" + settings.AnalyticsProfileId;

			// Get from All Visits
			var visits = new DataQuery(profileId, model.Start, model.End)
							{
								Metrics = "ga:visits",
								Dimensions = "ga:date",
								Sort = "ga:date"
							};
			var count = 0;
			foreach (DataEntry entry in analytics.Query(visits).Entries)
			{
				var value = entry.Metrics.First().IntegerValue;

				model.Visits.Add(model.Start.AddDays(count++), value);
			}

			// Get Site Usage
			var siteUsage = new DataQuery(profileId, model.Start, model.End)
							{
								Metrics = "ga:visits,ga:pageviews,ga:percentNewVisits,ga:avgTimeOnSite,ga:entranceBounceRate,ga:exitRate,ga:pageviewsPerVisit,ga:avgPageLoadTime"
							};
			var siteUsageResult = (DataEntry)analytics.Query(siteUsage).Entries.FirstOrDefault();
			if (siteUsageResult != null)
			{
				foreach (var metric in siteUsageResult.Metrics)
				{
					switch (metric.Name)
					{
						case "ga:visits":
							model.TotalVisits = metric.IntegerValue;
							break;
						case "ga:pageviews":
							model.TotalPageViews = metric.IntegerValue;
							break;
						case "ga:percentNewVisits":
							model.PercentNewVisits = metric.FloatValue;
							break;
						case "ga:avgTimeOnSite":
							model.AverageTimeOnSite = TimeSpan.FromSeconds(metric.FloatValue);
							break;
						case "ga:entranceBounceRate":
							model.EntranceBounceRate = metric.FloatValue;
							break;
						case "ga:exitRate":
							model.PercentExitRate = metric.FloatValue;
							break;
						case "ga:pageviewsPerVisit":
							model.PageviewsPerVisit = metric.FloatValue;
							break;
						case "ga:avgPageLoadTime":
							model.AveragePageLoadTime = TimeSpan.FromSeconds(metric.FloatValue);
							break;
					}
				}
			}

			// Get Top Pages
			var topPages = new DataQuery(profileId, model.Start, model.End)
							{
								Metrics = "ga:pageviews",
								Dimensions = "ga:pagePath,ga:pageTitle",
								Sort = "-ga:pageviews",
								NumberToRetrieve = 20
							};
			foreach (DataEntry entry in analytics.Query(topPages).Entries)
			{
				var value = entry.Metrics.First().IntegerValue;
				var url = entry.Dimensions.Single(x => x.Name == "ga:pagePath").Value.ToLowerInvariant();
				var title = entry.Dimensions.Single(x => x.Name == "ga:pageTitle").Value;

				if (!model.PageViews.ContainsKey(url))
					model.PageViews.Add(url, 0);
				model.PageViews[url] += value;

				if (!model.PageTitles.ContainsKey(url))
					model.PageTitles.Add(url, title);
			}

			// Get Top Referrers
			var topReferrers = new DataQuery(profileId, model.Start, model.End)
							{
								Metrics = "ga:visits",
								Dimensions = "ga:source,ga:medium",
								Sort = "-ga:visits",
								Filters = "ga:medium==referral",
								NumberToRetrieve = 5
							};
			foreach (DataEntry entry in analytics.Query(topReferrers).Entries)
			{
				var visitCount = entry.Metrics.First().IntegerValue;
				var source = entry.Dimensions.Single(x => x.Name == "ga:source").Value.ToLowerInvariant();

				model.TopReferrers.Add(source, visitCount);
			}

			// Get Top Searches
			var topSearches = new DataQuery(profileId, model.Start, model.End)
							{
								Metrics = "ga:visits",
								Dimensions = "ga:keyword",
								Sort = "-ga:visits",
								Filters = "ga:keyword!=(not set);ga:keyword!=(not provided)",
								NumberToRetrieve = 5
							};
			foreach (DataEntry entry in analytics.Query(topSearches).Entries)
			{
				var visitCount = entry.Metrics.First().IntegerValue;
				var source = entry.Dimensions.Single(x => x.Name == "ga:keyword").Value.ToLowerInvariant();

				model.TopSearches.Add(source, visitCount);
			}

			return View(model);
		}

		private IList<Account> GetAccounts(SiteSettings settings)
		{
			// Using RestSharp because I could not figure out how to access the analytics management api with the analytics nuget
			var client = new RestClient();
			client.AddHandler("application/json", new RestSharpJsonSerializer());
			var accountsRequest = new RestRequest("https://www.googleapis.com/analytics/v3/management/accounts?access_token=" + Server.UrlEncode(settings.AnalyticsToken), Method.GET)
				{
					RequestFormat = DataFormat.Json,
					JsonSerializer = new RestSharpJsonSerializer()
				};
			var accountsResult = client.Execute<ListResponse<Account>>(accountsRequest);
			return accountsResult.Data.Items ?? new List<Account>();
		}
		private IList<Profile> GetProfiles(SiteSettings settings)
		{
			// Using RestSharp because I could not figure out how to access the analytics management api with the analytics nuget
			var client = new RestClient();
			client.AddHandler("application/json", new RestSharpJsonSerializer());
			var profilesRequest = new RestRequest("https://www.googleapis.com/analytics/v3/management/accounts/~all/webproperties/~all/profiles?access_token=" + Server.UrlEncode(settings.AnalyticsToken), Method.GET)
				{
					RequestFormat = DataFormat.Json,
					JsonSerializer = new RestSharpJsonSerializer()
				};
			var profilesResult = client.Execute<ListResponse<Profile>>(profilesRequest);
			return profilesResult.Data.Items ?? new List<Profile>();
		}

		[HttpPost, Route("widgets/analytics/config", Name = "MvcKickstart_Analytics_Widgets_AnalyticsConfig")]
		public ActionResult AnalyticsConfig(Config model)
		{
			var settings = _siteSettingsService.GetSettings();
			var profiles = GetProfiles(settings);			
			var profile = profiles.SingleOrDefault(x => x.Id == model.ProfileId);
			if (profile == null)
				throw new Exception("Unable to find the specified analytics profile: " + model.ProfileId);

			settings.AnalyticsProfileId = profile.Id;
			Db.Save(settings);
			Cache.Trigger(TriggerFor.Id<SiteSettings>(settings.Id));

			return Redirect(Url.AdminHome());
		}
	}
}