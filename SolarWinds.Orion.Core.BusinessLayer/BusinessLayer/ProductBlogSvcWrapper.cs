using System;
using System.Collections.Generic;
using SolarWinds.Orion.Core.BusinessLayer.DAL;
using SolarWinds.Orion.Core.Common.Models;

namespace SolarWinds.Orion.Core.BusinessLayer
{
	// Token: 0x02000034 RID: 52
	public class ProductBlogSvcWrapper
	{
		// Token: 0x060003B7 RID: 951 RVA: 0x00018760 File Offset: 0x00016960
		public static BlogItemDAL GetBlogItem(RssBlogItem rssBlog)
		{
			BlogItemDAL blogItemDAL = new BlogItemDAL();
			blogItemDAL.Title = rssBlog.Title;
			blogItemDAL.Description = rssBlog.Description;
			blogItemDAL.Ignored = false;
			blogItemDAL.Url = rssBlog.Link;
			blogItemDAL.SetNotAcknowledged();
			blogItemDAL.PostGuid = rssBlog.PostGuid;
			blogItemDAL.PostId = rssBlog.PostId;
			blogItemDAL.Owner = rssBlog.Creator;
			blogItemDAL.PublicationDate = rssBlog.PubDate;
			blogItemDAL.CommentsUrl = rssBlog.CommentsURL;
			blogItemDAL.CommentsCount = rssBlog.CommentsNumber;
			return blogItemDAL;
		}

		// Token: 0x060003B8 RID: 952 RVA: 0x000187EC File Offset: 0x000169EC
		public static List<BlogItemDAL> GetBlogItems(RssBlogItems rssBlogs)
		{
			List<BlogItemDAL> list = new List<BlogItemDAL>();
			foreach (RssBlogItem rssBlog in rssBlogs.ItemList)
			{
				list.Add(ProductBlogSvcWrapper.GetBlogItem(rssBlog));
			}
			return list;
		}
	}
}
