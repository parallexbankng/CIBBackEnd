using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CIB.Core.Utils
{
	public static class Formater
	{
		public static string PhoneNumber(string phone)
		{
			var validateNumber = String.Empty;
			if (phone[..3] == "234")
			{
				validateNumber = phone;
			}
			else if (phone[..4] == "+234")
			{
				validateNumber = phone.Trim().Substring(1);
			}
			else if (phone.Length == 11)
			{
				validateNumber = "234" + phone.Trim().Substring(1);
			}
			else
			{
				validateNumber = phone;
			}
			return validateNumber;
		}
		public static string SerializeData(object postdata, string contentType)
		{
			var resp = "";
			switch (contentType)
			{
				case "application/json":
					resp = JsonConvert.SerializeObject(postdata);
					break;
				case "application/x-www-form-urlencoded":
					var jo = (JObject)postdata;
					resp = string.Join("&", jo.Properties().Select(property => property.Name + "=" + property.Value.ToString()).ToArray());
					break;
				default:
					break;
			}
			return resp;
		}
		public static string GetContentType(string path)
		{
			var provider = new FileExtensionContentTypeProvider();
			if (!provider.TryGetContentType(path, out var contentType))
			{
				contentType = "application/octet-stream";
			}
			return contentType;
		}
		public static string JsonType(object data)
		{
			return JsonConvert.SerializeObject(data);
		}
	}

	public class PaginationHelper<T>
	{
		private readonly List<T> items;
		private readonly int itemsPerPage;
		private readonly long TotalRecord;
		public PaginationHelper(List<T> collection, int itemsPerPage, long totalRecord)
		{
			this.items = collection;
			this.itemsPerPage = itemsPerPage;
			this.TotalRecord = totalRecord;
		}

		public int PageCount => (int)Math.Ceiling((double)TotalRecord / itemsPerPage);
		public long ItemCount => TotalRecord;
		public List<T> Page(int pageNumber)
		{
			if (pageNumber < 1 || pageNumber > PageCount) return new List<T>();
			return items.Skip((pageNumber - 1) * itemsPerPage).Take(itemsPerPage).ToList();
		}
	}

	public class Pagination<T>
	{
		public int TotalRecords { get; }
		public int TotalPages { get; }
		public int CurrentPages { get; }
		public List<T> Data { get; }

		public Pagination(List<T> sourceData, int pageNumber, int pageSize, int totalRecord)
		{
			TotalRecords = totalRecord;
			TotalPages = (int)Math.Ceiling((double)totalRecord / pageSize);
			CurrentPages = pageNumber;
			Data = sourceData;
		}
	}
}