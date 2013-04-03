using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.UI.Popups;

namespace Cyclestreets
{
    public class AsyncWebRequest
    {
        private string uri;
        private enum EState
        {
            NOT_STARTED,
            STARTED,
            COMPLETE,
        }
        private EState requestState;
        private Action<byte[]> OnComplete;


        public AsyncWebRequest(String uri, Action<byte[]> OnComplete)
        {
            this.uri = uri;
            this.OnComplete = OnComplete;
            requestState = EState.NOT_STARTED;
        }

        public async void Start()
        {
            requestState = EState.STARTED;

            byte[] taskData = await Task.Run(() => GetURLContentsAsync(this.uri));

            requestState = EState.COMPLETE;

            this.OnComplete(taskData);
        }

        public bool isComplete()
        {
            return requestState == EState.COMPLETE;
        }

        public static Task<byte[]> GetURLContentsAsync(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create( url );
            
			try
			{
				Task<WebResponse> task = Task.Factory.FromAsync<WebResponse>( request.BeginGetResponse, request.EndGetResponse, null );
				return task.ContinueWith( t =>
				{
					if( t.IsFaulted )
					{
						//handle error
						//Exception firstException = t.Exception.InnerExceptions.First();
						//Util.networkFailure();
						return null;
					}
					else
					{
						return ReadStreamFromResponse( t.Result );
					}
				} );

				//return task.ContinueWith( t => ReadStreamFromResponse( t.Result ) ); 
			}
			catch (System.Exception ex)
			{
				MessageBox.Show( "An error occurred getting data from "+url );
				//FlurryWP8SDK.Api.LogError("An error occurred getting data from " + url, ex);
				return null;
			}
            
        }

        private static byte[] ReadStreamFromResponse( WebResponse response )
        {
            try
            {
                using( var responseStream = response.GetResponseStream() )
                {
                    var content = new MemoryStream();
                    responseStream.CopyTo( content );
                    return content.ToArray();
                }
            }
            catch( System.Exception ex )
            {
				//FlurryWP8SDK.Api.LogError("Failed to read response stream", ex);
                return null;
            }
        }
    }
}
