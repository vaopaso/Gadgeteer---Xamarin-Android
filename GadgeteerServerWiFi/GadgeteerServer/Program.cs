using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;
using Microsoft.SPOT.Net.NetworkInformation;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;

namespace GadgeteerServer
{
    public partial class Program
    {
        private string IPAddress;
        private bool serverStarted = false;
        private string SSID = "Vicente";
        private string PASSWORD = "12345678";
        Distance_US3 distanceUS3 = new Distance_US3(8);
          
        void ProgramStarted()
        {
            Debug.Print("Program Started");

            if (!serverStarted)
                StartServer(); //Begin the connection
            else
                StopServer(); //Stops the connection
            
        }

        /// <summary>
        /// Stops the Gadgeteer server
        /// </summary>
        private void StopServer()
        {
            WebServer.StopLocalServer();
            serverStarted = false;
            Debug.Print("Server stopped");
        }

        /// <summary>
        /// Starts the Gadgeteer server, initializing the connection
        /// </summary>
        private void StartServer()
        {
            if (!wifiRS21.NetworkInterface.Opened)
            {
                wifiRS21.NetworkInterface.Open();
            }
            if (!wifiRS21.NetworkInterface.IsDhcpEnabled)
            {
                wifiRS21.NetworkInterface.EnableDhcp();
            }

            if (!wifiRS21.NetworkInterface.LinkConnected) //If there is still no established connection
            {
                //Assigning events that handle connection
                NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
                NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

                UdpSocketServer udpServer = new UdpSocketServer();
                udpServer.DataReceived += udpServer_DataReceived;

                wifiRS21.UseThisNetworkInterface();
                wifiRS21.NetworkInterface.Join(SSID, PASSWORD); //Joining the desired network

                Debug.Print("Connecting to network...");
                while (wifiRS21.NetworkInterface.IPAddress == "0.0.0.0") //The while remains running until is assigned an IP
                { Thread.Sleep(250); }
                Debug.Print("Connected!. SSID: " + SSID);

                udpServer.Start();
            }
            else //If there is already an established connection
            {
                RunServer();
            }
        }

        /// <summary>
        /// If receives a request, then replies with the IPAddress
        /// </summary>
        void udpServer_DataReceived(object sender, DataReceivedEventArgs e)
        {
            string receivedMessage = UdpSocketServer.BytesToString(e.Data);

            if (receivedMessage == "IP")
            {
                // Creates a response and assigns it to the DataReceivedEventArgs.ResponseData property, so that
                // it will be automtically sent to client.
                string response = "IP/"+IPAddress;
                e.ResponseData = System.Text.Encoding.UTF8.GetBytes(response);
            }

        }
        
        void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            Debug.Print("Network availability: " + e.IsAvailable.ToString());
        }

        void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            Debug.Print("Network address changed");
            IPAddress = wifiRS21.NetworkInterface.IPAddress;
            Debug.Print("Network address changed. IP: " + IPAddress);
            RunServer();
        }

        /// <summary>
        /// Starts the server, assigning the WebEvents
        /// </summary>
        private void RunServer()
        {
            Debug.Print("Starting Server");
            WebEvent GetValueEventSensors = WebServer.SetupWebEvent("Sensors"); //The name "Sensors" goes in the url http://{IP}:{port}/Sensors
            WebEvent GetValueEventRelay = WebServer.SetupWebEvent("Relay"); // http://{IP}:{port}/Relay

            GetValueEventSensors.WebEventReceived += GetValueEventSensors_WebEventReceived; //event to receive a request to address http://{IP}:{port}/Sensors
            GetValueEventRelay.WebEventReceived += GetValueEventRelay_WebEventReceived;

            WebServer.StartLocalServer(IPAddress, 80); //The server starts in the assigned IP, on port 80 in this case
            serverStarted = true;
        }

        /// <summary>
        /// Turns on/off the relay
        /// </summary>
        void GetValueEventRelay_WebEventReceived(string path, WebServer.HttpMethod method, Responder responder)
        {
            Debug.Print("Received request");
            if (relayX1.Enabled)
            { relayX1.TurnOff(); }
            else
            { relayX1.TurnOn(); }
        }
        
        /// <summary>
        /// Replies with the sensor values
        /// </summary>
        void GetValueEventSensors_WebEventReceived(string path, WebServer.HttpMethod method, Responder responder)
        {
            Debug.Print("Received request");
            string distance = distanceUS3.GetDistanceInCentimeters(3).ToString();
            string light = (lightSense.ReadProportion()*100).ToString();
            if (light.Length > 4) { light = light.Substring(0, 4); } //only to round off

            responder.Respond(light+"/"+distance); //Responding each sensor value, in this case separated by a "/".
            Debug.Print("Light: " + light + "%. Dist: "+distance+" cm.");
            Debug.Print("Sending response");
        }
    }

}
