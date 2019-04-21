// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.IO;
using System.Net;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class extends WebClient to provide an infinite timeout on the upload to webonary and to disable AutoRedirect so that an incorrect site
	/// name returns an error instead of sending us a webpage as a response.
	/// </summary>
	public class WebonaryClient : WebClient, IWebonaryClient
	{
		public HttpStatusCode ResponseStatusCode { get; set; }

		protected override WebRequest GetWebRequest(Uri address)
		{
			var request = base.GetWebRequest(address);

			if (request.GetType() == typeof(HttpWebRequest))
			{
				((HttpWebRequest)request).Timeout = -1;
				((HttpWebRequest)request).AllowAutoRedirect = false;
			}

			return request;
		}

		/// <summary>
		/// Wraps the UploadFile from WebClient to provide status accessor and allow mocking returns for the unit tests.
		/// </summary>
		/// <exception cref="WebonaryException"></exception>
		public byte[] UploadFileToWebonary(string address, string fileName)
		{
			try
			{
				return UploadFile(address, fileName);
			}
			catch (WebException ex)
			{
				if(ex.Response == null)
					throw new WebonaryException("WebException with null response stream.", ex);
				using (var stream = ex.Response.GetResponseStream())
				using (var reader = new StreamReader(stream))
				{
					var response = reader.ReadToEnd();
					throw new WebonaryException(response, ex);
				}
			}
		}

		protected override WebResponse GetWebResponse(WebRequest request)
		{
			var response = base.GetWebResponse(request);
			return SetStatusAndReturn((HttpWebResponse)response);
		}


		protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
		{
			var response = base.GetWebResponse(request, result);
			return SetStatusAndReturn((HttpWebResponse)response);
		}

		private WebResponse SetStatusAndReturn(HttpWebResponse response)
		{
			ResponseStatusCode = response.StatusCode;
			return response;
		}

		public class WebonaryException : Exception
		{
			public WebException WebException { get; private set; }
			public HttpStatusCode StatusCode { get; internal set; }
			/// <summary>
			/// The full response returned by the server. Useful for debugging connection issues.
			/// </summary>
			public string FullResponse { get; set; }

			public WebonaryException(WebException webException) : this(null, webException)
			{
			}

			internal WebonaryException(string fullResponse, WebException webException)
			{
				FullResponse = fullResponse;
				WebException = webException;
				if (webException.Response != null)
				{
					StatusCode = ((HttpWebResponse)webException.Response).StatusCode;
				}
			}
		}
	}
}