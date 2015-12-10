using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Threading;
using System.Net;
using Java.Net;
using Java.IO;
using System.IO;

using System.Net.Sockets;
using System.Text;

namespace App1
{
    enum Device { Sensors, Relay };

	[Activity (Label = "App1", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		string IP = "0.0.0.0";
		string port = "80";
		TextView textLight;
		TextView textDistance;
		Button buttonSensors;
		Switch switchRelay;
		private const int listenPort = 5123;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetContentView (Resource.Layout.Main);

			//Creating objects that represents the components of the screen
			textLight = FindViewById<TextView>(Resource.Id.textLight);
			textDistance = FindViewById<TextView>(Resource.Id.textDistance);
	        buttonSensors = FindViewById<Button>(Resource.Id.buttonSensors);
			switchRelay = FindViewById<Switch>(Resource.Id.switchRelay);

            buttonSensors.Click += delegate
			{
                buttonSensors.Text = "...";
				ThreadPool.QueueUserWorkItem(o => Request(Device.Sensors));
			};

			switchRelay.Click += delegate
			{				
				ThreadPool.QueueUserWorkItem(o => Request(Device.Relay));
			};

			ThreadPool.QueueUserWorkItem(o => StartListener());

			try //broadcast
			{
				UdpClient client = new UdpClient();
				byte[] sendbuf = Encoding.ASCII.GetBytes("IP");
				IPEndPoint ep = new IPEndPoint(IPAddress.Broadcast, listenPort);
				client.Send(sendbuf, sendbuf.Length, ep);
				client.Close();  
			}
			catch (Exception e) 
			{ }
		}

        /// <summary>
        /// Method that makes the request to the Gadgeteer server
        /// </summary>
        ///<param name="str">
        ///"Sensors" or "Relay" depending on which type of request it will be made
        ///</param>
		void Request(Device dev)
		{

			string url = "http://"+ IP +":"+ port +"/";
			string code;
			switch (dev) {
			case Device.Sensors:
				url = url + "Sensors";
				code = performRequest(url);
				RunOnUiThread(() => {
		            buttonSensors.Text = "Leer sensores";
					if (code!=null)
					{
						string[] values = code.Split('/');
						//Here we show the values obtained in the reading sensor
						textLight.Text = "Luminosidad: " + values[0] + "%";
						textDistance.Text = "Distancia: " + values[1] + " cm.";
					}
				});
				break;
			case Device.Relay:
				url = url + "Relay";
				code = performRequest(url);
				break;
			}				
		}

        /// <summary>
        /// Makes the Http url connection for the request, and receives the reply
        /// </summary>
        /// <param name="link">Url of the request</param>
        /// <returns>Response data sent by the Gadgeteer server</returns>
		public String performRequest(String link)
		{
			URL url;
			HttpURLConnection urlConnection = null;

			try
			{
				url = new URL(link);
				urlConnection = (HttpURLConnection)url.OpenConnection();
				urlConnection.RequestMethod = "GET";

				int responseCode = (int)urlConnection.ResponseCode;

				Stream stream = urlConnection.InputStream;
				InputStreamReader isr = new InputStreamReader(stream);
				BufferedReader reader = new BufferedReader(isr);

				String response = "";
				String line;
				while ((line = reader.ReadLine()) != null)
				{ response += line; }

				reader.Close();
				urlConnection.Disconnect();

				return response;

			}

			catch (Exception e) { var a = e.StackTrace; }

			finally
			{
				if (urlConnection != null) urlConnection.Disconnect();
			}
			return null;
		}

        /// <summary>
        /// Starts the UDP listener that waits for an incoming message
        /// </summary>
		void StartListener() 
		{
			bool done = false;

			UdpClient listener = new UdpClient(listenPort);
			IPEndPoint groupEP = new IPEndPoint(IPAddress.Any,listenPort);

			try 
			{
				while (!done) 
				{
					//Waiting the broadcast
					byte[] bytes = listener.Receive(ref groupEP);

					//Broadcast received
					string msg = Encoding.ASCII.GetString(bytes,0,bytes.Length);
					if (msg.Split('/').Length >= 2 & msg.Split('/')[0] == "IP")
					{
						IP = msg.Split('/')[1];
					}
				}

			} 
			catch (Exception e) 
			{ }
			finally
			{
				listener.Close();
			}
		}
	}
}


