﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using MarcelDigital.UmbracoExtensions.XmlSitemap.Filters;
using MarcelDigital.UmbracoExtensions.XmlSitemap.Generators;
using MarcelDigital.UmbracoExtensions.XmlSitemap.Optimization;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web;

namespace MarcelDigital.UmbracoExtensions.XmlSitemap {
    /// <summary>
    ///     Generetes the Xml sitemap for the umbraco website.
    /// </summary>
    public class XmlSitemapHandler : IHttpHandler {
        /// <summary>
        ///     Specifies whether the handler can be reused in the pool
        ///     or a new one needs to be created each time.
        /// </summary>
        public bool IsReusable => true;

        /// <summary>
        ///     Caching strategy for the XML sitemap.
        /// </summary>
        private readonly ISitemapCache _sitemapCache;

        /// <summary>
        ///     Generation strategy for the XML sitemap.
        /// </summary>
        private readonly IXmlSitemapGenerator _generator;

        /// <summary>
        ///     Constructor for the sitemap handler
        /// </summary>
        public XmlSitemapHandler() {
            _sitemapCache = new HttpContextCache(HttpContext.Current);
            _generator = new XmlSitemapGenerator();
        }

        /// <summary>
        ///     Default method for the http request
        /// </summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context) {
            UmbracoContext.EnsureContext(
                new HttpContextWrapper(HttpContext.Current),
                ApplicationContext.Current,
                true);

            var sitemap = GenerateSitemapXml(context);

            InsertSitemapIntoResponse(context, sitemap);
        }

        /// <summary>
        ///     Generates the sitemap and inserts it in the response
        /// </summary>
        /// <param name="context"></param>
        private XDocument GenerateSitemapXml(HttpContext context) {
            var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);

            XDocument sitemap = null;

            if (!_sitemapCache.IsInCache()) {
                var content = GetContent(umbracoHelper);

                sitemap = _generator.Generate(content);

                _sitemapCache.Insert(sitemap);
            } else {
                sitemap = _sitemapCache.Retrieve();
            }

            return sitemap;
        }

        /// <summary>
        ///     Gets the Umbraco content to submit to the sitemap XML
        /// </summary>
        /// <param name="umbracoHelper"></param>
        protected virtual IEnumerable<IPublishedContent> GetContent(UmbracoHelper umbracoHelper) {
            var contentFilter = new NoTemplateFilter(umbracoHelper);

            return contentFilter.GetContent();
        }

        /// <summary>
        ///     Puts the sitemap into the http response
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sitemap"></param>
        private static void InsertSitemapIntoResponse(HttpContext context, XDocument sitemap) {
            var response = context.Response;
            response.Clear();
            response.ContentType = "text/xml";

            using (var streamWriter = new StreamWriter(response.OutputStream, Encoding.UTF8)) {
                var xmlWriter = new XmlTextWriter(streamWriter);
                sitemap.WriteTo(xmlWriter);
            }
            response.End();
        }
    }
}