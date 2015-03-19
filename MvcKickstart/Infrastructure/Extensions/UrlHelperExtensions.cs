﻿using System;
using System.Configuration;
using System.Web.Mvc;
using CacheStack;
using MvcKickstart.Infrastructure.Extensions;
using ServiceStack;
using ServiceStack.CacheAccess;
using StructureMap;

namespace MvcKickstart.Analytics.Infrastructure.Extensions
{
	public static class UrlHelperExtensions
	{
        private static ICacheClient Cache { get; set; }

        static UrlHelperExtensions()
        {
            Cache = ObjectFactory.GetInstance<ICacheClient>();
        }

        private static string GetFileHash(string filename)
        {
            var key = "__FileHash__" + filename;
            return Cache.GetOrCache(key, _ => filename.ToMD5Hash());
        }

        private static string GetHashedContentFile(this UrlHelper helper, string filename)
        {
            var hash = GetFileHash(filename);
            return helper.Content(filename) + "?v={0}".Fmt(hash);
        }

        public static string Image(this UrlHelper helper, string file)
        {
            var imageDir = ConfigurationManager.AppSettings.Get("ImagesDirectory");
            if (string.IsNullOrWhiteSpace(imageDir)) imageDir = "images";
            return helper.GetHashedContentFile("~/content/{0}/{1}".Fmt(imageDir, file));
        }

        /// <summary>
        /// This extension method will help generating Absolute Urls in the mailer or other views
        /// </summary>
        /// <param name="urlHelper">The object that gets the extended behavior</param>
        /// <param name="relativeOrAbsoluteUrl">A relative or absolute URL to convert to Absolute</param>
        /// <returns>An absolute Url. e.g. http://domain:port/controller/action from /controller/action</returns>
        /// <remarks>Shamelessly stolen from MvcMailer: https://github.com/smsohan/MvcMailer/blob/master/Mvc.Mailer/ExtensionMethods/UrlHelperExtensions.cs</remarks>
        public static string Absolute(this UrlHelper urlHelper, string relativeOrAbsoluteUrl)
        {
            var uri = new Uri(relativeOrAbsoluteUrl, UriKind.RelativeOrAbsolute);
            if (uri.IsAbsoluteUri)
                return relativeOrAbsoluteUrl;

            Uri combinedUri;
            if (Uri.TryCreate(BaseUrl(urlHelper), relativeOrAbsoluteUrl, out combinedUri))
                return combinedUri.AbsoluteUri;

            throw new Exception(string.Format("Could not create absolute url for {0} using baseUri {1}", relativeOrAbsoluteUrl,
                                              BaseUrl(urlHelper)));
        }

        private static Uri BaseUrl(UrlHelper urlHelper)
        {
            var baseUrl = ConfigurationManager.AppSettings.Get("BaseUrl");

            //No configuration given, so use the one from the context
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                if (urlHelper.RequestContext.HttpContext.Request.Url != null)
                    baseUrl = urlHelper.RequestContext.HttpContext.Request.Url.GetLeftPart(UriPartial.Authority);
            }

            return new Uri(baseUrl);
        }

		public static string AdminHome(this UrlHelper url)
		{
			return url.RouteUrl("Admin_Home_Index");
		}
		public static AnalyticsWidgetUrls AnalyticsWidget(this UrlHelper url)
		{
			return new AnalyticsWidgetUrls(url);
		}

		public class AnalyticsWidgetUrls
		{
			protected UrlHelper Url { get; private set; }
			public AnalyticsWidgetUrls(UrlHelper url)
			{
				Url = url;
			}

			public string AuthResponse()
			{
				return Url.RouteUrl("MvcKickstart_Analytics_Widgets_AuthResponse");
			}
			public string Analytics()
			{
				return Url.RouteUrl("MvcKickstart_Analytics_Widgets_Analytics");
			}
		}
	}
}
