using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using com.thingworx.common.logging;
using com.thingworx.communications.client;
using com.thingworx.communications.common;

// Refer to the "Steam Sensor Example" section of the documentation
// for a detailed explanation of this example's operation 
namespace IoT
{

    
    public class SteamSensorClient : ConnectedThingClient
    {
     
        private static readonly TraceSource Logger = LoggerFactory.getLogger(typeof(SteamSensorClient));

        public SteamSensorClient(ClientConfigurator config) 
            : base(config)
        {}

        private void StartClient(object state)
        {
            start();
        }

        private void RunClient(object state)
        {
            // Loop over all the Virtual Things and process them
            foreach (var thing in getThings().Values)
            {
                try
                {
                    thing.processScanRequest();
                }
                catch (Exception eProcessing)
                {
                    Console.WriteLine("Error Processing Scan Request for [" + thing.getName() + "] : " + eProcessing.Message);
                }
            }
        }

        private static void Main(string[] args)
        {
                     
            if (args.Length < 3) 
            {
			    Console.WriteLine("Required arguments not found!");
			    Console.WriteLine("URI AppKey ScanRate");
			    Console.WriteLine("Example:");
                Console.WriteLine("SteamSensorClient.exe wss://localhost:443/Thingworx/WS xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx 1000");
                return;
		    }

            // Set the required configuration information
            var config = new ClientConfigurator
            {
                // Set the size of the threadpools
                MaxMsgHandlerThreadCount = 8,
                MaxApiTaskerThreadCount = 8,
                DisableCertValidation = true,
                OfflineMsgStoreDir = ".",
                // The uri for connecting to Thingworx
                Uri = args[0],
                // Reconnect every 15 seconds if a disconnect occurs or if initial connection cannot be made
                ReconnectInterval = 15
            };

            // Set the security using an Application Key
            var appKey = args[1];

            var claims = SecurityClaims.fromAppKey(appKey);
            config.Claims = claims;

            // Set the name of the client
            config.Name = "SteamSensorGateway";

            // Get the scan rate (milliseconds) that is specific to this example
            // The example will execute the processScanRequest of the VirtualThing
            // based on this scan rate
            var scanRate = int.Parse(args[2]);

            // Create the client passing in the configuration from above
            var client = new SteamSensorClient(config);

            try
            {
                // Create think
                var sensor1 = new SteamThing("ThingTemperature_dntichy", "Senzor id: ...dorob ID ak viac senzorov", "SN000", client);
                client.bindThing(sensor1);

                LoggerFactory.Log(Logger, TraceEventType.Critical, 
                    "CONNECTNG TO PLATFORM:\n   " +
                    "Uri: {0} \n   " +
                    "AppKey: {1} \n   " +
                    "Think: [name: {2}, identifier: {3}] \n   " +
                    "AllowSelfSignedCertificates: {4} \n   " +
                    "DisableCertValidation: {5}",
                    config.Uri, appKey, sensor1.getName(), sensor1.getIdentifier(),
                    config.AllowSelfSignedCertificates, config.DisableCertValidation);
                
                // Start the client
                ThreadPool.QueueUserWorkItem(client.StartClient);
            }
            catch (Exception eStart)
            {
                Console.WriteLine("Initial Start Failed : " + eStart.Message);
            }
            
            // Wait for the SteamSensorClient to connect, then process its associated things.
            // As long as the client has not been shutdown, continue
            while (!client.isShutdown())
            {
                // Only process the Virtual Things if the client is connected
                if (client.isConnected())
                {
                    ThreadPool.QueueUserWorkItem(client.RunClient);
                }
                
                // Suspend processing at the scan rate interval
                Thread.Sleep(scanRate);
            }
        }
    }
}
